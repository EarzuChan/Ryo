package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.Composable
import androidx.compose.ui.input.key.KeyEvent
import androidx.compose.ui.unit.Density
import com.arkivanov.decompose.ComponentContext as DutyContext
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.app.DialogService
import me.earzuchan.ryo.aiee.app.MenuService
import me.earzuchan.ryo.aiee.app.OldCommand
import me.earzuchan.ryo.aiee.app.OldCommandContext
import me.earzuchan.ryo.aiee.app.OldCommandService
import me.earzuchan.ryo.aiee.app.OldCommandAvailability
import me.earzuchan.ryo.aiee.app.OldWorkspaceService
import me.earzuchan.ryo.aiee.data.repository.ShortcutRepository
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.dialog.AboutDialog
import me.earzuchan.ryo.aiee.ui.resolve
import org.koin.core.component.KoinComponent as KoinDuty
import org.koin.core.component.inject

// CHECK：还是太上帝了，很多权责难道不该移交给Services？
class OldAppDuty(ctx: DutyContext, private val exitApp: () -> Unit) : DutyContext by ctx, KoinDuty, OldCommandContext {
    private val dutyScope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private val shortcutOverrideRepo by inject<ShortcutRepository>()
    val oldWorkspaceService by inject<OldWorkspaceService>()
    val oldWorkspaceDuty = OldWorkspaceDuty(ctx, dutyScope, oldWorkspaceService)
    val mainWindowDuty = MainWindowDuty()
    val menuService by inject<MenuService>()
    val dialogService by inject<DialogService>()
    val oldCommandService = OldCommandService(dutyScope, this)

    val sidePanelDuty get() = oldWorkspaceDuty.sidePanelDuty
    val windowInterop get() = mainWindowDuty.windowInterop
    val isMaximized get() = mainWindowDuty.isMaximized
    val lifecycleStage get() = mainWindowDuty.lifecycleStage

    val tabs get() = oldWorkspaceDuty.tabs
    val activeTabId get() = oldWorkspaceDuty.activeTabId
    val activeTab get() = oldWorkspaceDuty.activeTab
    val hasDirtyTabs get() = oldWorkspaceDuty.hasDirtyTabs

    val windowTitle @Composable get() = BuildConfig.APP_NAME.let { activeTab?.let { t -> "${t.title.resolve()} - $it" } ?: it }

    fun selectTab(tabId: String) = oldWorkspaceDuty.selectTab(tabId)

    fun requestCloseTab(id: String) {
        val tab = oldWorkspaceDuty.findTab(id) ?: return
        if (!tab.dirty) {
            oldWorkspaceDuty.closeTab(id)
            return
        }

        dialogService.orderCommon(
            headline = UiText.Res(Res.string.dialog_close_unsaved_title),
            description = UiText.Res(Res.string.dialog_close_unsaved_description_format, listOf(tab.title)),
            actions = listOf(
                DialogService.DialogAction(UiText.Res(Res.string.action_cancel)),
                DialogService.DialogAction(UiText.Res(Res.string.action_close_tab)) { oldWorkspaceDuty.closeTab(id); true }
            )
        )
    }

    fun closeTab(id: String) = oldWorkspaceDuty.closeTab(id)

    fun closeCurrentTab() = activeTabId?.also(::requestCloseTab)

    fun requestCloseOtherTabs(tabId: String) = oldWorkspaceDuty.requestCloseOtherTabs(tabId, ::requestCloseTab)

    fun requestCloseAllTabs() = oldWorkspaceDuty.requestCloseAllTabs(::requestCloseTab)

    fun moveTab(fromIndex: Int, toIndex: Int) = oldWorkspaceDuty.moveTab(fromIndex, toIndex)

    fun requestClose() {
        if (lifecycleStage == AppLifecycleStage.Closing) return
        mainWindowDuty.markClosing()

        dialogService.orderCommon(
            icon = Res.drawable.ic_ryo_24px,
            headline = UiText.Res(Res.string.dialog_close_app_title),
            description = if (hasDirtyTabs) UiText.Res(Res.string.dialog_close_app_description_dirty) else UiText.Res(Res.string.dialog_close_app_description_clean),
            actions = listOf(
                DialogService.DialogAction(UiText.Res(Res.string.action_cancel)) { cancelClose(); true },
                DialogService.DialogAction(UiText.Res(Res.string.action_exit)) { confirmClose(); true }
            )
        )
    }

    fun cancelClose() = mainWindowDuty.cancelClose()

    fun confirmClose() {
        mainWindowDuty.markClosing()
        dutyScope.cancel()
        exitApp()
    }

    fun minimizeWindow() = mainWindowDuty.minimizeWindow()

    override fun toggleMaximizeWindow() {
        mainWindowDuty.toggleMaximizeWindow()
    }

    override fun requestWindowClose() {
        mainWindowDuty.requestWindowClose(::requestClose)
    }

    override fun openWelcomeTab() {
        oldWorkspaceDuty.openWelcomeTab()
    }

    override fun openEditorSessionTab() {
        oldWorkspaceDuty.openEditorSessionTab()
    }

    override fun openSettingsTab() {
        oldWorkspaceDuty.openSettingsTab()
    }

    override fun restoreLastClosedTab() {
        oldWorkspaceDuty.restoreLastClosedTab()
    }

    fun executeCommand(oldCommand: OldCommand) = oldCommandService.execute(oldCommand)

    fun commandAvailability(oldCommand: OldCommand): OldCommandAvailability = oldCommandService.availability(oldCommand)

    fun canExecuteCommand(oldCommand: OldCommand): Boolean = oldCommandService.canExecute(oldCommand)

    fun shortcutFor(oldCommand: OldCommand) = oldCommandService.shortcutFor(oldCommand)

    fun clearShortcutOverride(oldCommand: OldCommand) = oldCommandService.clearShortcutOverride(oldCommand)

    fun shortcutOverrideSnapshot() = oldCommandService.shortcutOverrideSnapshot()

    fun handlePreviewKeyEvent(event: KeyEvent): Boolean = oldCommandService.handlePreviewKeyEvent(event)

    fun toggleMenuGroup(groupId: String, anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) = menuService.toggleMenuBarGroup(groupId, anchorX, anchorY, entries)

    fun hoverMenuGroup(groupId: String, anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) = menuService.hoverMenuBarGroup(groupId, anchorX, anchorY, entries)

    fun dismissMenu() = menuService.dismiss()

    fun showContextMenu(anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) = menuService.showContextMenu(anchorX, anchorY, entries)

    fun showInPlaceSelectMenu(textX: Int, textCenterY: Int, selectedIndex: Int, density: Density, entries: List<RyoMenuEntry>) = menuService.showInPlaceSelectMenu(textX, textCenterY, selectedIndex, density, entries)

    override fun hasActiveVolume(): Boolean = oldWorkspaceService.state.value.activeVolumeId != null

    override suspend fun openVolumeByDialog() {
        oldWorkspaceService.openVolumeByDialog()
    }

    override suspend fun saveActiveVolume() {
        oldWorkspaceService.saveActiveVolume()
    }

    override suspend fun saveActiveVolumeAsByDialog() {
        oldWorkspaceService.saveActiveVolumeAsByDialog()
    }

    override suspend fun closeActiveVolume() {
        oldWorkspaceService.closeActiveVolume()
    }

    override fun canRestoreClosedTab(): Boolean = oldWorkspaceDuty.canRestoreClosedTab

    override fun hasActiveTab(): Boolean = activeTabId != null

    override fun isWindowMaximized(): Boolean = isMaximized

    override fun canUndo(): Boolean = oldWorkspaceDuty.activeOldTabDuty?.canUndo() == true

    override fun canRedo(): Boolean = oldWorkspaceDuty.activeOldTabDuty?.canRedo() == true

    override fun canSaveTab(): Boolean = oldWorkspaceDuty.activeOldTabDuty?.canSave() == true

    override fun canDiscardTab(): Boolean = oldWorkspaceDuty.activeOldTabDuty?.canDiscard() == true

    override fun requestCloseCurrentTab() {
        closeCurrentTab()
    }

    override fun toggleSidePanel() {
        sidePanelDuty.toggleExpanded()
    }

    override fun focusAssetsPanel() {
        sidePanelDuty.focusPanel(SidePanelNavis.Assets.id)
    }

    override fun focusSchemasPanel() {
        sidePanelDuty.focusPanel(SidePanelNavis.Schemas.id)
    }

    override fun undo() = oldWorkspaceDuty.executeOnActiveTab(OldTabDuty::undo)

    override fun redo() = oldWorkspaceDuty.executeOnActiveTab(OldTabDuty::redo)

    override fun saveTab() = oldWorkspaceDuty.executeOnActiveTab(OldTabDuty::save)

    override fun discardTab() = oldWorkspaceDuty.executeOnActiveTab(OldTabDuty::discard)
}
