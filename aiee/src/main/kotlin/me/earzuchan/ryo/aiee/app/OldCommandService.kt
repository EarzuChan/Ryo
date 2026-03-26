package me.earzuchan.ryo.aiee.app

import androidx.compose.ui.input.key.KeyEvent
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.data.repository.OldShortcutOverrideRepository
import me.earzuchan.ryo.aiee.duty.OldShortcutDuty

// CHECK：这个设计得合理吗？

data class OldCommandAvailability(val enabled: Boolean, val reason: String? = null)

interface OldCommand {
    val id: String
    val defaultShortcut: OldShortcutDuty.Stroke?
    fun availability(ctx: OldCommandContext): OldCommandAvailability = OldCommandAvailability(true)
    suspend fun execute(ctx: OldCommandContext)
}

interface OldCommandContext {
    // CHECK：这个都列出来，会不会太招笑？

    fun hasActiveVolume(): Boolean
    suspend fun openVolumeByDialog()
    suspend fun saveActiveVolume()
    suspend fun saveActiveVolumeAsByDialog()
    suspend fun closeActiveVolume()

    fun openWelcomeTab()
    fun openEditorSessionTab()
    fun openSettingsTab()
    fun canRestoreClosedTab(): Boolean
    fun restoreLastClosedTab()
    fun hasActiveTab(): Boolean
    fun requestCloseCurrentTab()

    fun toggleSidePanel()
    fun focusAssetsPanel()
    fun focusSchemasPanel()

    fun isWindowMaximized(): Boolean
    fun toggleMaximizeWindow()
    fun requestWindowClose()

    fun canUndo(): Boolean
    fun undo()
    fun canRedo(): Boolean
    fun redo()
    fun canSaveTab(): Boolean
    fun saveTab()
    fun canDiscardTab(): Boolean
    fun discardTab()
}

// CHECK：如果命令们只是转发一下调用，那岂不是很废物？另外，命令既然是单例对象了，还要id作甚？

object OpenVolumeOldCommand : OldCommand {
    override val id = "workspace.open-volume"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.O, ctrl = true)
    override suspend fun execute(ctx: OldCommandContext) = ctx.openVolumeByDialog()
}

object SaveActiveVolumeOldCommand : OldCommand {
    override val id = "workspace.save-active-volume"
    override val defaultShortcut: OldShortcutDuty.Stroke? = null
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.hasActiveVolume())
    override suspend fun execute(ctx: OldCommandContext) = ctx.saveActiveVolume()
}

object SaveActiveVolumeAsOldCommand : OldCommand {
    override val id = "workspace.save-active-volume-as"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.S, ctrl = true, shift = true)
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.hasActiveVolume())
    override suspend fun execute(ctx: OldCommandContext) = ctx.saveActiveVolumeAsByDialog()
}

object CloseActiveVolumeOldCommand : OldCommand {
    override val id = "workspace.close-active-volume"
    override val defaultShortcut: OldShortcutDuty.Stroke? = null
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.hasActiveVolume())
    override suspend fun execute(ctx: OldCommandContext) = ctx.closeActiveVolume()
}

object OpenWelcomeTabOldCommand : OldCommand {
    override val id = "workspace.open-welcome-tab"
    override val defaultShortcut: OldShortcutDuty.Stroke? = null
    override suspend fun execute(ctx: OldCommandContext) = ctx.openWelcomeTab()
}

object OpenEditorSessionTabOldCommand : OldCommand {
    override val id = "workspace.open-editor-session-tab"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.N, ctrl = true)
    override suspend fun execute(ctx: OldCommandContext) = ctx.openEditorSessionTab()
}

object OpenSettingsTabOldCommand : OldCommand {
    override val id = "workspace.open-settings-tab"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.Comma, ctrl = true)
    override suspend fun execute(ctx: OldCommandContext) = ctx.openSettingsTab()
}

object RestoreClosedTabOldCommand : OldCommand {
    override val id = "workspace.restore-closed-tab"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.T, ctrl = true, shift = true)
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.canRestoreClosedTab())
    override suspend fun execute(ctx: OldCommandContext) = ctx.restoreLastClosedTab()
}

object CloseCurrentTabOldCommand : OldCommand {
    override val id = "workspace.close-current-tab"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.W, ctrl = true)
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.hasActiveTab())
    override suspend fun execute(ctx: OldCommandContext) = ctx.requestCloseCurrentTab()
}

object ToggleSidePanelOldCommand : OldCommand {
    override val id = "view.toggle-side-panel"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.B, ctrl = true)
    override suspend fun execute(ctx: OldCommandContext) = ctx.toggleSidePanel()
}

object FocusAssetsPanelOldCommand : OldCommand {
    override val id = "view.focus-assets-panel"
    override val defaultShortcut: OldShortcutDuty.Stroke? = null
    override suspend fun execute(ctx: OldCommandContext) = ctx.focusAssetsPanel()
}

object FocusSchemasPanelOldCommand : OldCommand {
    override val id = "view.focus-schemas-panel"
    override val defaultShortcut: OldShortcutDuty.Stroke? = null
    override suspend fun execute(ctx: OldCommandContext) = ctx.focusSchemasPanel()
}

object ToggleMaximizeWindowOldCommand : OldCommand {
    override val id = "window.toggle-maximize"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.F11)
    override suspend fun execute(ctx: OldCommandContext) = ctx.toggleMaximizeWindow()
}

object RequestWindowCloseOldCommand : OldCommand {
    override val id = "window.request-close"
    override val defaultShortcut: OldShortcutDuty.Stroke? = null
    override suspend fun execute(ctx: OldCommandContext) = ctx.requestWindowClose()
}

object UndoOldCommand : OldCommand {
    override val id = "editor.undo"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.Z, ctrl = true)
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.canUndo())
    override suspend fun execute(ctx: OldCommandContext) = ctx.undo()
}

object RedoOldCommand : OldCommand {
    override val id = "editor.redo"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.Y, ctrl = true)
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.canRedo())
    override suspend fun execute(ctx: OldCommandContext) = ctx.redo()
}

object SaveOldCommand : OldCommand {
    override val id = "editor.save"
    override val defaultShortcut = OldShortcutDuty.Stroke(OldShortcutDuty.Key.S, ctrl = true)
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.canSaveTab())
    override suspend fun execute(ctx: OldCommandContext) = ctx.saveTab()
}

object DiscardOldCommand : OldCommand {
    override val id = "editor.discard"
    override val defaultShortcut: OldShortcutDuty.Stroke? = null
    override fun availability(ctx: OldCommandContext) = OldCommandAvailability(ctx.canDiscardTab())
    override suspend fun execute(ctx: OldCommandContext) = ctx.discardTab()
}

object OldAppCommands {
    val all = listOf(
        OpenVolumeOldCommand,
        SaveActiveVolumeOldCommand,
        SaveActiveVolumeAsOldCommand,
        CloseActiveVolumeOldCommand,
        OpenWelcomeTabOldCommand,
        OpenEditorSessionTabOldCommand,
        OpenSettingsTabOldCommand,
        RestoreClosedTabOldCommand,
        CloseCurrentTabOldCommand,
        ToggleSidePanelOldCommand,
        FocusAssetsPanelOldCommand,
        FocusSchemasPanelOldCommand,
        ToggleMaximizeWindowOldCommand,
        RequestWindowCloseOldCommand,
        UndoOldCommand,
        RedoOldCommand,
        SaveOldCommand,
        DiscardOldCommand
    )
}

class OldCommandService(private val scope: CoroutineScope, private val context: OldCommandContext, private val shortcutOverrideRepo: OldShortcutOverrideRepository, oldCommands: List<OldCommand> = OldAppCommands.all) {
    private val commandsById = oldCommands.associateBy(OldCommand::id)
    private val oldShortcutDuty = OldShortcutDuty(oldCommands.mapNotNull { command -> command.defaultShortcut?.let { command.id to it } }.toMap())

    init {
        scope.launch { oldShortcutDuty.applyOverrideSnapshot(shortcutOverrideRepo.getSnapshot()) }
    }

    fun availability(oldCommand: OldCommand): OldCommandAvailability = oldCommand.availability(context)

    fun canExecute(oldCommand: OldCommand): Boolean = availability(oldCommand).enabled

    fun execute(oldCommand: OldCommand) {
        if (!canExecute(oldCommand)) return
        scope.launch { oldCommand.execute(context) }
    }

    fun shortcutFor(oldCommand: OldCommand) = oldShortcutDuty.effectiveStroke(oldCommand.id)

    fun setShortcutOverride(oldCommand: OldCommand, stroke: OldShortcutDuty.Stroke?): OldShortcutDuty.OverrideResult {
        val result = oldShortcutDuty.setOverride(oldCommand.id, stroke)
        if (result is OldShortcutDuty.OverrideResult.Accepted) scope.launch {
            if (stroke == null) shortcutOverrideRepo.delete(oldCommand.id) else shortcutOverrideRepo.upsert(oldCommand.id, stroke)
        }
        return result
    }

    fun clearShortcutOverride(oldCommand: OldCommand) {
        oldShortcutDuty.clearOverride(oldCommand.id)
        scope.launch { shortcutOverrideRepo.delete(oldCommand.id) }
    }

    fun shortcutOverrideSnapshot() = oldShortcutDuty.overrideSnapshot()

    fun handlePreviewKeyEvent(event: KeyEvent): Boolean {
        val commandId = oldShortcutDuty.resolve(event) ?: return false
        val command = commandsById[commandId] ?: return false
        if (!canExecute(command)) return false
        execute(command)
        return true
    }
}
