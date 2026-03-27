package me.earzuchan.ryo.aiee.data.repository

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import me.earzuchan.ryo.aiee.app.ShortcutService
import me.earzuchan.ryo.aiee.data.dao.ShortcutOverrideDao
import me.earzuchan.ryo.aiee.data.dao.toEntity

class ShortcutRepository(private val dao: ShortcutOverrideDao) {
    val overridesFlow: Flow<Map<String, ShortcutService.Stroke>> = dao.observeAll().map { l -> l.associate { it.commandId to it.toStroke() } }

    suspend fun saveOverride(commandId: String, stroke: ShortcutService.Stroke) = dao.insert(stroke.toEntity(commandId))

    suspend fun removeOverride(commandId: String) = dao.deleteByCommandId(commandId)
}