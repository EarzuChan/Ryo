package me.earzuchan.ryo.modern.session

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import me.earzuchan.ryo.foundation.value.RyoContainerValue
import me.earzuchan.ryo.foundation.value.RyoHostedValue
import me.earzuchan.ryo.foundation.value.RyoScalarValue
import me.earzuchan.ryo.foundation.value.RyoSpecialValue
import me.earzuchan.ryo.foundation.value.RyoUnknownValue
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.ModernRuntime
import kotlin.collections.iterator

sealed interface ValuePathSegment
data class MemberPathSegment(val name: String) : ValuePathSegment
data class IndexPathSegment(val index: Int) : ValuePathSegment

data class ValuePath(val segments: List<ValuePathSegment> = emptyList()) {
    fun member(name: String) = ValuePath(segments + MemberPathSegment(name))
    fun index(index: Int) = ValuePath(segments + IndexPathSegment(index))
    operator fun plus(other: ValuePath) = ValuePath(segments + other.segments)

    // 判断两个路径是否有交集（父子关系）。用于精准反应流唤醒！
    fun isAffectedBy(mutatedPath: ValuePath): Boolean = if (this.segments.size <= mutatedPath.segments.size)
        mutatedPath.segments.take(this.segments.size) == this.segments
    else this.segments.take(mutatedPath.segments.size) == mutatedPath.segments

    companion object {
        val ROOT = ValuePath()
    }
}

class EditorSession internal constructor(internal val runtime: ModernRuntime, initialSnapshot: RyoValue) {
    private var currentRoot: RyoValue = initialSnapshot
    private val history = mutableListOf<RyoValue>().apply { add(initialSnapshot) }
    private var cursorIndex = 0

    // KMP 轻量级线程安全 Map 替换方案：不可变 Map 交换 (Copy-on-Write)
    // 读操作完全无锁，性能极高；写操作加锁复制。
    private val registryLock = Any()
    private var flowRegistry: Map<ValuePath, MutableStateFlow<RyoValue?>> = emptyMap()

    // 按需分发 Flow (懒反应)
    fun observe(path: ValuePath): StateFlow<RyoValue?> {
        flowRegistry[path]?.let { return it.asStateFlow() }

        synchronized(registryLock) {
            // Double check
            flowRegistry[path]?.let { return it.asStateFlow() }
            val newFlow = MutableStateFlow(resolveValue(currentRoot, path))
            flowRegistry = flowRegistry + (path to newFlow)
            return newFlow.asStateFlow()
        }
    }

    fun resolve(path: ValuePath): RyoValue? = resolveValue(currentRoot, path)

    // 核心：处理批量事务提交
    internal fun commitBatch(mutations: Map<ValuePath, RyoValue?>) {
        if (mutations.isEmpty()) return

        // O(N) 批量结构共享，生成新树
        val flattenedMutations = mutations.map { it.key.segments to it.value }
        currentRoot = batchReplace(currentRoot, flattenedMutations) ?: error("Root cannot be null after commit")

        // 维护历史栈
        if (cursorIndex < history.lastIndex) history.subList(cursorIndex + 1, history.size).clear()
        history.add(currentRoot)
        cursorIndex = history.lastIndex
        if (history.size > 128) { // 限制历史记录数量
            history.removeAt(0)
            cursorIndex--
        }

        // 精准爆破唤醒 UI！
        val currentRegistry = flowRegistry
        currentRegistry.forEach { (registeredPath, stateFlow) ->
            val affected = mutations.keys.any { mutatedPath -> registeredPath.isAffectedBy(mutatedPath) }
            if (affected) stateFlow.value = resolveValue(currentRoot, registeredPath)
        }
    }

    fun undo(): Boolean {
        if (cursorIndex <= 0) return false
        cursorIndex--
        currentRoot = history[cursorIndex]
        broadcastAll()
        return true
    }

    fun redo(): Boolean {
        if (cursorIndex >= history.lastIndex) return false
        cursorIndex++
        currentRoot = history[cursorIndex]
        broadcastAll()
        return true
    }

    val rootCursor: RyoCursor get() = cursorAt(ValuePath.ROOT)
    inline fun <reified T : RyoCursor> getRootCursorAs(): T = rootCursor as? T ?: error("Root cursor is ${rootCursor::class.simpleName}, expected ${T::class.simpleName}")

    fun extractFinalResult(): RyoValue = currentRoot

    private fun broadcastAll() = flowRegistry.forEach { (path, flow) -> flow.value = resolveValue(currentRoot, path) }

    private fun cursorAt(path: ValuePath): RyoCursor = when (resolve(path)) {
        is RyoHostedValue -> HostedCursor(this, path)
        is RyoContainerValue -> ContainerCursor(this, path)
        else -> ScalarCursor(this, path)
    }
}

// ==========================================
// 智能游标系统 (Cursors)
// ==========================================
sealed class RyoCursor(val session: EditorSession, val path: ValuePath) {
    val stateFlow: StateFlow<RyoValue?> get() = session.observe(path)
    val snapshot: RyoValue? get() = session.resolve(path)

    // 快捷类型转换助手
    fun asHosted() = this as HostedCursor
    fun asContainer() = this as ContainerCursor
    fun asScalar() = this as ScalarCursor
}

class HostedCursor(session: EditorSession, path: ValuePath) : RyoCursor(session, path) {
    // 优雅向下寻址
    operator fun get(name: String): RyoCursor {
        val childPath = path.member(name)
        val snapshot = session.resolve(childPath)
        return when (snapshot) {
            is RyoHostedValue -> HostedCursor(session, childPath)
            is RyoContainerValue -> ContainerCursor(session, childPath)
            else -> ScalarCursor(session, childPath)
        }
    }

    // 💥 补上灵魂：单点直接赋值操作符
    operator fun set(name: String, value: Any?) {
        val childPath = path.member(name)
        session.commitBatch(mapOf(childPath to toCommitValue(session, childPath, value)))
    }

    // 开启高墙内的事务
    fun edit(block: EditorTransaction.() -> Unit) {
        val tx = EditorTransaction(session, path)
        tx.block()
        session.commitBatch(tx.pendingMutations)
    }
}

class ContainerCursor(session: EditorSession, path: ValuePath) : RyoCursor(session, path) {
    operator fun get(index: Int): RyoCursor {
        val childPath = path.index(index)
        val snapshot = session.resolve(childPath)
        return when (snapshot) {
            is RyoHostedValue -> HostedCursor(session, childPath)
            is RyoContainerValue -> ContainerCursor(session, childPath)
            else -> ScalarCursor(session, childPath)
        }
    }

    operator fun set(index: Int, value: Any?) {
        val childPath = path.index(index)
        session.commitBatch(mapOf(childPath to toCommitValue(session, childPath, value)))
    }

    fun edit(block: EditorTransaction.() -> Unit) {
        val tx = EditorTransaction(session, path)
        tx.block()
        session.commitBatch(tx.pendingMutations)
    }
}

class ScalarCursor(session: EditorSession, path: ValuePath) : RyoCursor(session, path) {
    // 没有get？？？

    fun set(value: Any?) {
        val current = snapshot as? RyoScalarValue ?: error("Path '$path' is not a scalar")
        val ryoValue = session.runtime.scalarValue(current.typeRef, value)
        session.commitBatch(mapOf(path to ryoValue))
    }
}

class EditorTransaction(
    private val session: EditorSession,
    val basePath: ValuePath,
    // 💥 关键修复：把草稿纸设为构造参数，并且默认创建一个。
    // 这样 nested 创建的子事务就能共享同一张草稿纸了！
    internal val pendingMutations: LinkedHashMap<ValuePath, RyoValue?> = LinkedHashMap()
) {
    // 拦截直接赋值，进行动态装箱
    operator fun set(name: String, value: Any?) {
        val childPath = basePath.member(name)
        pendingMutations[childPath] = toCommitValue(session, childPath, value)
    }

    fun setRyo(name: String, value: RyoValue?) {
        pendingMutations[basePath.member(name)] = value
    }

    fun nested(name: String): EditorTransaction = EditorTransaction(session, basePath.member(name), pendingMutations)
}


// ==========================================
// 核心算法：批量结构共享合并
// ==========================================
private fun resolveValue(root: RyoValue?, path: ValuePath): RyoValue? {
    var current = root
    for (segment in path.segments) current = when (segment) {
        is MemberPathSegment -> (current as? RyoHostedValue)?.members?.get(segment.name)
        is IndexPathSegment -> (current as? RyoContainerValue)?.elements?.getOrNull(segment.index)
    } ?: return null
    return current
}

private fun batchReplace(root: RyoValue?, mutations: List<Pair<List<ValuePathSegment>, RyoValue?>>): RyoValue? {
    if (mutations.isEmpty()) return root

    val exactMatch = mutations.find { it.first.isEmpty() }
    if (exactMatch != null) return exactMatch.second

    if (root == null) error("Cannot traverse null tree")

    val groupedMutations = mutations.groupBy { it.first.first() }

    return when (root) {
        is RyoHostedValue -> {
            val newMembers = LinkedHashMap(root.members)
            for ((segment, childMutations) in groupedMutations) {
                val name = (segment as? MemberPathSegment)?.name ?: error("Path mismatch: expected member segment but got ${segment::class.simpleName}")
                val strippedMutations = childMutations.map { it.first.drop(1) to it.second }
                newMembers[name] = batchReplace(newMembers[name], strippedMutations)
            }
            root.copy(members = newMembers)
        }

        is RyoContainerValue -> {
            val newElements = root.elements.toMutableList()
            for ((segment, childMutations) in groupedMutations) {
                val index = (segment as? IndexPathSegment)?.index ?: error("Path mismatch: expected index segment but got ${segment::class.simpleName}")
                require(index in newElements.indices) { "Container index out of bounds: $index (size=${newElements.size})" }
                val strippedMutations = childMutations.map { it.first.drop(1) to it.second }
                newElements[index] = batchReplace(newElements.getOrNull(index), strippedMutations)
            }
            root.copy(elements = newElements)
        }

        is RyoScalarValue -> error("Mutations went deeper than scalar value. Path mismatch.")
        is RyoSpecialValue<*> -> error("Mutations went deeper than special value. Path mismatch.")
        is RyoUnknownValue -> error("Mutations went deeper than unknown value. Path mismatch.")
    }
}

private fun toCommitValue(session: EditorSession, path: ValuePath, value: Any?): RyoValue? {
    if (value is RyoValue) return value
    val currentChild = session.resolve(path)
    return if (currentChild is RyoScalarValue) session.runtime.scalarValue(currentChild.typeRef, value)
    else if (value != null) session.runtime.inferScalar(value) else null
}
