package me.earzuchan.ryo.aiee.duty

class TabDutyFactory {
    fun create(tabId: String, spec: TabDuty.TabSpec, titleOverride: String? = null, initialDirty: Boolean = false): TabDuty = when (spec) {
        TabDuty.TabSpec.Welcome -> WelcomeTabDuty(tabId, titleOverride ?: "欢迎")
        TabDuty.TabSpec.Settings -> SettingsTabDuty(tabId, titleOverride ?: "设置")
        is TabDuty.TabSpec.EditorSession -> EditorSessionTabDuty(tabId, spec, titleOverride ?: "编辑会话", initialDirty)
    }
}
