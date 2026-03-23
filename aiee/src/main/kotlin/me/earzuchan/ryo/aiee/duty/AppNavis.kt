package me.earzuchan.ryo.aiee.duty

import kotlinx.serialization.Serializable

@Serializable
sealed class WorkspaceTabNavi {
    abstract val id: String

    @Serializable
    data object Empty : WorkspaceTabNavi() { override val id = "__empty__" }

    @Serializable
    data object Welcome : WorkspaceTabNavi() { override val id = "welcome" }

    @Serializable
    data object Settings : WorkspaceTabNavi() { override val id = "settings" }

    @Serializable
    data class EditorSession(override val id: String, val index: Int, val initialDirty: Boolean = false) : WorkspaceTabNavi()
}

@Serializable
sealed class SidePanelNavi {
    abstract val id: String

    @Serializable
    data object Assets : SidePanelNavi() { override val id = "assets" }

    @Serializable
    data object Schemas : SidePanelNavi() { override val id = "schemas" }
}
