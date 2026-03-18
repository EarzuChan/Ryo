package me.earzuchan.ryo.foundation.chamber

import me.earzuchan.ryo.foundation.RyoRuntime
import me.earzuchan.ryo.foundation.exception.GloryNotFoundException
import me.earzuchan.ryo.foundation.io.RyoReader
import me.earzuchan.ryo.foundation.io.RyoWriter
import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.special.requireFragmentalImage
import me.earzuchan.ryo.foundation.special.specialValue
import me.earzuchan.ryo.foundation.util.CompressionUtils
import me.earzuchan.ryo.foundation.value.RyoUnknownValue
import me.earzuchan.ryo.foundation.value.RyoValue

// VAMOS，这个Chamber的命名太好了，既表面了它是一个容器，也致敬了尚勃的臭勒。Vamos！

abstract class Chamber internal constructor(internal val ryo: RyoRuntime) { // TIPS：Id等为内部可用，故不向调库者泄露
    @Suppress("ArrayInDataClass")
    private data class EntryFrame(var gloryBindingId: Int, var metaHeapOffset: Int, var metaHeapCount: Int, var data: ByteArray?)

    private data class GloryBinding(val wireTypeId: String, val gloryId: String)

    internal class ReadCtx(val chamber: Chamber, val reader: RyoReader, var metaPtr: Int, val metaEnd: Int)

    internal class WriteCtx(val chamber: Chamber, val writer: RyoWriter, val pendingChildren: MutableList<Pair<Int, RyoValue>> = mutableListOf())

    companion object {
        internal const val META_SHIFT = 2
        internal const val META_TYPE_NULL = 0
        internal const val META_TYPE_INLINED = 2
        internal const val META_TYPE_REFED = 3

        internal const val TOMBSTONE_BINDING_ID = -1

        internal const val BLOB_DEFLATE_MIN_BYTES = 64
        internal const val BLOB_DEFLATE_RATIO_THRESHOLD = 0.9
        internal const val INDEX_DEFLATE_RATIO_THRESHOLD = 1.1
    }

    protected abstract val chamberName: String

    private val entryFrames = mutableListOf<EntryFrame>()
    private val gloryBindings = mutableListOf<GloryBinding>()
    private val metaHeap = mutableListOf<Int>()

    internal fun add(value: RyoValue): Int {
        val id = allocateId()
        set(id, value)
        return id
    }

    private fun allocateId(): Int {
        val id = entryFrames.size
        entryFrames += EntryFrame(TOMBSTONE_BINDING_ID, 0, 0, null)
        return id
    }

    internal fun set(id: Int, value: RyoValue) {
        require(id in entryFrames.indices) { "Invalid id $id" }
        val oldFrame = entryFrames[id]
        val frameCountStart = entryFrames.size

        if (value is RyoUnknownValue) {
            val payload = requireNotNull(value.opaquePayload) { "Cannot set unknown value without opaque payload. wireType=${value.wireTypeId}" }
            val bindingId = ensureBinding(value.wireTypeId, value.gloryId)
            val metaStart = metaHeap.size
            value.metaHeapSlice.forEach { metaHeap += it }
            entryFrames[id] = EntryFrame(bindingId, metaStart, value.metaHeapSlice.size, payload.copyOf())
            return
        }

        ryo.validateForWrite(value)
        val wireTypeId = value.wireTypeId
        val gloryId = ryo.resolveGloryId(value.typeRef)
        val bindingId = ensureBinding(wireTypeId, gloryId)

        val metaStart = metaHeap.size
        try {
            val writer = RyoWriter()
            val writeCtx = WriteCtx(this, writer)
            ryo.requireGlory(gloryId).write(writeCtx, value, wireTypeId)
            entryFrames[id] = EntryFrame(bindingId, metaStart, metaHeap.size - metaStart, writer.toByteArray())
            writeCtx.pendingChildren.forEach { (childId, childValue) -> set(childId, childValue) }
        } catch (t: Throwable) {
            while (metaHeap.size > metaStart) metaHeap.removeAt(metaHeap.lastIndex)
            while (entryFrames.size > frameCountStart) entryFrames.removeAt(entryFrames.lastIndex)
            entryFrames[id] = oldFrame
            throw t
        }
    }

    internal fun get(id: Int): RyoValue {
        require(id in entryFrames.indices) { "Invalid id $id" }
        val frame = entryFrames[id]
        require(frame.data != null) { "Deleted object id=$id" }

        val binding = gloryBindings[frame.gloryBindingId]
        val metaEnd = frame.metaHeapOffset + frame.metaHeapCount
        val metaSlice = if (frame.metaHeapCount <= 0) intArrayOf() else IntArray(frame.metaHeapCount) { idx -> metaHeap[frame.metaHeapOffset + idx] }
        val glory = ryo.findGlory(binding.gloryId) ?: return ryo.unknownValueFor(
            wireTypeId = binding.wireTypeId, gloryId = binding.gloryId, payload = frame.data!!.copyOf(), metaHeapSlice = metaSlice
        )
        val rctx = ReadCtx(this, RyoReader(frame.data!!), frame.metaHeapOffset, metaEnd)
        return glory.read(rctx, binding.wireTypeId)
    }

    internal fun remove(id: Int): Boolean = removeInternal(id, notify = true)

    protected fun removeSilently(id: Int): Boolean = removeInternal(id, notify = false)

    private fun removeInternal(id: Int, notify: Boolean): Boolean {
        if (id !in entryFrames.indices) return false
        entryFrames[id].data = null
        entryFrames[id].metaHeapCount = 0
        entryFrames[id].gloryBindingId = TOMBSTONE_BINDING_ID
        if (notify) onRemove(id)
        return true
    }

    fun loadFromBytes(bytes: ByteArray): Chamber {
        clearAll()

        val reader = RyoReader(bytes)
        require(reader.checkFixedString(chamberName)) { "Invalid chamber header for $chamberName" }

        val indexInfo = reader.readInt()
        val isDeflatedIndex = (indexInfo and 1) != 0
        val indexLength = indexInfo ushr 1
        val indexBlob = if (isDeflatedIndex) CompressionUtils.inflate(reader.readBytes(indexLength)) else reader.readBytes(indexLength)

        val indexReader = RyoReader(indexBlob)
        val objCount = indexReader.readInt()

        val entryBlobDeflated = BooleanArray(objCount)
        val entryBlobBindingIds = IntArray(objCount)
        for (i in 0 until objCount) {
            val entryBlobInfo = indexReader.readInt()
            if (entryBlobInfo == TOMBSTONE_BINDING_ID) {
                entryBlobBindingIds[i] = TOMBSTONE_BINDING_ID
                entryBlobDeflated[i] = false
            } else {
                entryBlobBindingIds[i] = entryBlobInfo ushr 1
                entryBlobDeflated[i] = (entryBlobInfo and 1) != 0
            }
        }

        val entryBlobEndPositions = IntArray(objCount) { indexReader.readInt() }
        val fileMetaHeapEndPositions = IntArray(objCount) { indexReader.readInt() }

        val metaCount = if (objCount == 0) 0 else fileMetaHeapEndPositions.last()
        repeat(metaCount) { metaHeap += indexReader.readInt() }

        val bindingCount = indexReader.readInt()
        repeat(bindingCount) {
            indexReader.readInt() // redundant sequential id
            val wireTypeId = indexReader.readString() ?: error("Null wireTypeId in binding")
            val gloryId = indexReader.readString() ?: error("Null gloryId in binding")
            gloryBindings += GloryBinding(wireTypeId, gloryId)
        }

        readExtraIndex(indexReader)

        val entryBlobsBuffer = reader.readAllBytes()
        val blobsReader = RyoReader(entryBlobsBuffer)

        var prevMetaHeapEnd = 0
        for (i in 0 until objCount) {
            val blobStart = if (i == 0) 0 else entryBlobEndPositions[i - 1]
            val blobLength = entryBlobEndPositions[i] - blobStart

            val blobData: ByteArray? = if (entryBlobBindingIds[i] == TOMBSTONE_BINDING_ID) null else {
                val raw = blobsReader.readBytes(blobLength)
                if (entryBlobDeflated[i]) CompressionUtils.inflate(raw) else raw
            }

            val currentMetaHeapEnd = fileMetaHeapEndPositions[i]
            val metaHeapOffset = prevMetaHeapEnd
            val metaHeapCount = currentMetaHeapEnd - prevMetaHeapEnd
            prevMetaHeapEnd = currentMetaHeapEnd

            entryFrames += EntryFrame(entryBlobBindingIds[i], metaHeapOffset, metaHeapCount, blobData)
        }

        return this
    }

    fun saveToBytes(allowDeflate: Boolean = true): ByteArray { // TIPS：为了方便检视，我们显式控制是否允许压缩；未来为了优雅，可以撤掉这个flag
        val writer = RyoWriter()
        writer.writeFixedString(chamberName)

        val objCount = entryFrames.size
        val finalDataChunks = Array(objCount) { ByteArray(0) }
        val isChunkDeflated = BooleanArray(objCount)

        for (i in 0 until objCount) {
            val rawData = entryFrames[i].data
            if (rawData == null || rawData.isEmpty()) {
                finalDataChunks[i] = ByteArray(0)
                isChunkDeflated[i] = false
                continue
            }

            if (allowDeflate && rawData.size > BLOB_DEFLATE_MIN_BYTES) {
                val compressed = CompressionUtils.deflate(rawData)
                if (compressed.size < (rawData.size * BLOB_DEFLATE_RATIO_THRESHOLD).toInt()) {
                    finalDataChunks[i] = compressed
                    isChunkDeflated[i] = true
                } else {
                    finalDataChunks[i] = rawData
                    isChunkDeflated[i] = false
                }
            } else {
                finalDataChunks[i] = rawData
                isChunkDeflated[i] = false
            }
        }

        val indexWriter = RyoWriter()
        indexWriter.writeInt(objCount)

        for (i in 0 until objCount) {
            val frame = entryFrames[i]
            if (frame.data == null) indexWriter.writeInt(TOMBSTONE_BINDING_ID) else {
                var info = frame.gloryBindingId shl 1
                if (isChunkDeflated[i]) info = info or 1
                indexWriter.writeInt(info)
            }
        }

        var currentSavedLength = 0
        for (i in 0 until objCount) {
            currentSavedLength += finalDataChunks[i].size
            indexWriter.writeInt(currentSavedLength)
        }

        var currentVirtualMetaEnd = 0
        for (i in 0 until objCount) {
            currentVirtualMetaEnd += entryFrames[i].metaHeapCount
            indexWriter.writeInt(currentVirtualMetaEnd)
        }

        entryFrames.forEach { for (k in 0 until it.metaHeapCount) indexWriter.writeInt(metaHeap[it.metaHeapOffset + k]) }

        indexWriter.writeInt(gloryBindings.size)
        gloryBindings.forEachIndexed { index, binding ->
            indexWriter.writeInt(index)
            indexWriter.writeString(binding.wireTypeId)
            indexWriter.writeString(binding.gloryId)
        }

        writeExtraIndex(indexWriter)

        val indexBytes = indexWriter.toByteArray()
        val compressedIndexBytes = if (indexBytes.isNotEmpty()) CompressionUtils.deflate(indexBytes) else indexBytes

        if (allowDeflate && indexBytes.isNotEmpty() && compressedIndexBytes.isNotEmpty()) {
            val ratio = indexBytes.size.toDouble() / compressedIndexBytes.size.toDouble()
            if (ratio >= INDEX_DEFLATE_RATIO_THRESHOLD) {
                writer.writeInt((compressedIndexBytes.size shl 1) or 1)
                writer.writeBytes(compressedIndexBytes)
            } else {
                writer.writeInt(indexBytes.size shl 1)
                writer.writeBytes(indexBytes)
            }
        } else {
            writer.writeInt(indexBytes.size shl 1)
            writer.writeBytes(indexBytes)
        }

        for (i in 0 until objCount) {
            val chunk = finalDataChunks[i]
            if (chunk.isNotEmpty()) writer.writeBytes(chunk)
        }

        return writer.toByteArray()
    }

    protected open fun onRemove(id: Int) {}
    protected open fun rootIds(): IntArray = intArrayOf()
    protected open fun onIdsRemapped(idMap: IntArray) {}
    protected open fun clearExtraIndex() {}
    protected open fun readExtraIndex(reader: RyoReader) {}
    protected open fun writeExtraIndex(writer: RyoWriter) {}

    // TIPS：卷GC，清理死对象，并联动实现类进行优化
    fun collectGarbage() {
        val roots = rootIds()
        val frameCount = entryFrames.size

        // 【优化点】：使用 BooleanArray 替代 HashSet，彻底消灭装箱和 Hash 计算
        val alive = BooleanArray(frameCount)

        // 【优化点】：手写一个基于 IntArray 的轻量级栈，替代 ArrayDeque<Int>，实现 100% 零装箱图遍历
        var stackSize = 0
        var stack = IntArray(roots.size.coerceAtLeast(128))

        fun push(id: Int) {
            if (stackSize == stack.size) stack = stack.copyOf(stack.size * 2)
            stack[stackSize++] = id
        }

        // 标记根节点
        for (root in roots) if (root in 0 until frameCount && entryFrames[root].data != null && !alive[root]) {
            alive[root] = true
            push(root)
        }

        // 标记-清除：深度优先遍历所有存活节点
        while (stackSize > 0) {
            val id = stack[--stackSize] // pop
            val b = entryFrames[id]
            val end = b.metaHeapOffset + b.metaHeapCount
            for (i in b.metaHeapOffset until end) {
                val m = metaHeap[i]
                if ((m and 3) != META_TYPE_REFED) continue

                val child = m ushr META_SHIFT
                if (child in 0 until frameCount && entryFrames[child].data != null && !alive[child]) {
                    alive[child] = true
                    push(child)
                }
            }
        }

        // 收集存活的 GloryBinding，同样使用 BooleanArray
        val bindingUsed = BooleanArray(gloryBindings.size)
        for (i in 0 until frameCount) if (alive[i]) {
            val bindingId = entryFrames[i].gloryBindingId
            if (bindingId != TOMBSTONE_BINDING_ID) bindingUsed[bindingId] = true
        }

        // 重映射 GloryBinding
        val bindingMap = IntArray(gloryBindings.size) { -1 }
        val newBindings = mutableListOf<GloryBinding>()
        gloryBindings.forEachIndexed { old, b ->
            if (bindingUsed[old]) {
                bindingMap[old] = newBindings.size
                newBindings += b
            }
        }

        // 重映射 EntryFrame ID
        val idMap = IntArray(frameCount) { -1 }
        var nextId = 0
        for (old in 0 until frameCount) if (alive[old]) idMap[old] = nextId++

        // 压缩阶段：重建 metaHeap 和 entryFrames
        val newMeta = mutableListOf<Int>()
        val newFrames = mutableListOf<EntryFrame>()

        for (oldId in 0 until frameCount) {
            if (!alive[oldId]) continue
            val old = entryFrames[oldId]

            val metaStart = newMeta.size
            val metaEnd = old.metaHeapOffset + old.metaHeapCount
            for (k in old.metaHeapOffset until metaEnd) {
                val m = metaHeap[k]
                val kind = m and 3
                val rewritten = if (kind == META_TYPE_REFED) {
                    val cOld = m ushr META_SHIFT
                    val cNew = if (cOld in idMap.indices) idMap[cOld] else -1
                    if (cNew == -1) META_TYPE_NULL else (cNew shl META_SHIFT) or META_TYPE_REFED
                } else m
                newMeta += rewritten
            }

            val newBinding = bindingMap[old.gloryBindingId]
            newFrames += EntryFrame(newBinding, metaStart, old.metaHeapCount, old.data) // 这里 data 已经是存活的引用，无需 copy
        }

        entryFrames.clear()
        entryFrames += newFrames
        metaHeap.clear()
        metaHeap += newMeta
        gloryBindings.clear()
        gloryBindings += newBindings

        onIdsRemapped(idMap)
    }

    internal fun deepCopy(rootId: Int): Int {
        require(rootId in entryFrames.indices && entryFrames[rootId].data != null) { "Cannot deep copy invalid or dead id: $rootId" }

        // 记录在本次深拷贝中，旧 ID 到新 ID 的映射
        // 这是为了防止 DAG(有向无环图) 中同一个对象被引用多次时，发生重复拷贝
        val originalFrameCount = entryFrames.size
        val copiedMap = IntArray(originalFrameCount) { -1 }

        fun copyRecursive(currentId: Int): Int { // 如果这个 ID 是在本次拷贝之外新增的，或者是死节点，直接返回
            if (currentId >= originalFrameCount) return currentId
            val oldFrame = entryFrames[currentId]
            if (oldFrame.data == null) return -1

            // 如果已经拷贝过，直接返回新 ID，维持原始图的引用结构（完美处理 DAG 分支共享）
            if (copiedMap[currentId] != -1) return copiedMap[currentId]

            // 预分配新的占位 ID
            val newId = allocateId()
            copiedMap[currentId] = newId

            val metaStart = metaHeap.size
            val metaEnd = oldFrame.metaHeapOffset + oldFrame.metaHeapCount

            // 递归深拷贝所有的 Meta 引用
            for (i in oldFrame.metaHeapOffset until metaEnd) {
                val m = metaHeap[i]
                val kind = m and 3
                if (kind == META_TYPE_REFED) {
                    val childOldId = m ushr META_SHIFT
                    val childNewId = copyRecursive(childOldId)
                    metaHeap += if (childNewId != -1) ((childNewId shl META_SHIFT) or META_TYPE_REFED) else META_TYPE_NULL // 死引用替换为空
                } else metaHeap += m
            }

            // 更新新节点的数据
            entryFrames[newId] = EntryFrame(
                gloryBindingId = oldFrame.gloryBindingId, metaHeapOffset = metaStart, metaHeapCount = metaHeap.size - metaStart, data = oldFrame.data!!.copyOf() // 【关键】深拷贝二进制 payload
            )

            return newId
        }

        return copyRecursive(rootId)
    }

    internal fun writeChild(ctx: WriteCtx, value: RyoValue?) {
        if (value == null) {
            metaHeap += META_TYPE_NULL
            return
        }

        val wireTypeId = value.wireTypeId
        if (ryo.isInlineWireType(wireTypeId)) {
            val gloryId = ryo.resolveGloryId(value.typeRef)
            val bindingId = ensureBinding(wireTypeId, gloryId)
            metaHeap += (bindingId shl META_SHIFT) or META_TYPE_INLINED
            ryo.requireGlory(gloryId).write(ctx, value, wireTypeId)
            return
        }

        val childId = allocateId()
        metaHeap += (childId shl META_SHIFT) or META_TYPE_REFED
        ctx.pendingChildren += (childId to value)
    }

    internal fun readChild(ctx: ReadCtx): RyoValue? {
        require(ctx.metaPtr < ctx.metaEnd) { "Read meta out of bounds" }
        val meta = metaHeap[ctx.metaPtr++]
        val kind = meta and 3
        val payload = meta ushr META_SHIFT

        return when (kind) {
            META_TYPE_NULL -> null
            META_TYPE_REFED -> get(payload)
            META_TYPE_INLINED -> {
                val binding = gloryBindings[payload]
                val glory = ryo.findGlory(binding.gloryId) ?: throw GloryNotFoundException(binding.gloryId, binding.wireTypeId)
                glory.read(ctx, binding.wireTypeId)
            }

            else -> error("Unsupported meta kind: $kind")
        }
    }

    private fun ensureBinding(wireTypeId: String, gloryId: String): Int {
        val idx = gloryBindings.indexOfFirst { it.wireTypeId == wireTypeId && it.gloryId == gloryId }
        if (idx >= 0) return idx

        gloryBindings += GloryBinding(wireTypeId, gloryId)
        return gloryBindings.lastIndex
    }

    private fun clearAll() {
        entryFrames.clear()
        gloryBindings.clear()
        metaHeap.clear()
        clearExtraIndex()
    }
}

class VolumeChamber internal constructor(ryo: RyoRuntime) : Chamber(ryo) {
    override val chamberName: String = "FileSystem"

    private val tokenToId = linkedMapOf<String, Int>()

    fun tokens(): Set<String> = tokenToId.keys.toSet()
    fun contains(token: String): Boolean = token in tokenToId
    fun get(token: String): RyoValue? = tokenToId[token]?.let { get(it) }
    fun tryGet(token: String): Result<RyoValue?> = runCatching { get(token) }

    fun set(token: String, value: RyoValue) {
        val newId = add(value)
        tokenToId[token] = newId
    }

    fun trySet(token: String, value: RyoValue): Result<Unit> = runCatching { set(token, value) }

    fun delete(token: String): Boolean {
        val id = tokenToId.remove(token) ?: return false
        return remove(id)
    }

    fun tryDelete(token: String): Result<Boolean> = runCatching { delete(token) }

    fun rename(oldToken: String, newToken: String) {
        val id = tokenToId.remove(oldToken) ?: error("Token not found: $oldToken")
        require(newToken !in tokenToId) { "Token exists: $newToken" }
        tokenToId[newToken] = id
    }

    fun clone(sourceToken: String, destToken: String, overwrite: Boolean = false) {
        val srcId = tokenToId[sourceToken] ?: error("Token not found: $sourceToken")
        if (!overwrite && destToken in tokenToId) error("Token exists: $destToken")

        // 一键纯二进制深拷贝整个树/图
        val newId = deepCopy(srcId)

        // 如果是要覆盖，先清理旧的根节点（顺便依赖后续的 GC 清理旧图）
        if (overwrite) {
            val oldDestId = tokenToId[destToken]
            if (oldDestId != null) remove(oldDestId)
        }

        tokenToId[destToken] = newId
    }

    override fun rootIds(): IntArray = tokenToId.values.toIntArray()

    override fun onIdsRemapped(idMap: IntArray) {
        val keys = tokenToId.keys.toList()
        keys.forEach { k ->
            val old = tokenToId[k] ?: return@forEach
            val mapped = if (old in idMap.indices) idMap[old] else -1
            if (mapped == -1) tokenToId.remove(k) else tokenToId[k] = mapped
        }
    }

    override fun onRemove(id: Int) {
        val key = tokenToId.entries.firstOrNull { it.value == id }?.key ?: return
        tokenToId.remove(key)
    }

    override fun readExtraIndex(reader: RyoReader) {
        val n = reader.readInt()
        tokenToId.clear()
        repeat(n) {
            val token = reader.readString() ?: error("Null token in volume index")
            val id = reader.readInt()
            tokenToId[token] = id
        }
    }

    override fun clearExtraIndex() = tokenToId.clear()

    override fun writeExtraIndex(writer: RyoWriter) {
        writer.writeInt(tokenToId.size)
        tokenToId.forEach { (token, id) ->
            writer.writeString(token)
            writer.writeInt(id)
        }
    }
}

class TextureChamber internal constructor(ryo: RyoRuntime) : Chamber(ryo) {
    override val chamberName: String = "TextureFile"
    private val groups = mutableListOf<IntArray>()

    val groupCount: Int get() = groups.size

    fun getGroup(groupIndex: Int): List<RyoValue> = groups[groupIndex].map(::get)

    fun setGroup(groupIndex: Int, values: List<RyoValue>) {
        require(values.isNotEmpty()) { "Group must contain at least one value" }
        val old = groups[groupIndex]
        for (id in old) removeSilently(id)
        groups[groupIndex] = values.map(::add).toIntArray()
    }

    fun createGroup(values: List<RyoValue>): Int {
        require(values.isNotEmpty()) { "Group must contain at least one value" }
        groups += values.map(::add).toIntArray()
        return groups.lastIndex
    }

    fun appendToGroup(groupIndex: Int, value: RyoValue): Int {
        val id = add(value)
        groups[groupIndex] = groups[groupIndex] + id
        return groups[groupIndex].lastIndex
    }

    fun removeFromGroup(groupIndex: Int, valueIndex: Int): Boolean {
        if (groupIndex !in groups.indices) return false
        val ids = groups[groupIndex]
        if (valueIndex !in ids.indices) return false
        removeSilently(ids[valueIndex])
        val next = ids.filterIndexed { idx, _ -> idx != valueIndex }.toIntArray()
        if (next.isEmpty()) groups.removeAt(groupIndex) else groups[groupIndex] = next
        return true
    }

    fun deleteGroup(groupIndex: Int): Boolean {
        if (groupIndex !in groups.indices) return false
        val ids = groups.removeAt(groupIndex)
        for (id in ids) removeSilently(id)
        return true
    }

    fun cloneGroup(groupIndex: Int): Int {
        val source = groups[groupIndex]
        val copied = IntArray(source.size) { i -> deepCopy(source[i]) }
        groups += copied
        return groups.lastIndex
    }

    fun getFi(groupIndex: Int, valueIndex: Int = 0): FragmentalImage = getGroup(groupIndex)[valueIndex].requireFragmentalImage()

    fun setFi(groupIndex: Int, fi: FragmentalImage, valueIndex: Int = 0) {
        val group = getGroup(groupIndex).toMutableList()
        require(valueIndex in group.indices) { "Invalid valueIndex=$valueIndex for group=$groupIndex" }
        group[valueIndex] = fi.specialValue()
        setGroup(groupIndex, group)
    }

    fun createFiGroup(fi: FragmentalImage): Int = createGroup(listOf(fi.specialValue()))

    override fun rootIds(): IntArray {
        var total = 0
        groups.forEach { total += it.size }
        val roots = IntArray(total)
        var offset = 0
        groups.forEach { ids ->
            ids.copyInto(roots, offset)
            offset += ids.size
        }
        return roots
    }

    override fun onRemove(id: Int) {
        var gi = groups.lastIndex
        while (gi >= 0) {
            val ids = groups[gi]
            if (!ids.contains(id)) {
                gi--
                continue
            }
            val next = ids.filter { it != id }.toIntArray()
            if (next.isEmpty()) groups.removeAt(gi) else groups[gi] = next
            gi--
        }
    }

    override fun onIdsRemapped(idMap: IntArray) {
        var gi = groups.lastIndex
        while (gi >= 0) {
            val src = groups[gi]
            var kept = 0
            val mapped = IntArray(src.size)
            src.forEach { old ->
                val newId = if (old in idMap.indices) idMap[old] else -1
                if (newId != -1) mapped[kept++] = newId
            }
            val result = if (kept == mapped.size) mapped else mapped.copyOf(kept)
            if (result.isEmpty()) groups.removeAt(gi) else groups[gi] = result
            gi--
        }
    }

    override fun readExtraIndex(reader: RyoReader) {
        val count = reader.readInt()
        groups.clear()
        repeat(count) {
            val valueCount = reader.readInt()
            groups += IntArray(valueCount) { reader.readInt() }
        }
    }

    override fun clearExtraIndex() = groups.clear()

    override fun writeExtraIndex(writer: RyoWriter) {
        writer.writeInt(groups.size)
        groups.forEach { ids ->
            writer.writeInt(ids.size)
            ids.forEach(writer::writeInt)
        }
    }
}
