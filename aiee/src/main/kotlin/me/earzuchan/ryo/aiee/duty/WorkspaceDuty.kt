package me.earzuchan.ryo.aiee.duty

import com.arkivanov.decompose.ComponentContext
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.UiText

class WorkspaceDuty(ctx: ComponentContext, private val tabHostDuty: TabHostDuty = TabHostDuty(TabDutyFactory())) {
    private var editorSessionSeed = 3

    val sideWorkspaceDuty = SideWorkspaceDuty(ctx)

    val tabs: List<TabDuty.Tab> get() = tabHostDuty.tabs
    val activeTabId: String? get() = tabHostDuty.activeTabId
    val activeTab: TabDuty.Tab? get() = tabHostDuty.activeTab
    val activeTabDuty: TabDuty? get() = tabHostDuty.activeTabDuty
    val hasDirtyTabs: Boolean get() = tabHostDuty.hasDirtyTabs

    fun seedDemoTabs() {
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), tabTitleEditorSession(1), true)
        tabHostDuty.open(TabDuty.TabSpec.EditorSession(), tabTitleEditorSession(2))
        tabHostDuty.open(TabDuty.TabSpec.Settings, tabTitleSettings())
    }

    fun selectTab(tabId: String) = tabHostDuty.selectTab(tabId)

    fun findTab(tabId: String) = tabHostDuty.findTab(tabId)

    fun closeTab(id: String) = tabHostDuty.closeTab(id)

    fun closeCurrentTab() = activeTabId?.also(tabHostDuty::closeTab)

    fun requestCloseOtherTabs(tabId: String, onEach: (String) -> Unit) = tabs.map(TabDuty.Tab::id).filter { it != tabId }.forEach(onEach)

    fun requestCloseAllTabs(onEach: (String) -> Unit) = tabs.map(TabDuty.Tab::id).forEach(onEach)

    fun moveTab(fromIndex: Int, toIndex: Int) = tabHostDuty.moveTab(fromIndex, toIndex)

    fun openWelcomeTab() = tabHostDuty.open(TabDuty.TabSpec.Welcome, tabTitleWelcome())

    fun openEditorSessionTab() = tabHostDuty.open(TabDuty.TabSpec.EditorSession(), tabTitleEditorSession(editorSessionSeed++))

    fun openSettingsTab() = tabHostDuty.open(TabDuty.TabSpec.Settings, tabTitleSettings())

    fun executeOnActiveTab(block: (TabDuty) -> Unit) = tabHostDuty.executeOnActiveTab(block)

    private fun tabTitleWelcome() = UiText.Res(Res.string.tab_title_welcome)

    private fun tabTitleSettings() = UiText.Res(Res.string.tab_title_settings)

    private fun tabTitleEditorSession(index: Int) = UiText.Res(Res.string.tab_title_editor_session_index_format, listOf(UiText.Plain(index.toString())))
}
