package me.earzuchan.ryo.aiee.data.workspace

import kotlinx.coroutines.flow.StateFlow
import me.earzuchan.ryo.foundation.Ryo
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.io.loadVolumeChamberFrom
import me.earzuchan.ryo.foundation.io.writeTo
import me.earzuchan.ryo.foundation.schema.ModelSchema
import me.earzuchan.ryo.modern.modernize
import me.earzuchan.ryo.modern.volume.ManagedVolume
import okio.Path
import okio.Path.Companion.toPath

class WorkspaceRuntime(schemas: List<ModelSchema> = emptyList()) {
    data class VolumeHandle(val id: String, val name: String, val path: Path, val entryRevisions: Map<String, Long>) {
        val tokens: List<String> get() = entryRevisions.keys.toList()
    }

    private data class VolumeRecord(val id: String, val path: Path, val chamber: VolumeChamber, val managed: ManagedVolume)

    private val foundation = Ryo { registerSchemas(schemas) }
    private val modern = foundation.modernize()
    private val records = linkedMapOf<String, VolumeRecord>()

    val schemaCatalog: List<ModelSchema> = schemas.toList()

    fun openedVolumes(): List<VolumeHandle> = records.values.map(::toHandle)

    fun openVolume(path: Path): VolumeHandle {
        val id = path.toString()
        records[id]?.also { return toHandle(it) }

        val normalizedPath = id.toPath()
        val chamber = foundation.loadVolumeChamberFrom(normalizedPath)
        val managed = modern.manage(chamber)
        val record = VolumeRecord(id, normalizedPath, chamber, managed)
        records[id] = record
        return toHandle(record)
    }

    fun closeVolume(id: String): Boolean = records.remove(id) != null

    fun saveVolume(id: String): Boolean = records[id]?.let {
        it.chamber.writeTo(it.path)
        true
    } ?: false

    fun entryRevisions(id: String): StateFlow<Map<String, Long>>? = records[id]?.managed?.entries

    private fun toHandle(record: VolumeRecord): VolumeHandle = VolumeHandle(record.id, record.path.name, record.path, record.managed.entries.value.toMap())
}
