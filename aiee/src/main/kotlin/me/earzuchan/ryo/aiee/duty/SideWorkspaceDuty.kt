package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.arkivanov.decompose.ComponentContext
import com.arkivanov.decompose.router.stack.ChildStack
import com.arkivanov.decompose.router.stack.StackNavigation
import com.arkivanov.decompose.router.stack.active
import com.arkivanov.decompose.router.stack.bringToFront
import com.arkivanov.decompose.router.stack.childStack
import com.arkivanov.decompose.value.Value
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import org.jetbrains.compose.resources.DrawableResource
import org.jetbrains.compose.resources.StringResource

class SideWorkspaceDuty(ctx: ComponentContext) : ComponentContext by ctx {
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

    data class Panel(val id: String, val navi: SidePanelNavi, val titleRes: StringResource, val icon: DrawableResource, val selectedIcon: DrawableResource = icon)

    class AssetsPanelDuty(ctx: ComponentContext) : ComponentContext by ctx

    class SchemasPanelDuty(ctx: ComponentContext) : ComponentContext by ctx

    sealed interface PanelChild {
        val panelId: String
        val titleRes: StringResource

        data class Assets(val duty: AssetsPanelDuty) : PanelChild {
            override val panelId = SidePanelNavi.Assets.id
            override val titleRes = Res.string.panel_assets_manager
        }

        data class Schemas(val duty: SchemasPanelDuty) : PanelChild {
            override val panelId = SidePanelNavi.Schemas.id
            override val titleRes = Res.string.panel_schemas_manager
        }
    }

    private val navigation = StackNavigation<SidePanelNavi>()

    val panelStack: Value<ChildStack<SidePanelNavi, PanelChild>> = childStack(
        source = navigation,
        serializer = SidePanelNavi.serializer(),
        initialConfiguration = SidePanelNavi.Assets,
        key = "SidePanelStack",
        handleBackButton = false,
        childFactory = ::mapPanelChild
    )

    val panels = listOf(
        Panel(SidePanelNavi.Assets.id, SidePanelNavi.Assets, Res.string.panel_assets_manager, Res.drawable.ic_list_24px, Res.drawable.ic_list_filled_24px),
        Panel(SidePanelNavi.Schemas.id, SidePanelNavi.Schemas, Res.string.panel_schemas_manager, Res.drawable.ic_schemas_24px, Res.drawable.ic_schemas_filled_24px)
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

    private fun mapPanelChild(navi: SidePanelNavi, subCtx: ComponentContext): PanelChild = when (navi) {
        SidePanelNavi.Assets -> PanelChild.Assets(AssetsPanelDuty(subCtx))
        SidePanelNavi.Schemas -> PanelChild.Schemas(SchemasPanelDuty(subCtx))
    }
}
