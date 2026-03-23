package me.earzuchan.ryo.aiee.duty

class TabDutyFactory {
    fun create(tabId: String, spec: TabDuty.TabSpec, titleOverride: String? = null, initialDirty: Boolean = false): TabDuty = when (spec) {
        TabDuty.TabSpec.Welcome -> WelcomeTabDuty(tabId, titleOverride ?: tabId)
        TabDuty.TabSpec.Settings -> SettingsTabDuty(tabId, titleOverride ?: tabId)
        is TabDuty.TabSpec.EditorSession -> EditorSessionTabDuty(tabId, spec, titleOverride ?: tabId, initialDirty)
    }
}
