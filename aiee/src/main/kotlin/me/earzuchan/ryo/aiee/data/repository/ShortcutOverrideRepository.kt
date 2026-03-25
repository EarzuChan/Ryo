package me.earzuchan.ryo.aiee.data.repository

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import me.earzuchan.ryo.aiee.data.dao.ShortcutOverrideDao
import me.earzuchan.ryo.aiee.data.database.ShortcutOverrideEntity
import me.earzuchan.ryo.aiee.duty.AppCommand
import me.earzuchan.ryo.aiee.duty.ShortcutDuty

class ShortcutOverrideRepository(private val dao: ShortcutOverrideDao) {
    fun observeSnapshot(): Flow<Map<AppCommand, ShortcutDuty.Stroke>> = dao.observeAll().map(::toSnapshot)

    suspend fun getSnapshot(): Map<AppCommand, ShortcutDuty.Stroke> = toSnapshot(dao.getAll())

    suspend fun upsert(command: AppCommand, stroke: ShortcutDuty.Stroke) {
        dao.upsert(
            ShortcutOverrideEntity(
                commandId = command.name,
                key = stroke.key.name,
                ctrl = stroke.ctrl,
                alt = stroke.alt,
                shift = stroke.shift,
                meta = stroke.meta
            )
        )
    }

    suspend fun delete(command: AppCommand) = dao.deleteByCommand(command.name)

    suspend fun clearAll() = dao.deleteAll()

    private fun toSnapshot(rows: List<ShortcutOverrideEntity>): Map<AppCommand, ShortcutDuty.Stroke> = buildMap {
        rows.forEach { row ->
            val command = runCatching { AppCommand.valueOf(row.commandId) }.getOrNull() ?: return@forEach
            val key = runCatching { ShortcutDuty.Key.valueOf(row.key) }.getOrNull() ?: return@forEach
            put(command, ShortcutDuty.Stroke(key = key, ctrl = row.ctrl, alt = row.alt, shift = row.shift, meta = row.meta))
        }
    }
}

