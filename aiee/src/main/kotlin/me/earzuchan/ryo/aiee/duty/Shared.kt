package me.earzuchan.ryo.aiee.duty

import kotlinx.serialization.Serializable

enum class AppCommand {
    OpenWelcomeTab,
    OpenEditorSessionTab,
    OpenSettingsTab,
    RestoreClosedTab,
    CloseCurrentTab,
    ToggleSidePanel,
    FocusAssetsPanel,
    FocusSchemasPanel,
    ToggleMaximizeWindow,
    RequestWindowClose,
    Undo,
    Redo,
    Save,
    Discard
}

enum class AppLifecycleStage {
    Running,
    Closing
}

@Serializable
sealed class WorkspaceTabNavis {
    abstract val id: String

    @Serializable
    data object Empty : WorkspaceTabNavis() { override val id = "__empty__" }

    @Serializable
    data object Welcome : WorkspaceTabNavis() { override val id = "welcome" }

    @Serializable
    data object Settings : WorkspaceTabNavis() { override val id = "settings" }

    @Serializable
    data class EditorSession(override val id: String, val index: Int, val initialDirty: Boolean = false) : WorkspaceTabNavis()
}

@Serializable
sealed class SidePanelNavis {
    abstract val id: String

    @Serializable
    data object Assets : SidePanelNavis() { override val id = "assets" }

    @Serializable
    data object Schemas : SidePanelNavis() { override val id = "schemas" }
}
