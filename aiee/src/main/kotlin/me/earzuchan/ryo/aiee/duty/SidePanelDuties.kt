package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.arkivanov.decompose.ComponentContext as DutyContext
import com.arkivanov.decompose.router.stack.ChildStack
import com.arkivanov.decompose.router.stack.StackNavigation
import com.arkivanov.decompose.router.stack.bringToFront
import com.arkivanov.decompose.router.stack.childStack
import com.arkivanov.decompose.value.Value
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import org.jetbrains.compose.resources.DrawableResource
import org.jetbrains.compose.resources.StringResource

class SidePanelDuty(ctx: DutyContext) : DutyContext by ctx {
    companion object {
        private const val DEFAULT_WIDTH_DP = 280F
        private const val COLLAPSE_THRESHOLD_DP = 80F
        private const val MAX_WIDTH_DP = 560F
    }

    data class State(val expanded: Boolean, val panelWidthDp: Float, val activePanelId: String)

    sealed interface Intent {
        data class Focus(val panelId: String) : Intent
        data object ToggleExpanded : Intent
        data class Resize(val deltaDp: Float) : Intent
    }

    sealed interface Effect {
        data object NoOp : Effect
        data class Focused(val panelId: String) : Effect
        data object Resized : Effect
    }

    data class Panel(val id: String, val navi: SidePanelNavis, val titleRes: StringResource, val icon: DrawableResource, val selectedIcon: DrawableResource = icon)

    sealed interface PanelChild {
        val panelId: String
        val titleRes: StringResource

        data class Assets(val duty: AssetsPanelDuty) : PanelChild {
            override val panelId = SidePanelNavis.Assets.id
            override val titleRes = Res.string.panel_assets_manager
        }

        data class Schemas(val duty: SchemasPanelDuty) : PanelChild {
            override val panelId = SidePanelNavis.Schemas.id
            override val titleRes = Res.string.panel_schemas_manager
        }
    }

    private val navigation = StackNavigation<SidePanelNavis>()

    val panelStack: Value<ChildStack<SidePanelNavis, PanelChild>> = childStack(navigation, SidePanelNavis.serializer(), SidePanelNavis.Assets, "SidePanelStack", false, ::mapPanelChild)

    val panels = listOf(
        Panel(SidePanelNavis.Assets.id, SidePanelNavis.Assets, Res.string.panel_assets_manager, Res.drawable.ic_list_24px, Res.drawable.ic_list_filled_24px),
        Panel(SidePanelNavis.Schemas.id, SidePanelNavis.Schemas, Res.string.panel_schemas_manager, Res.drawable.ic_schemas_24px, Res.drawable.ic_schemas_filled_24px)
    )

    var expanded by mutableStateOf(true); private set
    var panelWidthDp by mutableStateOf(DEFAULT_WIDTH_DP); private set
    private var collapsedByDrag by mutableStateOf(false)

    val activePanelId: String get() = panelStack.value.active.configuration.id
    val activePanel: Panel get() = panels.firstOrNull { it.id == activePanelId } ?: panels.first()
    val state get() = State(expanded, panelWidthDp, activePanelId)

    fun dispatch(intent: Intent): Effect = when (intent) {
        is Intent.Focus -> focus(intent.panelId)
        Intent.ToggleExpanded -> toggle()
        is Intent.Resize -> resize(intent.deltaDp)
    }

    fun focusPanel(id: String) = dispatch(Intent.Focus(id))

    fun toggleExpanded() = dispatch(Intent.ToggleExpanded)

    fun resizeBy(deltaDp: Float) = dispatch(Intent.Resize(deltaDp))

    private fun focus(id: String): Effect {
        val panel = panels.firstOrNull { it.id == id } ?: return Effect.NoOp
        navigation.bringToFront(panel.navi)
        if (!expanded && collapsedByDrag) panelWidthDp = DEFAULT_WIDTH_DP
        collapsedByDrag = false
        expanded = true
        return Effect.Focused(id)
    }

    private fun toggle(): Effect {
        if (expanded) {
            expanded = false
            return Effect.NoOp
        }

        if (collapsedByDrag) panelWidthDp = DEFAULT_WIDTH_DP
        collapsedByDrag = false
        expanded = true
        return Effect.NoOp
    }

    private fun resize(deltaDp: Float): Effect {
        val targetWidth = panelWidthDp + deltaDp
        if (targetWidth <= COLLAPSE_THRESHOLD_DP) {
            collapsedByDrag = true
            expanded = false
            return Effect.Resized
        }

        panelWidthDp = targetWidth.coerceAtMost(MAX_WIDTH_DP)
        collapsedByDrag = false
        expanded = true
        return Effect.Resized
    }

    private fun mapPanelChild(navi: SidePanelNavis, subCtx: DutyContext): PanelChild = when (navi) {
        SidePanelNavis.Assets -> PanelChild.Assets(AssetsPanelDuty(subCtx))
        SidePanelNavis.Schemas -> PanelChild.Schemas(SchemasPanelDuty(subCtx))
    }
}

class AssetsPanelDuty(ctx: DutyContext) : DutyContext by ctx {
    data class AssetEntry(val id: String, val name: String, val kind: String)
    data class State(val keyword: String, val entries: List<AssetEntry>, val selectedId: String?)

    sealed interface Intent {
        data class Search(val keyword: String) : Intent
        data class Select(val id: String) : Intent
    }

    private val all = listOf(
        AssetEntry("asset-ryo-mark", "RyoMark", "Image"),
        AssetEntry("asset-lawnchair", "Lawnchair", "Image"),
        AssetEntry("asset-main-theme", "MainTheme", "Style"),
        AssetEntry("asset-headers", "Headers", "Font")
    )

    var keyword by mutableStateOf(""); private set
    var selectedId by mutableStateOf<String?>(all.firstOrNull()?.id); private set

    val entries: List<AssetEntry>
        get() = if (keyword.isBlank()) all else all.filter { it.name.contains(keyword, ignoreCase = true) || it.kind.contains(keyword, ignoreCase = true) }

    val state get() = State(keyword, entries, selectedId)

    fun dispatch(intent: Intent) {
        when (intent) {
            is Intent.Search -> keyword = intent.keyword
            is Intent.Select -> if (entries.any { it.id == intent.id }) selectedId = intent.id
        }
    }

    fun search(keyword: String) = dispatch(Intent.Search(keyword))

    fun select(id: String) = dispatch(Intent.Select(id))
}

class SchemasPanelDuty(ctx: DutyContext) : DutyContext by ctx {
    data class SchemaEntry(val id: String, val modelId: String, val memberCount: Int, val kind: String)
    data class State(val keyword: String, val entries: List<SchemaEntry>, val selectedId: String?)

    sealed interface Intent {
        data class Search(val keyword: String) : Intent
        data class Select(val id: String) : Intent
    }

    private val all = listOf(
        SchemaEntry("schema-actor", "game.Actor", 12, "FIELD"),
        SchemaEntry("schema-level", "game.Level", 7, "CTOR"),
        SchemaEntry("schema-audio", "game.AudioProfile", 5, "FIELD"),
        SchemaEntry("schema-material", "game.MaterialPreset", 9, "CUSTOM")
    )

    var keyword by mutableStateOf(""); private set
    var selectedId by mutableStateOf<String?>(all.firstOrNull()?.id); private set

    val entries: List<SchemaEntry>
        get() = if (keyword.isBlank()) all else all.filter { it.modelId.contains(keyword, ignoreCase = true) || it.kind.contains(keyword, ignoreCase = true) }

    val state get() = State(keyword, entries, selectedId)

    fun dispatch(intent: Intent) {
        when (intent) {
            is Intent.Search -> keyword = intent.keyword
            is Intent.Select -> if (entries.any { it.id == intent.id }) selectedId = intent.id
        }
    }

    fun search(keyword: String) = dispatch(Intent.Search(keyword))

    fun select(id: String) = dispatch(Intent.Select(id))
}