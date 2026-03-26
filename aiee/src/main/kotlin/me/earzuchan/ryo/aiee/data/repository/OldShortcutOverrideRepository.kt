package me.earzuchan.ryo.aiee.data.repository

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import me.earzuchan.ryo.aiee.data.dao.OldShortcutOverrideDao
import me.earzuchan.ryo.aiee.data.database.OldShortcutOverrideEntity
import me.earzuchan.ryo.aiee.duty.OldShortcutDuty

class OldShortcutOverrideRepository(private val dao: OldShortcutOverrideDao) {
    fun observeSnapshot(): Flow<Map<String, OldShortcutDuty.Stroke>> = dao.observeAll().map(::toSnapshot)

    suspend fun getSnapshot(): Map<String, OldShortcutDuty.Stroke> = toSnapshot(dao.getAll())

    suspend fun upsert(commandId: String, stroke: OldShortcutDuty.Stroke) {
        dao.upsert(
            OldShortcutOverrideEntity(
                commandId = commandId,
                key = stroke.key.name,
                ctrl = stroke.ctrl,
                alt = stroke.alt,
                shift = stroke.shift,
                meta = stroke.meta
            )
        )
    }

    suspend fun delete(commandId: String) = dao.deleteByCommand(commandId)

    suspend fun clearAll() = dao.deleteAll()

    private fun toSnapshot(rows: List<OldShortcutOverrideEntity>): Map<String, OldShortcutDuty.Stroke> = buildMap {
        rows.forEach { row ->
            val key = runCatching { OldShortcutDuty.Key.valueOf(row.key) }.getOrNull() ?: return@forEach
            put(row.commandId, OldShortcutDuty.Stroke(key = key, ctrl = row.ctrl, alt = row.alt, shift = row.shift, meta = row.meta))
        }
    }
}
