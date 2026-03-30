package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.mutableStateListOf
import com.arkivanov.decompose.Child
import com.arkivanov.decompose.router.stack.ChildStack
import com.arkivanov.decompose.router.stack.StackNavigation
import com.arkivanov.decompose.router.stack.childStack
import com.arkivanov.decompose.router.stack.navigate
import com.arkivanov.decompose.value.Value
import me.earzuchan.ryo.aiee.app.CommandService
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import com.arkivanov.decompose.ComponentContext as DutyContext

class MainPanelDuty(ctx: DutyContext) : DutyContext by ctx, KoinComponent {
    val commandService by inject<CommandService>()

    companion object {
        private const val TAG = "MainPanelDuty"

        const val CMD_CLOSE_ACTIVE_TAB = "$TAG.CloseActive"
        const val CMD_CLOSE_ALL_TABS = "$TAG.CloseAll"
        const val CMD_MENTION_WELCOME = "$TAG.MentionWelcome"
        const val CMD_MENTION_SETTINGS = "$TAG.MentionSettings"
    }

    data class TabData(val id: String, val duty: TabDuty, val isResidential: Boolean)

    fun getUiModels(stackItems: List<Child<MainPanelTabNavis, TabDuty>>): List<TabData> = slots.mapNotNull { slot ->
        val child = stackItems.find { it.configuration.id == slot.id }
        if (child == null) null else TabData(slot.id, child.instance!!, slot.residential)
    }

    data class TabSlot(val id: String, val navi: MainPanelTabNavis, val residential: Boolean = false)

    // --- 导航与状态管理 ---

    private val navigation = StackNavigation<MainPanelTabNavis>()

    val tabStack: Value<ChildStack<MainPanelTabNavis, TabDuty>> = childStack(navigation, MainPanelTabNavis.serializer(), MainPanelTabNavis.Empty, "MainPanelStack", false, ::mapChild)

    // 视图层绑定的真实 Tabs 列表 (内部存的是 TabState)
    val slots = mutableStateListOf<TabSlot>()

    val activeTabId: String get() = tabStack.value.active.configuration.id

    init {
        commandService.register(CMD_CLOSE_ACTIVE_TAB) { if (activeTabId != MainPanelTabNavis.Empty.id) close(activeTabId) }
        commandService.register(CMD_CLOSE_ALL_TABS) { closeAll() }
        commandService.register(CMD_MENTION_WELCOME) { mentionWelcome() }
        commandService.register(CMD_MENTION_SETTINGS) { mentionSettings() }
    }

    // --- 精细化的 Mention (唤起) 逻辑 ---

    fun mentionWelcome() {
        val id = MainPanelTabNavis.Welcome.id
        if (slots.none { it.id == id }) slots.add(TabSlot(id, MainPanelTabNavis.Welcome, true))
        focus(id)
    }

    fun mentionSettings() {
        val id = MainPanelTabNavis.Settings.id
        if (slots.none { it.id == id }) slots.add(TabSlot(id, MainPanelTabNavis.Settings, true))
        focus(id)
    }

    // 未来去掉Preview，并改为自动替换非常驻
    fun mentionSession(session: MainPanelTabNavis.EditorSession) {
        val existingIndex = slots.indexOfFirst { it.id == session.id }

        if (existingIndex != -1) {
            focus(session.id)
            return
        }

        val newSlot = TabSlot(session.id, session)
        val activeIndex = slots.indexOfFirst { it.id == activeTabId }
        val shouldReplaceActive = activeIndex != -1 && !slots[activeIndex].residential

        if (shouldReplaceActive) {
            val oldId = slots[activeIndex].id
            slots[activeIndex] = newSlot
            navigation.navigate { curr -> curr.filter { it.id != oldId && it !is MainPanelTabNavis.Empty } + session }
        } else {
            slots.add(newSlot)
            navigation.navigate { curr -> curr.filter { it !is MainPanelTabNavis.Empty } + session }
        }
    }

    // --- Tab 状态变更 (给编辑器内部或者 Tab 双击事件用的) ---

    /** 双击 Tab，使其转正(变成常驻) */
    fun makeResidential(id: String) {
        val index = slots.indexOfFirst { it.id == id }
        if (index != -1 && !slots[index].residential) slots[index] = slots[index].copy(residential = true)
    }

    // --- 供视图 TabChips 调用的基础导航方法 ---

    fun focus(id: String) {
        if (slots.none { it.id == id }) return
        navigation.navigate { currentStack -> currentStack.filter { it.id != id && it !is MainPanelTabNavis.Empty } + slots.first { it.id == id }.navi }
    }

    fun moveTab(fromIndex: Int, toIndex: Int) {
        if (fromIndex !in slots.indices || toIndex !in slots.indices) return
        val item = slots.removeAt(fromIndex)
        slots.add(toIndex, item)
    }

    // --- 关闭逻辑群 ---

    fun close(id: String) = syncState(slots.filter { it.id != id }.map { it.id }.toSet())

    fun closeOthers(id: String) = syncState(setOf(id))

    fun closeLeftOf(id: String) {
        val index = slots.indexOfFirst { it.id == id }
        if (index > 0) syncState(slots.drop(index).map { it.id }.toSet())
    }

    fun closeRightOf(id: String) {
        val index = slots.indexOfFirst { it.id == id }
        if (index in 0 until slots.size - 1) syncState(slots.take(index + 1).map { it.id }.toSet())
    }

    fun closeAll() = syncState(emptySet())

    // --- 私有辅助与映射 ---

    private fun syncState(retainedIds: Set<String>) {
        slots.retainAll { it.id in retainedIds }
        navigation.navigate { currentStack ->
            if (retainedIds.isEmpty()) listOf(MainPanelTabNavis.Empty)
            else currentStack.filter { it.id in retainedIds }
        }
    }

    private fun mapChild(navi: MainPanelTabNavis, subCtx: DutyContext): TabDuty = when (navi) {
        is MainPanelTabNavis.Empty -> EmptyTabDuty(subCtx)
        is MainPanelTabNavis.Welcome -> WelcomeTabDuty(subCtx)
        is MainPanelTabNavis.Settings -> SettingsTabDuty(subCtx)
        is MainPanelTabNavis.EditorSession -> EditorSessionTabDuty(subCtx, navi) { makeResidential(navi.id) }
    }
}