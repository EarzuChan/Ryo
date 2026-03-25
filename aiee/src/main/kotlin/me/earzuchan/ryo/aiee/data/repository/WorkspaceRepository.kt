package me.earzuchan.ryo.aiee.data.repository

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.data.workspace.WorkspaceRuntime
import me.earzuchan.ryo.foundation.schema.ModelSchema
import okio.Path

class WorkspaceRepository(private val runtime: WorkspaceRuntime) {
    data class VolumeState(val id: String, val name: String, val path: String, val tokens: List<String>, val entryRevisions: Map<String, Long>)
    data class SchemaState(val modelId: String, val kind: ModelSchema.GloryKind, val members: List<String>)
    data class State(val volumes: List<VolumeState>, val activeVolumeId: String?, val schemas: List<SchemaState>, val lastError: String?)

    private val repoScope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
    private val watcherJobs = mutableMapOf<String, Job>()

    private var activeVolumeId: String? = null
    private var lastError: String? = null

    private val _state = MutableStateFlow(buildState(lastError = null))
    val state: StateFlow<State> = _state.asStateFlow()

    fun openVolume(path: Path): Result<Unit> = runAction {
        val opened = runtime.openVolume(path)
        ensureWatcher(opened.id)
        activeVolumeId = opened.id
        publish(clearError = true)
    }

    fun saveVolume(volumeId: String): Result<Unit> = runAction {
        check(runtime.saveVolume(volumeId)) { "Volume not found: $volumeId" }
        publish(clearError = true)
    }

    fun closeVolume(volumeId: String): Result<Unit> = runAction {
        check(runtime.closeVolume(volumeId)) { "Volume not found: $volumeId" }
        watcherJobs.remove(volumeId)?.cancel()
        if (activeVolumeId == volumeId) activeVolumeId = null
        publish(clearError = true)
    }

    fun saveActiveVolume(): Result<Unit> = state.value.activeVolumeId?.let(::saveVolume) ?: Result.failure(IllegalStateException("No active volume"))

    fun closeActiveVolume(): Result<Unit> = state.value.activeVolumeId?.let(::closeVolume) ?: Result.failure(IllegalStateException("No active volume"))

    fun selectVolume(volumeId: String) {
        if (state.value.volumes.none { it.id == volumeId }) return
        activeVolumeId = volumeId
        publish()
    }

    private fun ensureWatcher(volumeId: String) {
        if (volumeId in watcherJobs) return
        val flow = runtime.entryRevisions(volumeId) ?: return
        watcherJobs[volumeId] = repoScope.launch { flow.collectLatest { publish() } }
    }

    private fun buildState(lastError: String?): State {
        val schemas = runtime.schemaCatalog.map { SchemaState(it.modelId, it.kind, it.members.map { member -> member.name }) }
        val volumes = runtime.openedVolumes().map { handle ->
            val revisions = handle.entryRevisions.toList().sortedBy { it.first }.toMap()
            VolumeState(handle.id, handle.name, handle.path.toString(), revisions.keys.toList(), revisions)
        }
        val nextActive = activeVolumeId?.takeIf { target -> volumes.any { it.id == target } } ?: volumes.firstOrNull()?.id
        activeVolumeId = nextActive
        return State(volumes, nextActive, schemas, lastError)
    }

    private fun publish(clearError: Boolean = false) {
        if (clearError) lastError = null
        _state.value = buildState(lastError)
    }

    private inline fun runAction(block: () -> Unit): Result<Unit> = runCatching(block).onFailure {
        lastError = it.message ?: it::class.simpleName ?: "Unknown error"
        publish()
    }
}