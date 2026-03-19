package me.earzuchan.ryo.foundation.chamber

import me.earzuchan.ryo.foundation.RyoRuntime
import me.earzuchan.ryo.foundation.exception.GloryNotFoundException
import me.earzuchan.ryo.foundation.io.RyoReader
import me.earzuchan.ryo.foundation.io.RyoWriter
import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.special.requireFragmentalImage
import me.earzuchan.ryo.foundation.special.specialValue
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.util.CompressionUtils
import me.earzuchan.ryo.foundation.value.RyoMeta
import me.earzuchan.ryo.foundation.value.RyoNullValue
import me.earzuchan.ryo.foundation.value.RyoUnknownFrame
import me.earzuchan.ryo.foundation.value.RyoUnknownGraph
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
        internal const val META_SHIFT = RyoMeta.SHIFT
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
    private val metaHeap = mutableListOf<RyoMeta>()

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
            importUnknownValue(id, value)
            return
        }

        ryo.validateRyoValue(value)
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
        val metaSlice = if (frame.metaHeapCount <= 0) emptyList() else List(frame.metaHeapCount) { idx -> metaHeap[frame.metaHeapOffset + idx] }
        val glory = ryo.findGlory(binding.gloryId) ?: return ryo.unknownValueFor(wireTypeId = binding.wireTypeId, gloryId = binding.gloryId, payload = frame.data!!.copyOf(), metas = metaSlice, graph = exportUnknownGraph(id))
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
        repeat(metaCount) { metaHeap += RyoMeta(indexReader.readInt()) }

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

        entryFrames.forEach { for (k in 0 until it.metaHeapCount) indexWriter.writeInt(metaHeap[it.metaHeapOffset + k].raw) }

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
                if (m.type != META_TYPE_REFED) continue
                val child = m.payload
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
        val newMeta = mutableListOf<RyoMeta>()
        val newFrames = mutableListOf<EntryFrame>()

        for (oldId in 0 until frameCount) {
            if (!alive[oldId]) continue
            val old = entryFrames[oldId]

            val metaStart = newMeta.size
            val metaEnd = old.metaHeapOffset + old.metaHeapCount
            for (k in old.metaHeapOffset until metaEnd) {
                val m = metaHeap[k]
                val rewritten = if (m.type == META_TYPE_REFED) {
                    val cOld = m.payload
                    val cNew = if (cOld in idMap.indices) idMap[cOld] else -1
                    if (cNew == -1) RyoMeta.create(0, META_TYPE_NULL) else RyoMeta.create(cNew, META_TYPE_REFED)
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

        val originalFrameCount = entryFrames.size
        val copiedMap = IntArray(originalFrameCount) { -1 }

        fun copyRecursive(currentId: Int): Int {
            if (currentId >= originalFrameCount) return currentId
            val oldFrame = entryFrames[currentId]
            if (oldFrame.data == null) return -1
            if (copiedMap[currentId] != -1) return copiedMap[currentId]

            val newId = allocateId()
            copiedMap[currentId] = newId

            val localMetas = IntArray(oldFrame.metaHeapCount)
            for (i in 0 until oldFrame.metaHeapCount) {
                val m = metaHeap[oldFrame.metaHeapOffset + i]
                if (m.type == META_TYPE_REFED) {
                    val childNewId = copyRecursive(m.payload)
                    localMetas[i] = if (childNewId != -1) RyoMeta.create(childNewId, META_TYPE_REFED).raw else RyoMeta.NULL.raw
                } else localMetas[i] = m.raw
            }

            // 统一写入全局堆
            val metaStart = metaHeap.size
            for (rawMeta in localMetas) metaHeap += RyoMeta(rawMeta)

            entryFrames[newId] = EntryFrame(
                gloryBindingId = oldFrame.gloryBindingId,
                metaHeapOffset = metaStart,
                metaHeapCount = oldFrame.metaHeapCount,
                data = oldFrame.data!!.copyOf()
            )

            return newId
        }

        return copyRecursive(rootId)
    }

    internal fun writeChild(ctx: WriteCtx, value: RyoValue) {
        if (value is RyoNullValue) {
            metaHeap += RyoMeta.create(0, META_TYPE_NULL)
            return
        }

        val wireTypeId = value.wireTypeId
        if (ryo.isInlineWireType(wireTypeId)) {
            val gloryId = ryo.resolveGloryId(value.typeRef)
            val bindingId = ensureBinding(wireTypeId, gloryId)
            metaHeap += RyoMeta.create(bindingId, META_TYPE_INLINED)
            ryo.requireGlory(gloryId).write(ctx, value, wireTypeId)
            return
        }

        val childId = allocateId()
        metaHeap += RyoMeta.create(childId, META_TYPE_REFED)
        ctx.pendingChildren += (childId to value)
    }

    internal fun readChild(ctx: ReadCtx, expectedTypeRef: TypeRef = TypeRefs.objectType(TypeIds.OBJECT)): RyoValue {
        require(ctx.metaPtr < ctx.metaEnd) { "Read meta out of bounds" }
        val meta = metaHeap[ctx.metaPtr++]
        val kind = meta.type
        val payload = meta.payload

        return when (kind) {
            META_TYPE_NULL -> RyoNullValue(expectedTypeRef)
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

    private fun importUnknownValue(targetId: Int, value: RyoUnknownValue) {
        val graph = value.graph
        if (graph == null) {
            require(value.metas.none { it.type == META_TYPE_REFED }) { "Unknown value contains referenced metas but has no unknown graph snapshot" }
            val bindingId = ensureBinding(value.wireTypeId, value.gloryId)
            val metaStart = metaHeap.size
            value.metas.forEach { metaHeap += it }
            entryFrames[targetId] = EntryFrame(bindingId, metaStart, value.metas.size, value.opaquePayload.copyOf())
            return
        }

        val frames = graph.frames.associateBy { it.nodeId }
        val imported = mutableMapOf<Int, Int>()

        fun importNode(legacyId: Int, forcedId: Int? = null): Int {
            imported[legacyId]?.let { return it }
            val frame = frames[legacyId] ?: error("Unknown graph missing frame id=$legacyId")
            val newId = forcedId ?: allocateId()
            imported[legacyId] = newId

            val bindingId = ensureBinding(frame.wireTypeId, frame.gloryId)

            // 【修Bug关键】预先收集子节点的引用，避免递归调用污染全局的 metaHeap
            val localMetas = IntArray(frame.metas.size)
            frame.metas.forEachIndexed { index, meta ->
                if (meta.type != META_TYPE_REFED) localMetas[index] = meta.raw else {
                    val childNewId = importNode(meta.payload) // 触发递归
                    localMetas[index] = RyoMeta.create(childNewId, META_TYPE_REFED).raw
                }
            }

            // 递归全部回归后，当前节点再安全、连续地推入全局 metaHeap
            val metaStart = metaHeap.size
            for (rawMeta in localMetas) metaHeap += RyoMeta(rawMeta)

            entryFrames[newId] = EntryFrame(bindingId, metaStart, localMetas.size, frame.opaquePayload.copyOf())
            return newId
        }

        importNode(graph.rootId, targetId)
    }

    private fun exportUnknownGraph(rootId: Int): RyoUnknownGraph {
        val stack = ArrayDeque<Int>()
        stack.addLast(rootId)

        // 【优化】维护一个从原绝对 ID 到相对序号(0 ~ N-1)的映射表
        val idRemap = linkedMapOf<Int, Int>()
        val frames = mutableListOf<RyoUnknownFrame>()

        fun remap(oldId: Int): Int = idRemap.getOrPut(oldId) { idRemap.size }

        while (stack.isNotEmpty()) {
            val id = stack.removeLast()
            val relativeId = remap(id)

            // 避免重复遍历
            if (frames.any { it.nodeId == relativeId }) continue

            val frame = entryFrames.getOrNull(id) ?: error("Unknown export frame id out of bounds: $id")
            val payload = requireNotNull(frame.data) { "Unknown export frame id=$id is deleted" }
            val binding = gloryBindings[frame.gloryBindingId]

            val metas = if (frame.metaHeapCount <= 0) emptyList() else List(frame.metaHeapCount) { idx -> metaHeap[frame.metaHeapOffset + idx] }

            // 【优雅】将 Meta 中的绝对引用替换为相对引用
            val relativeMetas = metas.map {
                if (it.type == META_TYPE_REFED) {
                    stack.addLast(it.payload) // 顺便压栈
                    RyoMeta.create(remap(it.payload), META_TYPE_REFED)
                } else it
            }

            frames += RyoUnknownFrame(relativeId, binding.wireTypeId, binding.gloryId, payload.copyOf(), relativeMetas)
        }

        // rootId 的相对序号一定是通过 remap(rootId) 获取的，通常就是 0
        return RyoUnknownGraph(remap(rootId), frames)
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
    inline fun <reified T : RyoValue> getAs(token: String): T? = get(token) as? T
    inline fun <reified T : RyoValue> requireAs(token: String): T = getAs<T>(token) ?: error("Token '$token' is not ${T::class.simpleName ?: "expected type"}")

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

    fun getFragmentalImage(groupIndex: Int, valueIndex: Int = 0): FragmentalImage = getGroup(groupIndex)[valueIndex].requireFragmentalImage()

    fun setFragmentalImage(groupIndex: Int, fi: FragmentalImage, valueIndex: Int = 0) {
        val group = getGroup(groupIndex).toMutableList()
        require(valueIndex in group.indices) { "Invalid valueIndex=$valueIndex for group=$groupIndex" }
        group[valueIndex] = fi.specialValue()
        setGroup(groupIndex, group)
    }

    fun createFragmentalImageGroup(fi: FragmentalImage): Int = createGroup(listOf(fi.specialValue())) // TIPS：便捷方法

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
