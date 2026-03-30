package me.earzuchan.ryo.aiee.duty

import kotlinx.serialization.Serializable

enum class AppLifecycleStage {
    Running,
    Closing
}

@Serializable
sealed class MainPanelTabNavis {
    abstract val id: String

    @Serializable
    data object Empty : MainPanelTabNavis() { override val id = "__empty__" }

    @Serializable
    data object Welcome : MainPanelTabNavis() { override val id = "welcome" }

    @Serializable
    data object Settings : MainPanelTabNavis() { override val id = "settings" }

    @Serializable
    data class EditorSession(override val id: String) : MainPanelTabNavis()
}

@Serializable
sealed class SidePanelNavis {
    abstract val id: String

    @Serializable
    data object Assets : SidePanelNavis() { override val id = "assets" }

    @Serializable
    data object Schemas : SidePanelNavis() { override val id = "schemas" }
}
