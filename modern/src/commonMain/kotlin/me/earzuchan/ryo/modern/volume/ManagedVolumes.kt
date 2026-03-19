package me.earzuchan.ryo.modern.volume

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.ModernizedRyo
import me.earzuchan.ryo.modern.session.EditorSession

class ManagedVolume internal constructor(private val modern: ModernizedRyo, private val chamber: VolumeChamber) {
    private val revisions = linkedMapOf<String, Long>().apply { chamber.tokens().forEach { put(it, 0L) } }
    private val _entries = MutableStateFlow(revisions.toMap())

    val entries: StateFlow<Map<String, Long>> get() = _entries.asStateFlow() // 可监听不可写
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
        publish()
    }

    fun commit(token: String, session: EditorSession) {
        val value = session.extractFinalResult()
        commit(token, value)
    }

    fun remove(token: String): Boolean {
        val removed = chamber.delete(token)
        if (removed) {
            revisions.remove(token)
            publish()
        }
        return removed
    }

    fun rename(oldToken: String, newToken: String) {
        chamber.rename(oldToken, newToken)
        val revision = revisions.remove(oldToken) ?: 0L
        revisions[newToken] = revision
        publish()
    }

    fun clone(sourceToken: String, destToken: String, overwrite: Boolean = false) {
        chamber.clone(sourceToken, destToken, overwrite)
        revisions[destToken] = (revisions[destToken] ?: 0L) + 1
        publish()
    }

    fun collectGarbage() = chamber.collectGarbage()

    private fun publish() {
        _entries.value = revisions.toMap()
    }
}
