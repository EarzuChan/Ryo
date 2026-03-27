package me.earzuchan.ryo.modern.volume

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.stateIn
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.ModernizedRyo
import me.earzuchan.ryo.modern.session.EditorSession

// TODO：线程安全问题；卷（本身）克隆
class ManagedVolume internal constructor(private val modern: ModernizedRyo, val chamber: VolumeChamber) {
    fun markClean() {
        _isStructureDirty.value = false
        revisions.keys.forEach { revisions[it] = 0L } // 版本重置这一块
        publishEntries()
    }

    private val revisions = linkedMapOf<String, Long>().apply { chamber.tokens().forEach { put(it, 0L) } }
    private val _entries = MutableStateFlow(revisions.toMap())

    val entries = _entries.asStateFlow() // 可监听不可写，str_id to revision

    private val _isStructureDirty = MutableStateFlow(false)
    val isStructureDirty = _isStructureDirty.asStateFlow()

    val isDirty: Flow<Boolean> = combine(_isStructureDirty, _entries) { structDirty, revisions -> structDirty || revisions.values.any { it > 0 } }

    val tokens: Set<String> get() = _entries.value.keys

    fun contains(token: String): Boolean = token in revisions

    fun snapshot(token: String): RyoValue? = chamber.get(token)
    inline fun <reified T : RyoValue> snapshotAs(token: String): T? = snapshot(token) as? T
    inline fun <reified T : RyoValue> requireSnapshotAs(token: String): T = snapshotAs<T>(token) ?: error("Token '$token' is not ${T::class.simpleName ?: "expected type"}")

    fun checkout(token: String): EditorSession? = chamber.get(token)?.let { modern.newSession(it) }

    // 允许将裸值直接写入
    fun commit(token: String, value: RyoValue) {
        chamber.set(token, value)
        revisions[token] = (revisions[token] ?: 0L) + 1
        publishEntries()
    }

    fun commit(token: String, session: EditorSession) {
        val value = session.snapshot
        commit(token, value)
    }

    fun remove(token: String): Boolean {
        val removed = chamber.delete(token)
        if (removed) {
            _isStructureDirty.value = true // 删除是卷脏
            revisions.remove(token)
            publishEntries()
        }
        return removed
    }

    fun rename(oldToken: String, newToken: String, allowOverride: Boolean = false) {
        chamber.rename(oldToken, newToken, allowOverride)
        revisions[newToken] = (revisions.remove(oldToken) ?: 0L) + 1
        publishEntries()
    }

    fun clone(sourceToken: String, destToken: String, allowOverride: Boolean = false) {
        chamber.clone(sourceToken, destToken, allowOverride)
        revisions[destToken] = (revisions[destToken] ?: 0L) + 1
        publishEntries()
    }

    fun collectGarbage() {
        chamber.collectGarbage()
        _isStructureDirty.value = true // 压缩是卷脏
    }

    private fun publishEntries() {
        _entries.value = revisions.toMap()
    }
}
