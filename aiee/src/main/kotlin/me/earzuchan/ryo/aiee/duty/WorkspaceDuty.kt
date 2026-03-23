package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.snapshots.SnapshotStateList
import com.arkivanov.decompose.ComponentContext
import com.arkivanov.decompose.router.stack.ChildStack
import com.arkivanov.decompose.router.stack.StackNavigation
import com.arkivanov.decompose.router.stack.active
import com.arkivanov.decompose.router.stack.backStack
import com.arkivanov.decompose.router.stack.childStack
import com.arkivanov.decompose.router.stack.navigate
import com.arkivanov.decompose.value.Value
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.UiText

class WorkspaceDuty(ctx: ComponentContext) : ComponentContext by ctx {
    data class State(val tabs: List<TabDuty.Tab>, val activeTabId: String?)

    sealed interface Intent {
        data class Open(val navi: WorkspaceTabNavi) : Intent
        data class Select(val tabId: String) : Intent
        data class Close(val tabId: String) : Intent
        data class Move(val fromIndex: Int, val toIndex: Int) : Intent
        data object RestoreLastClosed : Intent
    }

    sealed interface Effect {
        data object NoOp : Effect
        data class Opened(val tabId: String) : Effect
        data class Closed(val tabId: String) : Effect
        data class Selected(val tabId: String) : Effect
        data object Restored : Effect
    }

    private val navigation = StackNavigation<WorkspaceTabNavi>()
    private val tabOrder: SnapshotStateList<String> = mutableStateListOf()
    private val closedHistory = ArrayDeque<WorkspaceTabNavi>()
    private var editorSessionSeed = 3

    val tabStack: Value<ChildStack<WorkspaceTabNavi, TabDuty>> = childStack(
        source = navigation,
        serializer = WorkspaceTabNavi.serializer(),
        initialConfiguration = WorkspaceTabNavi.Empty,
        key = "WorkspaceTabStack",
        handleBackButton = false,
        childFactory = ::createTabDuty
    )

    val sideWorkspaceDuty = SideWorkspaceDuty(ctx)

    val tabs: List<TabDuty.Tab> get() = tabOrder.mapNotNull(::findTab)
    val activeTabId: String? get() = tabStack.value.active.configuration.takeIf { it !is WorkspaceTabNavi.Empty }?.id
    val activeTab: TabDuty.Tab? get() = activeTabId?.let(::findTab)
    val activeTabDuty: TabDuty? get() = tabStack.value.active.instance.takeIf { it.navi !is WorkspaceTabNavi.Empty }
    val hasDirtyTabs: Boolean get() = tabs.any(TabDuty.Tab::dirty)
    val canRestoreClosedTab: Boolean get() = closedHistory.isNotEmpty()
    val state get() = State(tabs, activeTabId)

    init {
        seedDemoTabs()
    }

    fun dispatch(intent: Intent): Effect = when (intent) {
        is Intent.Open -> open(intent.navi)
        is Intent.Select -> select(intent.tabId)
        is Intent.Close -> close(intent.tabId)
        is Intent.Move -> move(intent.fromIndex, intent.toIndex)
        Intent.RestoreLastClosed -> restoreLastClosed()
    }

    fun selectTab(tabId: String) = dispatch(Intent.Select(tabId))

    fun findTab(tabId: String): TabDuty.Tab? = allTabDuties().firstOrNull { it.navi.id == tabId }?.let { TabDuty.Tab(it.navi.id, it.navi, it.title, it.dirty) }

    fun closeTab(id: String) = dispatch(Intent.Close(id))

    fun closeCurrentTab() = activeTabId?.also(::closeTab)

    fun requestCloseOtherTabs(tabId: String, onEach: (String) -> Unit) = tabs.map(TabDuty.Tab::id).filter { it != tabId }.forEach(onEach)

    fun requestCloseAllTabs(onEach: (String) -> Unit) = tabs.map(TabDuty.Tab::id).forEach(onEach)

    fun moveTab(fromIndex: Int, toIndex: Int) = dispatch(Intent.Move(fromIndex, toIndex))

    fun openWelcomeTab() = dispatch(Intent.Open(WorkspaceTabNavi.Welcome))

    fun openEditorSessionTab() = dispatch(Intent.Open(WorkspaceTabNavi.EditorSession("editor-${editorSessionSeed - 1}", editorSessionSeed++)))

    fun openSettingsTab() = dispatch(Intent.Open(WorkspaceTabNavi.Settings))

    fun restoreLastClosedTab() = dispatch(Intent.RestoreLastClosed)

    fun executeOnActiveTab(block: (TabDuty) -> Unit) {
        activeTabDuty?.also(block)
    }

    private fun open(navi: WorkspaceTabNavi): Effect {
        if (navi is WorkspaceTabNavi.Empty) return Effect.NoOp

        val singleton = when (navi) {
            WorkspaceTabNavi.Welcome -> WorkspaceTabNavi.Welcome.id
            WorkspaceTabNavi.Settings -> WorkspaceTabNavi.Settings.id
            else -> null
        }

        if (singleton != null && tabOrder.contains(singleton)) return select(singleton)
        if (tabOrder.contains(navi.id)) return select(navi.id)

        if (!tabOrder.contains(navi.id)) tabOrder += navi.id
        navigation.navigate { stack -> normalizeStack(withoutEmpty(stack).filterNot { it.id == navi.id } + navi) }
        return Effect.Opened(navi.id)
    }

    private fun select(tabId: String): Effect {
        if (tabId !in tabOrder) return Effect.NoOp
        val target = findNavi(tabId) ?: return Effect.NoOp
        navigation.navigate { stack -> normalizeStack(withoutEmpty(stack).filterNot { it.id == tabId } + target) }
        return Effect.Selected(tabId)
    }

    private fun close(tabId: String): Effect {
        if (tabId !in tabOrder) return Effect.NoOp

        val navi = findNavi(tabId) ?: return Effect.NoOp
        closedHistory += navi
        tabOrder.remove(tabId)

        navigation.navigate { stack -> normalizeStack(stack.filterNot { it.id == tabId }) }
        return Effect.Closed(tabId)
    }

    private fun move(fromIndex: Int, toIndex: Int): Effect {
        if (tabOrder.isEmpty()) return Effect.NoOp
        val normalizedTo = toIndex.coerceIn(0, tabOrder.lastIndex)
        if (fromIndex !in tabOrder.indices || normalizedTo !in tabOrder.indices || fromIndex == normalizedTo) return Effect.NoOp

        val moving = tabOrder.removeAt(fromIndex)
        tabOrder.add(normalizedTo, moving)
        navigation.navigate { stack ->
            val activeId = withoutEmpty(stack).lastOrNull()?.id
            val byId = withoutEmpty(stack).associateBy(WorkspaceTabNavi::id)
            val ordered = tabOrder.mapNotNull(byId::get)

            if (ordered.isEmpty()) listOf(WorkspaceTabNavi.Empty)
            else if (activeId == null) ordered
            else ordered.firstOrNull { it.id == activeId }?.let { active -> ordered.filterNot { it.id == activeId } + active } ?: ordered
        }

        return Effect.NoOp
    }

    private fun restoreLastClosed(): Effect {
        val last = closedHistory.removeLastOrNull() ?: return Effect.NoOp
        return open(last).let { Effect.Restored }
    }

    private fun findNavi(tabId: String): WorkspaceTabNavi? = allTabNavis().firstOrNull { it.id == tabId }

    private fun allTabNavis(): List<WorkspaceTabNavi> = tabStack.value.backStack.map { it.configuration } + tabStack.value.active.configuration

    private fun allTabDuties(): List<TabDuty> = tabStack.value.backStack.map { it.instance } + tabStack.value.active.instance

    private fun withoutEmpty(stack: List<WorkspaceTabNavi>) = stack.filterNot { it is WorkspaceTabNavi.Empty }

    private fun normalizeStack(stack: List<WorkspaceTabNavi>) = withoutEmpty(stack).ifEmpty { listOf(WorkspaceTabNavi.Empty) }

    private fun seedDemoTabs() {
        val demo = listOf(WorkspaceTabNavi.EditorSession("editor-0", 1, true), WorkspaceTabNavi.EditorSession("editor-1", 2), WorkspaceTabNavi.Settings)
        tabOrder.clear()
        tabOrder.addAll(demo.map(WorkspaceTabNavi::id))
        navigation.navigate { normalizeStack(demo) }
    }

    private fun createTabDuty(navi: WorkspaceTabNavi, subCtx: ComponentContext): TabDuty = when (navi) {
        WorkspaceTabNavi.Empty -> EmptyTabDuty(subCtx)
        WorkspaceTabNavi.Welcome -> WelcomeTabDuty(subCtx, UiText.Res(Res.string.tab_title_welcome))
        WorkspaceTabNavi.Settings -> SettingsTabDuty(subCtx, UiText.Res(Res.string.tab_title_settings))
        is WorkspaceTabNavi.EditorSession -> EditorSessionTabDuty(subCtx, navi, UiText.Res(Res.string.tab_title_editor_session_index_format, listOf(UiText.Plain(navi.index.toString()))), navi.initialDirty)
    }
}
