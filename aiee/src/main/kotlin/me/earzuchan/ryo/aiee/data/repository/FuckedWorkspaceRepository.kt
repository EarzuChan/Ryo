package me.earzuchan.ryo.aiee.data.repository

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.Job
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.collectLatest
import kotlinx.coroutines.launch
import kotlinx.coroutines.withContext
import me.earzuchan.ryo.aiee.util.OldPathUtils
import me.earzuchan.ryo.foundation.Ryo
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.io.loadVolumeChamberFrom
import me.earzuchan.ryo.foundation.io.writeTo
import me.earzuchan.ryo.foundation.schema.ModelSchema
import me.earzuchan.ryo.modern.modernize
import me.earzuchan.ryo.modern.volume.ManagedVolume
import okio.FileSystem
import okio.Path

class FuckedWorkspaceRepository(schemas: List<ModelSchema> = emptyList()) {
    data class VolumeState(val id: String, val name: String, val path: String?, val tokens: List<String>, val entryRevisions: Map<String, Long>)
    data class SchemaState(val modelId: String, val kind: ModelSchema.GloryKind, val members: List<String>)
    data class State(val volumes: List<VolumeState>, val activeVolumeId: String?, val schemas: List<SchemaState>, val lastError: String?)

    private data class VolumeRecord(val id: String, var path: Path?, var pathKey: String?, val chamber: VolumeChamber, val managed: ManagedVolume)

    private val fileSystem = FileSystem.SYSTEM
    private val writerDispatcher = Dispatchers.IO.limitedParallelism(1)
    private val repoScope = CoroutineScope(SupervisorJob() + writerDispatcher)
    private val foundation = Ryo { registerSchemas(schemas) }
    private val modern = foundation.modernize()
    private val schemaCatalog = schemas.toList()
    private val watcherJobs = mutableMapOf<String, Job>()
    private val records = linkedMapOf<String, VolumeRecord>()
    private val boundIdsByPathKey = mutableMapOf<String, String>()

    private var nextVolumeId = 1
    private var activeVolumeId: String? = null
    private var lastError: String? = null

    private val _state = MutableStateFlow(buildState())
    val state: StateFlow<State> = _state.asStateFlow()

    suspend fun openVolume(path: Path): Result<Unit> = runAction {
        val canonicalPath = OldPathUtils.canonicalVolumePath(path, fileSystem)
        val pathKey = OldPathUtils.volumePathKey(canonicalPath, fileSystem)

        boundIdsByPathKey[pathKey]?.also {
            activeVolumeId = it
            publish(clearError = true)
            return@runAction
        }

        val chamber = foundation.loadVolumeChamberFrom(canonicalPath, fileSystem)
        val record = VolumeRecord(
            id = "volume-${nextVolumeId++}",
            path = canonicalPath,
            pathKey = pathKey,
            chamber = chamber,
            managed = modern.manage(chamber)
        )

        records[record.id] = record
        boundIdsByPathKey[pathKey] = record.id
        ensureWatcher(record)
        activeVolumeId = record.id
        publish(clearError = true)
    }

    suspend fun saveVolume(volumeId: String): Result<Unit> = runAction {
        val record = requireRecord(volumeId)
        val path = requireNotNull(record.path) { "Volume has no path and must be saved as a new file" }
        ensureParentDirectory(path)
        record.chamber.writeTo(path, fileSystem)
        publish(clearError = true)
    }

    suspend fun saveVolumeAs(volumeId: String, path: Path): Result<Unit> = runAction {
        val record = requireRecord(volumeId)
        val canonicalPath = OldPathUtils.canonicalVolumePath(path, fileSystem)
        val pathKey = OldPathUtils.volumePathKey(canonicalPath, fileSystem)
        val existingId = boundIdsByPathKey[pathKey]

        require(existingId == null || existingId == volumeId) { "Volume already opened: $canonicalPath" }

        ensureParentDirectory(canonicalPath)
        record.chamber.writeTo(canonicalPath, fileSystem)

        record.pathKey?.let(boundIdsByPathKey::remove)
        record.path = canonicalPath
        record.pathKey = pathKey
        boundIdsByPathKey[pathKey] = volumeId
        publish(clearError = true)
    }

    suspend fun closeVolume(volumeId: String): Result<Unit> = runAction {
        val record = records.remove(volumeId) ?: error("Volume not found: $volumeId")
        record.pathKey?.let(boundIdsByPathKey::remove)
        watcherJobs.remove(volumeId)?.cancel()
        if (activeVolumeId == volumeId) activeVolumeId = null
        publish(clearError = true)
    }

    fun selectVolume(volumeId: String) {
        repoScope.launch {
            if (records.none { it.key == volumeId }) return@launch
            activeVolumeId = volumeId
            publish()
        }
    }

    fun close() = repoScope.cancel()

    private fun ensureWatcher(record: VolumeRecord) {
        if (record.id in watcherJobs) return
        watcherJobs[record.id] = repoScope.launch { record.managed.entries.collectLatest { publish() } }
    }

    private fun buildState(): State {
        val schemas = schemaCatalog.map { SchemaState(it.modelId, it.kind, it.members.map { member -> member.name }) }
        val volumes = records.values.map { record ->
            val revisions = record.managed.entries.value.toList().sortedBy { it.first }.toMap()
            VolumeState(record.id, record.path?.name ?: "Untitled volume", record.path?.toString(), revisions.keys.toList(), revisions)
        }
        val nextActive = activeVolumeId?.takeIf { target -> volumes.any { it.id == target } } ?: volumes.firstOrNull()?.id
        activeVolumeId = nextActive
        return State(volumes, nextActive, schemas, lastError)
    }

    private fun publish(clearError: Boolean = false) {
        if (clearError) lastError = null
        _state.value = buildState()
    }

    private suspend fun runAction(block: () -> Unit): Result<Unit> = withContext(writerDispatcher) {
        runCatching(block).onFailure {
            lastError = it.message ?: it::class.simpleName ?: "Unknown error"
            publish()
        }
    }

    private fun requireRecord(volumeId: String): VolumeRecord = records[volumeId] ?: error("Volume not found: $volumeId")

    private fun ensureParentDirectory(path: Path) {
        path.parent?.also(fileSystem::createDirectories)
    }
}
