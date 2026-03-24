package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.Composable
import androidx.compose.ui.input.key.KeyEvent
import androidx.compose.ui.unit.Density
import com.arkivanov.decompose.ComponentContext as DutyContext
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences
import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.dialog.AboutDialog
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.resolve
import me.earzuchan.ryo.aiee.BuildConfig
import org.koin.core.component.KoinComponent as KoinDuty
import org.koin.core.component.inject

class AppDuty(ctx: DutyContext, private val exitApp: () -> Unit) : DutyContext by ctx, KoinDuty {
    private val dutyScope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private val prefsRepo: RyoPreferencesRepository by inject()

    val preferencesDuty = PreferencesDuty(dutyScope, prefsRepo)
    val workspaceDuty = WorkspaceDuty(ctx)
    val mainWindowDuty = MainWindowDuty()
    val menuDuty = MenuDuty()
    val dialogDuty = DialogDuty()

    val commandDuty = CommandDuty()
    val shortcutDuty = ShortcutDuty(defaultShortcutBindings())

    val forceDarkMode get() = preferencesDuty.forceDarkMode
    val appThemeMode get() = preferencesDuty.appThemeMode
    val appLanguage get() = preferencesDuty.appLanguage

    val sidePanelDuty get() = workspaceDuty.sidePanelDuty
    val windowInterop get() = mainWindowDuty.windowInterop
    val isMaximized get() = mainWindowDuty.isMaximized
    val lifecycleStage get() = mainWindowDuty.lifecycleStage

    val tabs get() = workspaceDuty.tabs
    val activeTabId get() = workspaceDuty.activeTabId
    val activeTab get() = workspaceDuty.activeTab
    val hasDirtyTabs get() = workspaceDuty.hasDirtyTabs

    val windowTitle @Composable get() = BuildConfig.APP_NAME.let { activeTab?.let { t -> "${t.title.resolve()} - $it" } ?: it }

    init {
        registerCommands()
    }

    fun setAppThemeMode(mode: RyoPreferences.ThemeMode) = preferencesDuty.setAppThemeMode(mode)

    fun setAppLanguage(lang: RyoPreferences.Language) = preferencesDuty.setAppLanguage(lang)

    fun selectTab(tabId: String) = workspaceDuty.selectTab(tabId)

    fun requestCloseTab(id: String) {
        val tab = workspaceDuty.findTab(id) ?: return
        if (!tab.dirty) {
            workspaceDuty.closeTab(id)
            return
        }

        dialogDuty.orderCommon(
            headline = UiText.Res(Res.string.dialog_close_unsaved_title), description = UiText.Res(Res.string.dialog_close_unsaved_description_format, listOf(tab.title)), actions = listOf(
                DialogDuty.DialogAction(UiText.Res(Res.string.action_cancel)), DialogDuty.DialogAction(UiText.Res(Res.string.action_close_tab)) { workspaceDuty.closeTab(id); true })
        )
    }

    fun closeTab(id: String) = workspaceDuty.closeTab(id)

    fun closeCurrentTab() = activeTabId?.also(::requestCloseTab)

    fun requestCloseOtherTabs(tabId: String) = workspaceDuty.requestCloseOtherTabs(tabId, ::requestCloseTab)

    fun requestCloseAllTabs() = workspaceDuty.requestCloseAllTabs(::requestCloseTab)

    fun moveTab(fromIndex: Int, toIndex: Int) = workspaceDuty.moveTab(fromIndex, toIndex)

    fun requestClose() {
        if (lifecycleStage == AppLifecycleStage.Closing) return
        mainWindowDuty.markClosing()

        dialogDuty.orderCommon(
            icon = Res.drawable.ic_ryo_24px,
            headline = UiText.Res(Res.string.dialog_close_app_title),
            description = if (hasDirtyTabs) UiText.Res(Res.string.dialog_close_app_description_dirty) else UiText.Res(Res.string.dialog_close_app_description_clean),
            actions = listOf(DialogDuty.DialogAction(UiText.Res(Res.string.action_cancel)) { cancelClose(); true }, DialogDuty.DialogAction(UiText.Res(Res.string.action_exit)) { confirmClose(); true })
        )
    }

    fun cancelClose() = mainWindowDuty.cancelClose()

    fun confirmClose() {
        mainWindowDuty.markClosing()
        dutyScope.cancel()
        exitApp()
    }

    fun minimizeWindow() = mainWindowDuty.minimizeWindow()

    fun toggleMaximizeWindow() = mainWindowDuty.toggleMaximizeWindow()

    fun requestWindowClose() = mainWindowDuty.requestWindowClose(::requestClose)

    fun openWelcomeTab() = workspaceDuty.openWelcomeTab()

    fun openEditorSessionTab() = workspaceDuty.openEditorSessionTab()

    fun openSettingsTab() = workspaceDuty.openSettingsTab()

    fun restoreLastClosedTab() = workspaceDuty.restoreLastClosedTab()

    fun showAboutDialog() = dialogDuty.orderSpecial(closeOnOverlayClick = true) { _ -> AboutDialog() }

    fun executeCommand(command: AppCommand) = commandDuty.execute(command)

    fun shortcutFor(command: AppCommand) = shortcutDuty.effectiveStroke(command)

    fun setShortcutOverride(command: AppCommand, stroke: ShortcutDuty.Stroke?) = shortcutDuty.setOverride(command, stroke)

    fun clearShortcutOverride(command: AppCommand) = shortcutDuty.clearOverride(command)

    fun shortcutOverrideSnapshot() = shortcutDuty.overrideSnapshot()

    fun handlePreviewKeyEvent(event: KeyEvent): Boolean {
        val command = shortcutDuty.resolve(event) ?: return false
        if (!commandDuty.canExecute(command)) return false
        commandDuty.execute(command)
        return true
    }

    fun toggleMenuGroup(groupId: String, anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) = menuDuty.toggleMenuBarGroup(groupId, anchorX, anchorY, entries)

    fun hoverMenuGroup(groupId: String, anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) = menuDuty.hoverMenuBarGroup(groupId, anchorX, anchorY, entries)

    fun dismissMenu() = menuDuty.dismiss()

    fun showContextMenu(anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) = menuDuty.showContextMenu(anchorX, anchorY, entries)

    fun showInPlaceSelectMenu(textX: Int, textCenterY: Int, selectedIndex: Int, density: Density, entries: List<RyoMenuEntry>) = menuDuty.showInPlaceSelectMenu(textX, textCenterY, selectedIndex, density, entries)

    private fun registerCommands() {
        commandDuty.register(AppCommand.OpenWelcomeTab) { workspaceDuty.openWelcomeTab() }
        commandDuty.register(AppCommand.OpenEditorSessionTab) { workspaceDuty.openEditorSessionTab() }
        commandDuty.register(AppCommand.OpenSettingsTab) { workspaceDuty.openSettingsTab() }
        commandDuty.register(AppCommand.RestoreClosedTab, canExecute = { workspaceDuty.canRestoreClosedTab }) { workspaceDuty.restoreLastClosedTab() }
        commandDuty.register(AppCommand.CloseCurrentTab, canExecute = { activeTabId != null }) { closeCurrentTab() }
        commandDuty.register(AppCommand.ToggleSidePanel) { sidePanelDuty.toggleExpanded() }
        commandDuty.register(AppCommand.FocusAssetsPanel) { sidePanelDuty.focusPanel("assets") }
        commandDuty.register(AppCommand.FocusSchemasPanel) { sidePanelDuty.focusPanel("schemas") }
        commandDuty.register(AppCommand.ToggleMaximizeWindow) { toggleMaximizeWindow() }
        commandDuty.register(AppCommand.RequestWindowClose) { requestWindowClose() }
        commandDuty.register(AppCommand.Undo, canExecute = { workspaceDuty.activeTabDuty?.canUndo() == true }) { workspaceDuty.executeOnActiveTab(TabDuty::undo) }
        commandDuty.register(AppCommand.Redo, canExecute = { workspaceDuty.activeTabDuty?.canRedo() == true }) { workspaceDuty.executeOnActiveTab(TabDuty::redo) }
        commandDuty.register(AppCommand.Save, canExecute = { workspaceDuty.activeTabDuty?.canSave() == true }) { workspaceDuty.executeOnActiveTab(TabDuty::save) }
        commandDuty.register(AppCommand.Discard, canExecute = { workspaceDuty.activeTabDuty?.canDiscard() == true }) { workspaceDuty.executeOnActiveTab(TabDuty::discard) }
    }

    private fun defaultShortcutBindings() = mapOf(
        AppCommand.OpenEditorSessionTab to ShortcutDuty.Stroke(ShortcutDuty.Key.N, ctrl = true),
        AppCommand.OpenSettingsTab to ShortcutDuty.Stroke(ShortcutDuty.Key.Comma, ctrl = true),
        AppCommand.RestoreClosedTab to ShortcutDuty.Stroke(ShortcutDuty.Key.T, ctrl = true, shift = true),
        AppCommand.CloseCurrentTab to ShortcutDuty.Stroke(ShortcutDuty.Key.W, ctrl = true),
        AppCommand.Save to ShortcutDuty.Stroke(ShortcutDuty.Key.S, ctrl = true),
        AppCommand.Undo to ShortcutDuty.Stroke(ShortcutDuty.Key.Z, ctrl = true),
        AppCommand.Redo to ShortcutDuty.Stroke(ShortcutDuty.Key.Y, ctrl = true),
        AppCommand.ToggleSidePanel to ShortcutDuty.Stroke(ShortcutDuty.Key.B, ctrl = true),
        AppCommand.ToggleMaximizeWindow to ShortcutDuty.Stroke(ShortcutDuty.Key.F11)
    )
}
