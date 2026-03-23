package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateListOf
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import me.earzuchan.ryo.aiee.ui.UiText

class TabHostDuty(private val tabDutyFactory: TabDutyFactory) {
    private val _tabs = mutableStateListOf<TabDuty>()
    private var tabIdSeed = 0

    var activeTabId by mutableStateOf<String?>(null)

    val tabs: List<TabDuty.Tab> get() = _tabs.map { TabDuty.Tab(it.tabId, it.spec, it.title, it.dirty) }

    val activeTab: TabDuty.Tab? get() = tabs.firstOrNull { it.id == activeTabId }

    val activeTabDuty: TabDuty? get() = _tabs.firstOrNull { it.tabId == activeTabId }

    val hasDirtyTabs: Boolean get() = _tabs.any { it.dirty }

    fun open(spec: TabDuty.TabSpec, titleOverride: UiText? = null, initialDirty: Boolean = false): TabDuty.Tab {
        singletonTabId(spec)?.let { singletonId ->
            _tabs.firstOrNull { it.tabId == singletonId }?.also {
                activeTabId = it.tabId
                return TabDuty.Tab(it.tabId, it.spec, it.title, it.dirty)
            }
        }

        val tabId = singletonTabId(spec) ?: "${spec.tabPrefix}-${tabIdSeed++}"
        val tabDuty = tabDutyFactory.create(tabId, spec, titleOverride, initialDirty)
        _tabs += tabDuty
        activeTabId = tabDuty.tabId
        return TabDuty.Tab(tabDuty.tabId, tabDuty.spec, tabDuty.title, tabDuty.dirty)
    }

    fun selectTab(id: String) {
        if (_tabs.none { it.tabId == id }) return
        activeTabId = id
    }

    fun findTab(tabId: String): TabDuty.Tab? = _tabs.firstOrNull { it.tabId == tabId }?.let { TabDuty.Tab(it.tabId, it.spec, it.title, it.dirty) }

    fun closeTab(tabId: String) {
        val index = _tabs.indexOfFirst { it.tabId == tabId }
        if (index == -1) return

        val target = if (activeTabId == tabId) _tabs.getOrNull(index - 1)?.tabId ?: _tabs.getOrNull(index + 1)?.tabId else activeTabId
        _tabs.removeAt(index)
        activeTabId = target ?: _tabs.lastOrNull()?.tabId
    }

    fun closeCurrentTab() {
        activeTabId?.also { closeTab(it) }
    }

    fun moveTab(fromIndex: Int, toIndex: Int) {
        if (fromIndex !in _tabs.indices || toIndex !in _tabs.indices || fromIndex == toIndex) return

        val moving = _tabs.removeAt(fromIndex)
        _tabs.add(toIndex, moving)
    }

    fun executeOnActiveTab(block: (TabDuty) -> Unit) {
        activeTabDuty?.also(block)
    }

    private fun singletonTabId(spec: TabDuty.TabSpec) = spec.singletonId
}
