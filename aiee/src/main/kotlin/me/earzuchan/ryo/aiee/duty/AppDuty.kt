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
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.prefs.AppSettings
import me.earzuchan.ryo.aiee.prefs.DataStorePrefs
import me.earzuchan.ryo.aiee.prefs.ThemeMode
import me.earzuchan.ryo.aiee.prefs.UiLanguage
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.app_name
import me.earzuchan.ryo.aiee.resources.ic_ryo_24px
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.dialog.AboutDialog
import me.earzuchan.ryo.aiee.ui.window.RyoWindowController
import me.earzuchan.ryo.aiee.ui.window.RyoWindowInterop
import me.earzuchan.ryo.aiee.util.ResUtils.text
import com.arkivanov.decompose.ComponentContext as DutyContext
import org.koin.core.component.KoinComponent as KoinDuty
import org.koin.core.component.inject

class AppDuty(ctx: DutyContext, private val exitApp: () -> Unit) : DutyContext by ctx, KoinDuty {
    data class MenuGroup(
        val id: String, val label: String, val items: List<RyoMenuEntry>
    )

    private val tabHostDuty = TabHostDuty(TabDutyFactory())
    private val dutyScope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)
    private val dataStorePrefs: DataStorePrefs by inject()
    private var editorSessionSeed = 2

    val sideWorkspaceDuty = SideWorkspaceDuty(ctx)
    val menuDuty = MenuDuty()
    val dialogDuty = DialogDuty()
    val commandDuty = CommandDuty()
    val shortcutDuty = ShortcutDuty(defaultShortcutBindings())

    private var windowController: RyoWindowController? = null

    var lifecycleStage by mutableStateOf(AppLifecycleStage.Running)

    var isMaximized by mutableStateOf(false); private set

    var appSettings by mutableStateOf(AppSettings()); private set

    val forceDarkTheme: Boolean?
        get() = when (appSettings.themeMode) {
            ThemeMode.FollowSystem -> null
            ThemeMode.Dark -> true
            ThemeMode.Light -> false
        }

    val tabs: List<TabDuty.Tab> get() = tabHostDuty.tabs

    val activeTabId: String? get() = tabHostDuty.activeTabId

    val activeTab: TabDuty.Tab? get() = tabHostDuty.activeTab

    val hasDirtyTabs: Boolean get() = tabHostDuty.hasDirtyTabs

    val windowTitle: String @Composable get() = Res.string.app_name.text.let { name -> activeTab?.let { "${it.title} - $name" } ?: name }

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
        get() = listOf(
            MenuGroup(
                "file", "文件", listOf(
                    RyoMenuEntry.MenuItem(
                        "新建", children = listOf(
                            RyoMenuEntry.MenuItem(text = menuLabel("欢迎页", AppCommand.OpenWelcomeTab), enabled = commandDuty.canExecute(AppCommand.OpenWelcomeTab), onClick = { commandDuty.execute(AppCommand.OpenWelcomeTab) }),
                            RyoMenuEntry.MenuItem(
                                text = menuLabel("编辑会话页", AppCommand.OpenEditorSessionTab), enabled = commandDuty.canExecute(AppCommand.OpenEditorSessionTab), onClick = { commandDuty.execute(AppCommand.OpenEditorSessionTab) }),
                            RyoMenuEntry.MenuItem(text = menuLabel("设置页", AppCommand.OpenSettingsTab), enabled = commandDuty.canExecute(AppCommand.OpenSettingsTab), onClick = { commandDuty.execute(AppCommand.OpenSettingsTab) })
                        )
                    ),
                    RyoMenuEntry.MenuItem(text = menuLabel("关闭标签页", AppCommand.CloseCurrentTab), enabled = commandDuty.canExecute(AppCommand.CloseCurrentTab), onClick = { commandDuty.execute(AppCommand.CloseCurrentTab) }),
                    RyoMenuEntry.Divider,
                    RyoMenuEntry.MenuItem(text = menuLabel("退出", AppCommand.RequestWindowClose), enabled = commandDuty.canExecute(AppCommand.RequestWindowClose), onClick = { commandDuty.execute(AppCommand.RequestWindowClose) })
                )
            ), MenuGroup(
                "edit", "编辑", listOf(
                    RyoMenuEntry.MenuItem(text = menuLabel("撤销", AppCommand.Undo), enabled = commandDuty.canExecute(AppCommand.Undo), onClick = { commandDuty.execute(AppCommand.Undo) }),
                    RyoMenuEntry.MenuItem(text = menuLabel("重做", AppCommand.Redo), enabled = commandDuty.canExecute(AppCommand.Redo), onClick = { commandDuty.execute(AppCommand.Redo) }),
                    RyoMenuEntry.Divider,
                    RyoMenuEntry.MenuItem(text = menuLabel("保存", AppCommand.Save), enabled = commandDuty.canExecute(AppCommand.Save), onClick = { commandDuty.execute(AppCommand.Save) }),
                    RyoMenuEntry.MenuItem(text = menuLabel("放弃更改", AppCommand.Discard), enabled = commandDuty.canExecute(AppCommand.Discard), onClick = { commandDuty.execute(AppCommand.Discard) })
                )
            ), MenuGroup(
                "view", "视图", listOf(
                    RyoMenuEntry.MenuItem(
                        text = menuLabel(if (sideWorkspaceDuty.expanded) "收起侧栏" else "展开侧栏", AppCommand.ToggleSidePanel),
                        enabled = commandDuty.canExecute(AppCommand.ToggleSidePanel),
                        onClick = { commandDuty.execute(AppCommand.ToggleSidePanel) }), RyoMenuEntry.MenuItem(
                        "切换面板", children = listOf(
                            RyoMenuEntry.MenuItem(text = "资产管理器", enabled = commandDuty.canExecute(AppCommand.FocusAssetsPanel), onClick = { commandDuty.execute(AppCommand.FocusAssetsPanel) }),
                            RyoMenuEntry.MenuItem(text = "Schema管理器", enabled = commandDuty.canExecute(AppCommand.FocusSchemasPanel), onClick = { commandDuty.execute(AppCommand.FocusSchemasPanel) })
                        )
                    ), RyoMenuEntry.Divider, RyoMenuEntry.MenuItem(
                        text = menuLabel(if (isMaximized) "退出全屏" else "全屏", AppCommand.ToggleMaximizeWindow),
                        enabled = commandDuty.canExecute(AppCommand.ToggleMaximizeWindow),
                        onClick = { commandDuty.execute(AppCommand.ToggleMaximizeWindow) })
                )
            ), MenuGroup(
                "help", "帮助", listOf(
                    RyoMenuEntry.MenuItem(text = menuLabel("欢迎页", AppCommand.OpenWelcomeTab), enabled = commandDuty.canExecute(AppCommand.OpenWelcomeTab), onClick = { commandDuty.execute(AppCommand.OpenWelcomeTab) }),
                    RyoMenuEntry.MenuItem(text = menuLabel("设置", AppCommand.OpenSettingsTab), enabled = commandDuty.canExecute(AppCommand.OpenSettingsTab), onClick = { commandDuty.execute(AppCommand.OpenSettingsTab) }),
                    RyoMenuEntry.Divider,
                    RyoMenuEntry.MenuItem(text = "关于", onClick = ::showAboutDialog)
                )
            )
        )

    init {
        dutyScope.launch { dataStorePrefs.settingsFlow.collect { appSettings = it } }
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), "Sometext", true)
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), "Sometext")
        tabHostDuty.open(TabDuty.TabSpec.Settings)
        registerCommands()
    }

    fun selectTab(tabId: String) = tabHostDuty.selectTab(tabId)

    fun requestCloseTab(id: String) {
        val tab = tabHostDuty.findTab(id) ?: return
        if (!tab.dirty) return tabHostDuty.closeTab(id)

        dialogDuty.orderCommon(
            headline = "关闭未保存标签页", description = "“${tab.title}” 尚未保存，仍要关闭吗", actions = listOf(
                DialogDuty.DialogAction("取消"), DialogDuty.DialogAction("关闭标签") { tabHostDuty.closeTab(id); true })
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
            headline = "确认关闭应用",
            description = if (hasDirtyTabs) "存在未保存的标签页，仍要退出吗" else "确定要退出吗",
            actions = listOf(DialogDuty.DialogAction("取消") { cancelClose(); true }, DialogDuty.DialogAction("退出") { confirmClose(); true })
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

    fun setUiLanguage(uiLanguage: UiLanguage) {
        dutyScope.launch { dataStorePrefs.setUiLanguage(uiLanguage) }
    }

    fun setThemeMode(themeMode: ThemeMode) {
        dutyScope.launch { dataStorePrefs.setThemeMode(themeMode) }
    }

    fun minimizeWindow() = windowController?.minimize()

    fun toggleMaximizeWindow() = windowController?.toggleMaximize()

    fun requestWindowClose() = windowController?.requestClose() ?: requestClose()

    fun openWelcomeTab() {
        tabHostDuty.open(TabDuty.TabSpec.Welcome)
    }

    fun openEditorSessionTab() {
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), titleOverride = "编辑会话 ${editorSessionSeed++}")
    }

    fun openSettingsTab() {
        tabHostDuty.open(TabDuty.TabSpec.Settings)
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
                RyoMenuEntry.MenuItem(text = "关闭“${tab.title}”", onClick = { requestCloseTab(tab.id) }),
                RyoMenuEntry.MenuItem(text = "关闭其他标签页", enabled = hasOtherTabs, onClick = { requestCloseOtherTabs(tab.id) }),
                RyoMenuEntry.MenuItem(text = "关闭全部标签页", enabled = tabs.isNotEmpty(), onClick = ::requestCloseAllTabs),
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(
                    text = menuLabel("新建编辑会话页", AppCommand.OpenEditorSessionTab), enabled = commandDuty.canExecute(AppCommand.OpenEditorSessionTab), onClick = { commandDuty.execute(AppCommand.OpenEditorSessionTab) })
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
