package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue

abstract class TabDuty(val tabId: String, val spec: TabSpec, initialTitle: String, initialDirty: Boolean = false) {
    sealed interface TabSpec {
        val singletonId: String?
        val tabPrefix: String

        object Welcome : TabSpec {
            override val singletonId = "welcome"
            override val tabPrefix = "welcome"
        }

        enum class EditorSessionKind {
            Default
        }

        data class EditorSession(val kind: EditorSessionKind = EditorSessionKind.Default) : TabSpec {
            override val singletonId: String? = null
            override val tabPrefix = "editor"
        }

        object Settings : TabSpec {
            override val singletonId = "settings"
            override val tabPrefix = "settings"
        }
    }

    data class Tab(
        val id: String,
        val spec: TabSpec,
        val title: String,
        val dirty: Boolean = false
    )

    var title by mutableStateOf(initialTitle)
    var dirty by mutableStateOf(initialDirty)

    open fun canUndo() = false
    open fun undo() {}
    open fun canRedo() = false
    open fun redo() {}
    open fun canSave() = false
    open fun save() {}
    open fun canDiscard() = false
    open fun discard() {}
}

class WelcomeTabDuty(tabId: String, title: String = "欢迎") : TabDuty(tabId, TabSpec.Welcome, title)

class SettingsTabDuty(tabId: String, title: String = "设置") : TabDuty(tabId, TabSpec.Settings, title)

class EditorSessionTabDuty(tabId: String, spec: TabSpec.EditorSession, title: String, initialDirty: Boolean = false) : TabDuty(tabId, spec, title, initialDirty) {
    override fun canSave() = dirty

    override fun save() {
        dirty = false
    }

    override fun canDiscard() = dirty

    override fun discard() {
        dirty = false
    }
}