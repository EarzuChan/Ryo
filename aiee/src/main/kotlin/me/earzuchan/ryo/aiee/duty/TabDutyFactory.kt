package me.earzuchan.ryo.aiee.duty

import me.earzuchan.ryo.aiee.ui.UiText

class TabDutyFactory {
    fun create(tabId: String, spec: TabDuty.TabSpec, titleOverride: UiText? = null, initialDirty: Boolean = false): TabDuty = when (spec) {
        TabDuty.TabSpec.Welcome -> WelcomeTabDuty(tabId, titleOverride ?: UiText.Plain(tabId))
        TabDuty.TabSpec.Settings -> SettingsTabDuty(tabId, titleOverride ?: UiText.Plain(tabId))
        is TabDuty.TabSpec.EditorSession -> EditorSessionTabDuty(tabId, spec, titleOverride ?: UiText.Plain(tabId), initialDirty)
    }
}
