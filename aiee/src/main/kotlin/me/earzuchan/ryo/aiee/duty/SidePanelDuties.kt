package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.arkivanov.decompose.router.stack.ChildStack
import com.arkivanov.decompose.router.stack.StackNavigation
import com.arkivanov.decompose.router.stack.bringToFront
import com.arkivanov.decompose.router.stack.childStack
import com.arkivanov.decompose.value.Value
import io.github.vinceglb.filekit.FileKit
import io.github.vinceglb.filekit.dialogs.FileKitMode
import io.github.vinceglb.filekit.dialogs.openFilePicker
import io.github.vinceglb.filekit.path
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.runBlocking
import me.earzuchan.ryo.aiee.data.repository.WorkspaceRepository
import me.earzuchan.ryo.aiee.resources.*
import okio.Path
import okio.Path.Companion.toPath
import org.jetbrains.compose.resources.DrawableResource
import org.jetbrains.compose.resources.StringResource
import com.arkivanov.decompose.ComponentContext as DutyContext

class SidePanelDuty(ctx: DutyContext, private val workspaceRepo: WorkspaceRepository) : DutyContext by ctx {
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
    fun openVolumeByDialog() = pickVolumePathToOpen()?.let(workspaceRepo::openVolume)
    fun saveActiveVolume() = workspaceRepo.saveActiveVolume()
    fun closeActiveVolume() = workspaceRepo.closeActiveVolume()

    val workspaceState: StateFlow<WorkspaceRepository.State> get() = workspaceRepo.state
    val hasActiveVolume: Boolean get() = workspaceRepo.state.value.activeVolumeId != null

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
        SidePanelNavis.Assets -> PanelChild.Assets(AssetsPanelDuty(subCtx, workspaceRepo))
        SidePanelNavis.Schemas -> PanelChild.Schemas(SchemasPanelDuty(subCtx, workspaceRepo))
    }
}

class AssetsPanelDuty(ctx: DutyContext, private val workspaceRepo: WorkspaceRepository) : DutyContext by ctx {
    val state: StateFlow<WorkspaceRepository.State> = workspaceRepo.state

    fun volumeByPath(path: List<Int>): WorkspaceRepository.VolumeState? = path.firstOrNull()?.let { state.value.volumes.getOrNull(it) }

    fun saveVolume(volumeId: String) = workspaceRepo.saveVolume(volumeId)

    fun closeVolume(volumeId: String) = workspaceRepo.closeVolume(volumeId)

    fun onTreeNodeClick(path: List<Int>) {
        val volume = volumeByPath(path) ?: return
        workspaceRepo.selectVolume(volume.id)
    }
}

class SchemasPanelDuty(ctx: DutyContext, private val workspaceRepo: WorkspaceRepository) : DutyContext by ctx {
    val state: StateFlow<WorkspaceRepository.State> = workspaceRepo.state
}

// TODO：tmd，标题、预设扩展名什么的都没安排上
private fun pickVolumePathToOpen(): Path? = runBlocking {
    val file = FileKit.openFilePicker(mode = FileKitMode.Single)
    file?.path?.toPath()
}