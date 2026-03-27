package me.earzuchan.ryo.modern.session

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import me.earzuchan.ryo.foundation.path.RyoPath
import me.earzuchan.ryo.foundation.path.RyoPathSegment
import me.earzuchan.ryo.foundation.value.RyoContainerValue
import me.earzuchan.ryo.foundation.value.RyoHostedValue
import me.earzuchan.ryo.foundation.value.RyoNullValue
import me.earzuchan.ryo.foundation.value.RyoScalarValue
import me.earzuchan.ryo.foundation.value.RyoSpecialValue
import me.earzuchan.ryo.foundation.value.RyoUnknownValue
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.ModernRuntime
import kotlin.collections.iterator

class EditorSession internal constructor(internal val runtime: ModernRuntime, initialSnapshot: RyoValue) {
    private var currentRoot: RyoValue = initialSnapshot
    private val history = mutableListOf<RyoValue>().apply { add(initialSnapshot) }
    private var cursorIndex = 0

    // KMP 轻量线程安全映射
    private val registryLock = Any()
    private var flowRegistry: Map<RyoPath, MutableStateFlow<RyoValue?>> = emptyMap()

    fun observe(path: RyoPath): StateFlow<RyoValue?> {
        flowRegistry[path]?.let { return it.asStateFlow() }

        synchronized(registryLock) {
            flowRegistry[path]?.let { return it.asStateFlow() }
            val newFlow = MutableStateFlow(resolveValue(currentRoot, path))
            flowRegistry = flowRegistry + (path to newFlow)
            return newFlow.asStateFlow()
        }
    }

    fun resolve(path: RyoPath): RyoValue? = resolveValue(currentRoot, path)

    internal fun commitBatch(mutations: Map<RyoPath, RyoValue>) {
        if (mutations.isEmpty()) return

        val flattenedMutations = mutations.map { it.key.segments to it.value }
        currentRoot = batchReplace(currentRoot, flattenedMutations)

        if (cursorIndex < history.lastIndex) history.subList(cursorIndex + 1, history.size).clear()
        history.add(currentRoot)
        cursorIndex = history.lastIndex
        if (history.size > 128) {
            history.removeAt(0)
            cursorIndex--
        }

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

    val rootCursor: RyoCursor get() = cursorAt(RyoPath.ROOT)
    inline fun <reified T : RyoCursor> getRootCursorAs(): T = rootCursor as? T ?: error("Root cursor is ${rootCursor::class.simpleName}, expected ${T::class.simpleName}")

    val snapshot: RyoValue get() = currentRoot

    private fun broadcastAll() = flowRegistry.forEach { (path, flow) -> flow.value = resolveValue(currentRoot, path) }

    private fun cursorAt(path: RyoPath): RyoCursor = when (resolve(path)) {
        is RyoHostedValue -> HostedCursor(this, path)
        is RyoContainerValue -> ContainerCursor(this, path)
        else -> ScalarCursor(this, path)
    }
}

sealed class RyoCursor(val session: EditorSession, val path: RyoPath) {
    val stateFlow: StateFlow<RyoValue?> get() = session.observe(path) // CHECK：有人说这里改用`by lazy`而不是`get()`
    val snapshot: RyoValue? get() = session.resolve(path)

    fun asHosted() = this as HostedCursor
    fun asContainer() = this as ContainerCursor
    fun asScalar() = this as ScalarCursor
}

class HostedCursor(session: EditorSession, path: RyoPath) : RyoCursor(session, path) {
    operator fun get(name: String): RyoCursor {
        val childPath = path.member(name)
        val snapshot = session.resolve(childPath)
        return when (snapshot) {
            is RyoHostedValue -> HostedCursor(session, childPath)
            is RyoContainerValue -> ContainerCursor(session, childPath)
            else -> ScalarCursor(session, childPath)
        }
    }

    operator fun set(name: String, value: Any?) {
        val childPath = path.member(name)
        session.commitBatch(mapOf(childPath to toCommitValue(session, childPath, value)))
    }

    fun edit(block: EditorTransaction.() -> Unit) {
        val tx = EditorTransaction(session, path)
        tx.block()
        session.commitBatch(tx.pendingMutations)
    }
}

class ContainerCursor(session: EditorSession, path: RyoPath) : RyoCursor(session, path) {
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

class ScalarCursor(session: EditorSession, path: RyoPath) : RyoCursor(session, path) {
    fun set(value: Any?) {
        val current = snapshot as? RyoScalarValue ?: error("Path '$path' is not a scalar")
        val ryoValue = if (value == null) session.runtime.nullValue(current.typeRef) else session.runtime.scalarValue(current.typeRef, value)
        session.commitBatch(mapOf(path to ryoValue))
    }
}

class EditorTransaction(
    private val session: EditorSession,
    val basePath: RyoPath,
    internal val pendingMutations: LinkedHashMap<RyoPath, RyoValue> = LinkedHashMap()
) {
    operator fun set(name: String, value: Any?) {
        val childPath = basePath.member(name)
        pendingMutations[childPath] = toCommitValue(session, childPath, value)
    }

    fun setRyo(name: String, value: RyoValue) {
        pendingMutations[basePath.member(name)] = value
    }

    fun nested(name: String): EditorTransaction = EditorTransaction(session, basePath.member(name), pendingMutations)
}

private fun resolveValue(root: RyoValue?, path: RyoPath): RyoValue? {
    var current = root
    for (segment in path.segments) current = when (segment) {
        is RyoPathSegment.Member -> (current as? RyoHostedValue)?.members?.get(segment.name)
        is RyoPathSegment.Index -> (current as? RyoContainerValue)?.elements?.getOrNull(segment.index)
    } ?: return null
    return current
}

private fun batchReplace(root: RyoValue, mutations: List<Pair<List<RyoPathSegment>, RyoValue>>): RyoValue {
    if (mutations.isEmpty()) return root

    val exactMatch = mutations.find { it.first.isEmpty() }
    if (exactMatch != null) return exactMatch.second

    val groupedMutations = mutations.groupBy { it.first.first() }

    return when (root) {
        is RyoHostedValue -> {
            val newMembers = LinkedHashMap(root.members)
            for ((segment, childMutations) in groupedMutations) {
                val name = (segment as? RyoPathSegment.Member)?.name ?: error("Path mismatch: expected member segment but got ${segment::class.simpleName}")
                val strippedMutations = childMutations.map { it.first.drop(1) to it.second }
                val current = newMembers[name] ?: error("Path mismatch: member '$name' not found")
                newMembers[name] = batchReplace(current, strippedMutations)
            }
            root.copy(members = newMembers)
        }

        is RyoContainerValue -> {
            val newElements = root.elements.toMutableList()
            for ((segment, childMutations) in groupedMutations) {
                val index = (segment as? RyoPathSegment.Index)?.index ?: error("Path mismatch: expected index segment but got ${segment::class.simpleName}")
                require(index in newElements.indices) { "Container index out of bounds: $index (size=${newElements.size})" }
                val strippedMutations = childMutations.map { it.first.drop(1) to it.second }
                newElements[index] = batchReplace(newElements[index], strippedMutations)
            }
            root.copy(elements = newElements)
        }

        is RyoScalarValue -> error("Mutations went deeper than scalar value. Path mismatch.")
        is RyoNullValue -> error("Mutations went deeper than null value. Path mismatch.")
        is RyoSpecialValue<*> -> error("Mutations went deeper than special value. Path mismatch.")
        is RyoUnknownValue -> error("Mutations went deeper than unknown value. Path mismatch.")
    }
}

private fun toCommitValue(session: EditorSession, path: RyoPath, value: Any?): RyoValue {
    if (value is RyoValue) return value

    val currentChild = session.resolve(path)
    if (value == null) return session.runtime.nullValue(currentChild?.typeRef)

    return if (currentChild is RyoScalarValue) session.runtime.scalarValue(currentChild.typeRef, value) else session.runtime.inferScalar(value)
}
