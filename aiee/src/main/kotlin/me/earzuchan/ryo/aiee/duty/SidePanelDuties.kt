package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.arkivanov.decompose.router.stack.ChildStack
import com.arkivanov.decompose.router.stack.StackNavigation
import com.arkivanov.decompose.router.stack.bringToFront
import com.arkivanov.decompose.router.stack.childStack
import com.arkivanov.decompose.value.Value
import kotlinx.coroutines.ExperimentalCoroutinesApi
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.combine
import kotlinx.coroutines.flow.flatMapLatest
import kotlinx.coroutines.flow.flowOf
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import me.earzuchan.ryo.aiee.app.CommandService
import me.earzuchan.ryo.aiee.app.WorkspaceService
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.TreeNodeModel
import me.earzuchan.ryo.aiee.util.CoroutineObject
import org.jetbrains.compose.resources.DrawableResource
import org.jetbrains.compose.resources.StringResource
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import kotlin.collections.emptyList
import kotlin.coroutines.CoroutineContext
import com.arkivanov.decompose.ComponentContext as DutyContext

class SidePanelDuty(ctx: DutyContext) : DutyContext by ctx, KoinComponent {
    val commandService by inject<CommandService>()

    companion object {
        private const val DEFAULT_WIDTH_DP = 280F
        private const val COLLAPSE_THRESHOLD_DP = 80F
        private const val MAX_WIDTH_DP = 560F

        const val CMD_TOGGLE = "side_panel.toggle"
    }

    data class Panel(val id: String, val navi: SidePanelNavis, val title: StringResource, val icon: DrawableResource, val selectedIcon: DrawableResource = icon)

    private val navigation = StackNavigation<SidePanelNavis>()

    val panelStack: Value<ChildStack<SidePanelNavis, Any>> = childStack(navigation, SidePanelNavis.serializer(), SidePanelNavis.Assets, "SidePanelStack", false, ::mapChild)

    val panels = listOf(
        Panel(SidePanelNavis.Assets.id, SidePanelNavis.Assets, Res.string.panel_assets_manager, Res.drawable.ic_list_24px, Res.drawable.ic_list_filled_24px),
        Panel(SidePanelNavis.Schemas.id, SidePanelNavis.Schemas, Res.string.panel_schemas_manager, Res.drawable.ic_schemas_24px, Res.drawable.ic_schemas_filled_24px)
    )

    var expanded by mutableStateOf(true); private set
    var panelWidthDp by mutableStateOf(DEFAULT_WIDTH_DP); private set
    private var collapsedByDrag by mutableStateOf(false)

    init {
        commandService.register(CMD_TOGGLE) { toggle() }
    }

    // --- 本地视图交互API ---

    fun focus(id: String) {
        val panel = panels.firstOrNull { it.id == id } ?: return
        navigation.bringToFront(panel.navi)

        if (!expanded) {
            if (collapsedByDrag) panelWidthDp = DEFAULT_WIDTH_DP
            collapsedByDrag = false
            expanded = true
        }
    }

    fun resizeBy(deltaDp: Float) {
        val targetWidth = panelWidthDp + deltaDp
        if (targetWidth <= COLLAPSE_THRESHOLD_DP) {
            collapsedByDrag = true
            expanded = false
        } else {
            panelWidthDp = targetWidth.coerceAtMost(MAX_WIDTH_DP)
            collapsedByDrag = false
            expanded = true
        }
    }

    fun toggle() = if (expanded) expanded = false else {
        if (collapsedByDrag) panelWidthDp = DEFAULT_WIDTH_DP
        collapsedByDrag = false
        expanded = true
    }

    fun mentionSettings() = commandService.dispatch(MainPanelDuty.CMD_MENTION_SETTINGS)

    // --- 私有实现 ---

    // 直接映射到真实的子 Duty，UI 层通过 when(val duty = instance) 来分配对应的 Composable
    private fun mapChild(navi: SidePanelNavis, subCtx: DutyContext): Any = when (navi) {
        SidePanelNavis.Assets -> AssetsPanelDuty(subCtx)
        SidePanelNavis.Schemas -> SchemasPanelDuty(subCtx)
    }
}

class AssetsPanelDuty(ctx: DutyContext) : DutyContext by ctx, KoinComponent, CoroutineObject() {
    val workspaceService by inject<WorkspaceService>()

    val keyword = mutableStateOf("")

    @OptIn(ExperimentalCoroutinesApi::class)
    val nodes: StateFlow<List<TreeNodeModel>> = workspaceService.volumes.flatMapLatest { volumeList ->
        if (volumeList.isEmpty()) flowOf(emptyList()) else {
            val nodeFlows: List<Flow<TreeNodeModel>> = volumeList.map { vol ->
                vol.volume.entries.map { items -> TreeNodeModel(vol.displayName, items.keys.map { TreeNodeModel(it) }) }
            }

            // 使用 combine 将所有的 Flow<TreeNodeModel> 组合成一个 Flow<List<TreeNodeModel>>
            combine(nodeFlows) { it.toList() }
        }
    }.stateIn(scope, SharingStarted.WhileSubscribed(5000), emptyList())

    fun getVolumeUUID(index: Int) = runCatching{ workspaceService.volumes.value[index].id }.getOrNull()
}

class SchemasPanelDuty(ctx: DutyContext) : DutyContext by ctx