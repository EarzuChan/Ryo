package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.input.key.KeyEvent
import androidx.compose.ui.unit.Density
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.collect
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import kotlinx.coroutines.runBlocking
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences.ThemeMode.DARK
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences.ThemeMode.LIGHT
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences.ThemeMode.SYSTEM
import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.dialog.AboutDialog
import me.earzuchan.ryo.aiee.ui.window.RyoWindowController
import me.earzuchan.ryo.aiee.ui.window.RyoWindowInterop
import me.earzuchan.ryo.aiee.util.AppLocaleUtils
import me.earzuchan.ryo.aiee.util.ResUtils.text
import org.jetbrains.compose.resources.StringResource
import org.jetbrains.compose.resources.getString
import com.arkivanov.decompose.ComponentContext as DutyContext
import org.koin.core.component.KoinComponent as KoinDuty
import org.koin.core.component.inject

class AppDuty(ctx: DutyContext, private val exitApp: () -> Unit) : DutyContext by ctx, KoinDuty {
    data class MenuGroup(val id: String, val label: String, val items: List<RyoMenuEntry>)

    private val tabHostDuty = TabHostDuty(TabDutyFactory())
    private val dutyScope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private val prefsRepo: RyoPreferencesRepository by inject()
    private var editorSessionSeed = 3

    val sideWorkspaceDuty = SideWorkspaceDuty(ctx)
    val menuDuty = MenuDuty()
    val dialogDuty = DialogDuty()
    val commandDuty = CommandDuty()
    val shortcutDuty = ShortcutDuty(defaultShortcutBindings())

    private var windowController: RyoWindowController? = null

    var lifecycleStage by mutableStateOf(AppLifecycleStage.Running)

    var isMaximized by mutableStateOf(false); private set

    val forceDarkMode = prefsRepo.themeModeFlow().map {
        when (it) {
            SYSTEM -> null
            DARK -> true
            LIGHT -> false
        }
    }.stateIn(dutyScope, SharingStarted.WhileSubscribed(5000), null)

    val appThemeMode = prefsRepo.themeModeFlow().stateIn(dutyScope, SharingStarted.WhileSubscribed(5000), RyoPreferences.DEFAULT_THEME_MODE)
    val appLanguage = prefsRepo.languageFlow().stateIn(dutyScope, SharingStarted.WhileSubscribed(5000), RyoPreferences.DEFAULT_LANGUAGE)

    fun setAppThemeMode(mode: RyoPreferences.ThemeMode) = dutyScope.launch { prefsRepo.setThemeMode(mode) }
    fun setAppLanguage(lang: RyoPreferences.Language) = dutyScope.launch { prefsRepo.setLanguage(lang) }

    val tabs: List<TabDuty.Tab> get() = tabHostDuty.tabs
    val activeTabId: String? get() = tabHostDuty.activeTabId
    val activeTab: TabDuty.Tab? get() = tabHostDuty.activeTab
    val hasDirtyTabs: Boolean get() = tabHostDuty.hasDirtyTabs

    val windowTitle: String; @Composable get() = Res.string.app_name.text.let { name -> activeTab?.let { "${it.title} - $name" } ?: name }

    val windowInterop = object : RyoWindowInterop {
        override fun attachWindowController(controller: RyoWindowController) {
            windowController = controller
            isMaximized = controller.isMaximized
        }

        override fun detachWindowController(controller: RyoWindowController) {
            if (windowController !== controller) return
            windowController = null
            isMaximized = false
        }

        override fun onWindowMaximizedChanged(isMaximized: Boolean) {
            this@AppDuty.isMaximized = isMaximized
        }
    }

    val menuGroups: List<MenuGroup>
        get() {
            val welcomePage = tr(Res.string.menu_item_welcome_page)
            val editorSessionPage = tr(Res.string.menu_item_editor_session_page)
            val settingsPage = tr(Res.string.menu_item_settings_page)

            return listOf(
                MenuGroup(
                    "file", tr(Res.string.menu_group_file), listOf(
                        RyoMenuEntry.MenuItem(
                            tr(Res.string.menu_item_new),
                            children = listOf(
                                RyoMenuEntry.MenuItem(menuLabel(welcomePage, AppCommand.OpenWelcomeTab), commandDuty.canExecute(AppCommand.OpenWelcomeTab)) { commandDuty.execute(AppCommand.OpenWelcomeTab) },
                                RyoMenuEntry.MenuItem(
                                    menuLabel(editorSessionPage, AppCommand.OpenEditorSessionTab), commandDuty.canExecute(AppCommand.OpenEditorSessionTab)
                                ) { commandDuty.execute(AppCommand.OpenEditorSessionTab) },
                                RyoMenuEntry.MenuItem(menuLabel(settingsPage, AppCommand.OpenSettingsTab), commandDuty.canExecute(AppCommand.OpenSettingsTab)) { commandDuty.execute(AppCommand.OpenSettingsTab) })
                        ),
                        RyoMenuEntry.MenuItem(menuLabel(tr(Res.string.menu_item_close_tab), AppCommand.CloseCurrentTab), commandDuty.canExecute(AppCommand.CloseCurrentTab)) { commandDuty.execute(AppCommand.CloseCurrentTab) },
                        RyoMenuEntry.Divider,
                        RyoMenuEntry.MenuItem(menuLabel(tr(Res.string.menu_item_exit), AppCommand.RequestWindowClose), commandDuty.canExecute(AppCommand.RequestWindowClose)) { commandDuty.execute(AppCommand.RequestWindowClose) })
                ), MenuGroup(
                    "edit", tr(Res.string.menu_group_edit), listOf(
                        RyoMenuEntry.MenuItem(menuLabel(tr(Res.string.menu_item_undo), AppCommand.Undo), commandDuty.canExecute(AppCommand.Undo)) { commandDuty.execute(AppCommand.Undo) },
                        RyoMenuEntry.MenuItem(menuLabel(tr(Res.string.menu_item_redo), AppCommand.Redo), commandDuty.canExecute(AppCommand.Redo)) { commandDuty.execute(AppCommand.Redo) },
                        RyoMenuEntry.Divider,
                        RyoMenuEntry.MenuItem(menuLabel(tr(Res.string.menu_item_save), AppCommand.Save), commandDuty.canExecute(AppCommand.Save)) { commandDuty.execute(AppCommand.Save) },
                        RyoMenuEntry.MenuItem(menuLabel(tr(Res.string.menu_item_discard_changes), AppCommand.Discard), commandDuty.canExecute(AppCommand.Discard)) { commandDuty.execute(AppCommand.Discard) })
                ), MenuGroup(
                    "view", tr(Res.string.menu_group_view), listOf(
                        RyoMenuEntry.MenuItem(
                            menuLabel(tr(if (sideWorkspaceDuty.expanded) Res.string.menu_item_collapse_sidebar else Res.string.menu_item_expand_sidebar), AppCommand.ToggleSidePanel),
                            commandDuty.canExecute(AppCommand.ToggleSidePanel),
                        ) { commandDuty.execute(AppCommand.ToggleSidePanel) }, RyoMenuEntry.MenuItem(
                            tr(Res.string.menu_item_switch_panel),
                            children = listOf(
                                RyoMenuEntry.MenuItem(tr(Res.string.panel_assets_manager), commandDuty.canExecute(AppCommand.FocusAssetsPanel)) { commandDuty.execute(AppCommand.FocusAssetsPanel) },
                                RyoMenuEntry.MenuItem(tr(Res.string.panel_schemas_manager), commandDuty.canExecute(AppCommand.FocusSchemasPanel)) { commandDuty.execute(AppCommand.FocusSchemasPanel) })
                        ), RyoMenuEntry.Divider, RyoMenuEntry.MenuItem(
                            menuLabel(tr(if (isMaximized) Res.string.menu_item_exit_fullscreen else Res.string.menu_item_fullscreen), AppCommand.ToggleMaximizeWindow), commandDuty.canExecute(AppCommand.ToggleMaximizeWindow)
                        ) { commandDuty.execute(AppCommand.ToggleMaximizeWindow) })
                ), MenuGroup(
                    "help",
                    tr(Res.string.menu_group_help),
                    listOf(
                        RyoMenuEntry.MenuItem(menuLabel(welcomePage, AppCommand.OpenWelcomeTab), commandDuty.canExecute(AppCommand.OpenWelcomeTab)) { commandDuty.execute(AppCommand.OpenWelcomeTab) },
                        RyoMenuEntry.MenuItem(menuLabel(tr(Res.string.menu_item_settings), AppCommand.OpenSettingsTab), commandDuty.canExecute(AppCommand.OpenSettingsTab)) { commandDuty.execute(AppCommand.OpenSettingsTab) },
                        RyoMenuEntry.Divider,
                        RyoMenuEntry.MenuItem(tr(Res.string.menu_item_about), onClick = ::showAboutDialog)
                    )
                )
            )
        }

    init {
        AppLocaleUtils.applyAppLanguage(appLanguage.value)
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), tabTitleEditorSession(1), true)
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), tabTitleEditorSession(2))
        tabHostDuty.open(TabDuty.TabSpec.Settings, tabTitleSettings())
        registerCommands()

        dutyScope.launch {
            appLanguage.collect { language ->
                AppLocaleUtils.applyAppLanguage(language)
                tabHostDuty.updateSingletonTabTitles(tabTitleWelcome(), tabTitleSettings())
            }
        }
    }

    fun selectTab(tabId: String) = tabHostDuty.selectTab(tabId)

    fun requestCloseTab(id: String) {
        val tab = tabHostDuty.findTab(id) ?: return
        if (!tab.dirty) return tabHostDuty.closeTab(id)

        dialogDuty.orderCommon(
            headline = tr(Res.string.dialog_close_unsaved_title), description = tr(Res.string.dialog_close_unsaved_description_format, tab.title), actions = listOf(
                DialogDuty.DialogAction(tr(Res.string.action_cancel)), DialogDuty.DialogAction(tr(Res.string.action_close_tab)) {
                    tabHostDuty.closeTab(id)
                    true
                })
        )
    }

    fun closeTab(id: String) = tabHostDuty.closeTab(id)

    fun closeCurrentTab() = activeTabId?.also(::requestCloseTab)

    fun requestCloseOtherTabs(tabId: String) = tabs.map(TabDuty.Tab::id).filter { it != tabId }.forEach(::requestCloseTab)

    fun requestCloseAllTabs() = tabs.map(TabDuty.Tab::id).forEach(::requestCloseTab)

    fun moveTab(fromIndex: Int, toIndex: Int) = tabHostDuty.moveTab(fromIndex, toIndex)

    fun requestClose() {
        if (lifecycleStage == AppLifecycleStage.Closing) return
        lifecycleStage = AppLifecycleStage.Closing
        dialogDuty.orderCommon(
            icon = Res.drawable.ic_ryo_24px,
            headline = tr(Res.string.dialog_close_app_title),
            description = if (hasDirtyTabs) tr(Res.string.dialog_close_app_description_dirty) else tr(Res.string.dialog_close_app_description_clean),
            actions = listOf(DialogDuty.DialogAction(tr(Res.string.action_cancel)) { cancelClose(); true }, DialogDuty.DialogAction(tr(Res.string.action_exit)) { confirmClose(); true })
        )
    }

    fun cancelClose() {
        lifecycleStage = AppLifecycleStage.Running
    }

    fun confirmClose() {
        lifecycleStage = AppLifecycleStage.Closing
        dutyScope.cancel()
        exitApp()
    }

    fun minimizeWindow() = windowController?.minimize()

    fun toggleMaximizeWindow() = windowController?.toggleMaximize()

    fun requestWindowClose() = windowController?.requestClose() ?: requestClose()

    fun openWelcomeTab() {
        tabHostDuty.open(TabDuty.TabSpec.Welcome, tabTitleWelcome())
    }

    fun openEditorSessionTab() {
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), titleOverride = tabTitleEditorSession(editorSessionSeed++))
    }

    fun openSettingsTab() {
        tabHostDuty.open(TabDuty.TabSpec.Settings, tabTitleSettings())
    }

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

    fun toggleMenuGroup(groupId: String, anchorX: Int, anchorY: Int) {
        val group = menuGroups.firstOrNull { it.id == groupId } ?: return
        menuDuty.toggleMenuBarGroup(group.id, anchorX, anchorY, group.items)
    }

    fun hoverMenuGroup(groupId: String, anchorX: Int, anchorY: Int) {
        val group = menuGroups.firstOrNull { it.id == groupId } ?: return
        menuDuty.hoverMenuBarGroup(group.id, anchorX, anchorY, group.items)
    }

    fun dismissMenu() = menuDuty.dismiss()

    fun showContextMenu(anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) = menuDuty.showContextMenu(anchorX, anchorY, entries)

    fun showInPlaceSelectMenu(textX: Int, textCenterY: Int, selectedIndex: Int, density: Density, entries: List<RyoMenuEntry>) = menuDuty.showInPlaceSelectMenu(textX, textCenterY, selectedIndex, density, entries)

    fun showTabContextMenu(tabId: String, anchorX: Int, anchorY: Int) {
        val tab = tabHostDuty.findTab(tabId) ?: return
        val hasOtherTabs = tabs.size > 1
        showContextMenu(
            anchorX = anchorX, anchorY = anchorY, entries = listOf(
                RyoMenuEntry.MenuItem(text = tr(Res.string.tab_context_close_format, tab.title), onClick = { requestCloseTab(tab.id) }),
                RyoMenuEntry.MenuItem(text = tr(Res.string.tab_context_close_others), enabled = hasOtherTabs, onClick = { requestCloseOtherTabs(tab.id) }),
                RyoMenuEntry.MenuItem(text = tr(Res.string.tab_context_close_all), enabled = tabs.isNotEmpty(), onClick = ::requestCloseAllTabs),
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(
                    text = menuLabel(tr(Res.string.menu_item_new_editor_session_page), AppCommand.OpenEditorSessionTab),
                    enabled = commandDuty.canExecute(AppCommand.OpenEditorSessionTab),
                    onClick = { commandDuty.execute(AppCommand.OpenEditorSessionTab) })
            )
        )
    }

    private fun registerCommands() {
        commandDuty.register(AppCommand.OpenWelcomeTab) { openWelcomeTab() }
        commandDuty.register(AppCommand.OpenEditorSessionTab) { openEditorSessionTab() }
        commandDuty.register(AppCommand.OpenSettingsTab) { openSettingsTab() }
        commandDuty.register(AppCommand.CloseCurrentTab, canExecute = { activeTabId != null }) { closeCurrentTab() }
        commandDuty.register(AppCommand.ToggleSidePanel) { sideWorkspaceDuty.toggleExpanded() }
        commandDuty.register(AppCommand.FocusAssetsPanel) { sideWorkspaceDuty.focusPanel("assets") }
        commandDuty.register(AppCommand.FocusSchemasPanel) { sideWorkspaceDuty.focusPanel("schemas") }
        commandDuty.register(AppCommand.ToggleMaximizeWindow) { toggleMaximizeWindow() }
        commandDuty.register(AppCommand.RequestWindowClose) { requestWindowClose() }
        commandDuty.register(AppCommand.Undo, canExecute = { tabHostDuty.activeTabDuty?.canUndo() == true }) { tabHostDuty.executeOnActiveTab { it.undo() } }
        commandDuty.register(AppCommand.Redo, canExecute = { tabHostDuty.activeTabDuty?.canRedo() == true }) { tabHostDuty.executeOnActiveTab { it.redo() } }
        commandDuty.register(AppCommand.Save, canExecute = { tabHostDuty.activeTabDuty?.canSave() == true }) { tabHostDuty.executeOnActiveTab { it.save() } }
        commandDuty.register(AppCommand.Discard, canExecute = { tabHostDuty.activeTabDuty?.canDiscard() == true }) { tabHostDuty.executeOnActiveTab { it.discard() } }
    }

    private fun menuLabel(base: String, command: AppCommand) = shortcutDuty.commandDisplay(command)?.let { "$base\t$it" } ?: base

    private fun tr(resource: StringResource, vararg format: String): String = runBlocking {
        AppLocaleUtils.applyAppLanguage(appLanguage.value)
        getString(resource, *format)
    }

    private fun tabTitleWelcome() = tr(Res.string.tab_title_welcome)

    private fun tabTitleSettings() = tr(Res.string.tab_title_settings)

    private fun tabTitleEditorSession(index: Int) = tr(Res.string.tab_title_editor_session_index_format, index.toString())

    private fun defaultShortcutBindings() = mapOf(
        AppCommand.OpenEditorSessionTab to ShortcutDuty.Stroke(ShortcutDuty.Key.N, ctrl = true),
        AppCommand.OpenSettingsTab to ShortcutDuty.Stroke(ShortcutDuty.Key.Comma, ctrl = true),
        AppCommand.CloseCurrentTab to ShortcutDuty.Stroke(ShortcutDuty.Key.W, ctrl = true),
        AppCommand.Save to ShortcutDuty.Stroke(ShortcutDuty.Key.S, ctrl = true),
        AppCommand.Undo to ShortcutDuty.Stroke(ShortcutDuty.Key.Z, ctrl = true),
        AppCommand.Redo to ShortcutDuty.Stroke(ShortcutDuty.Key.Y, ctrl = true),
        AppCommand.ToggleSidePanel to ShortcutDuty.Stroke(ShortcutDuty.Key.B, ctrl = true),
        AppCommand.ToggleMaximizeWindow to ShortcutDuty.Stroke(ShortcutDuty.Key.F11)
    )
}
