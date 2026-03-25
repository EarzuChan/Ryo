package me.earzuchan.ryo.aiee.ui.panel

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import me.earzuchan.ryo.aiee.duty.AssetsPanelDuty
import me.earzuchan.ryo.aiee.duty.SchemasPanelDuty
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.component.EditableLabel
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.component.TreeNodeModel
import me.earzuchan.ryo.aiee.ui.component.TreeView
import me.earzuchan.ryo.aiee.ui.component.TreeViewState.Companion.rememberTreeViewState
import me.earzuchan.ryo.aiee.util.UiUtils.text
import org.jetbrains.compose.resources.StringResource

@Composable
private fun String?.PanelErr() = this?.also { Text(it, Modifier.fillMaxWidth().padding(vertical = 16.dp), MaterialTheme.colorScheme.error, style = MaterialTheme.typography.bodyMedium) }

@Composable
private fun StringResource.EmptyStr()=Box(Modifier.fillMaxSize().padding(bottom = 12.dp), Alignment.Center) { Text(text, color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.bodyMedium) }

@Composable
private fun PanelScaffold(searchHint: StringResource, keyword: String, onKeywordChange: (String) -> Unit, content: @Composable () -> Unit) = Column(Modifier.fillMaxSize()) {
    EditableLabel(UiText.Res(searchHint), keyword, onKeywordChange, true, Modifier.fillMaxWidth())
    Box(Modifier.fillMaxHeight(), Alignment.TopStart) { content() }
}

@Composable
fun SchemasPanel(duty: SchemasPanelDuty) {
    var keyword by remember { mutableStateOf("") }
    val treeState = rememberTreeViewState()

    val state by duty.state.collectAsState()

    val nodes = remember(state.schemas) { state.schemas.map { TreeNodeModel(it.modelId, listOf(TreeNodeModel("kind: ${it.kind.name}")) + it.members.map(::TreeNodeModel)) } }

    PanelScaffold(Res.string.panel_schemas_manager, keyword, { keyword = it }) {
        state.lastError.PanelErr()

        if (nodes.isEmpty()) Res.string.panel_schemas_empty.EmptyStr()
        else TreeView(nodes, Modifier.fillMaxSize(), keyword, state = treeState)
    }
}

@Composable
fun AssetsPanel(duty: AssetsPanelDuty, onShowContextMenu: (anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) -> Unit) {
    var keyword by remember { mutableStateOf("") }
    val treeState = rememberTreeViewState()

    val state by duty.state.collectAsState()
    val volumeInfoText = Res.string.menu_item_volume_info.text
    val saveText = Res.string.menu_item_save.text
    val closeText = Res.string.action_close.text

    val nodes = remember(state.volumes) { state.volumes.map { volume -> TreeNodeModel(volume.name, volume.tokens.map(::TreeNodeModel)) } }

    LaunchedEffect(state.activeVolumeId, state.volumes) {
        val index = state.volumes.indexOfFirst { it.id == state.activeVolumeId }
        if (index < 0) return@LaunchedEffect
        val focusedPath = treeState.pathOfLastClickedNode
        if (focusedPath.firstOrNull() == index) return@LaunchedEffect
        treeState.locatePath(listOf(index))
    }

    PanelScaffold(Res.string.panel_assets_manager, keyword, { keyword = it }) {
        Column(Modifier.fillMaxSize()) {
            state.lastError.PanelErr()

            if (nodes.isEmpty())  Res.string.panel_assets_empty.EmptyStr()
            else TreeView(nodes, Modifier.fillMaxSize(), keyword, state = treeState, onNodeClick = duty::onTreeNodeClick, onNodeRightClick = { path, anchorX, anchorY ->
                if (path.size != 1) return@TreeView
                val volume = duty.volumeByPath(path) ?: return@TreeView

                duty.onTreeNodeClick(path)
                onShowContextMenu(anchorX, anchorY, listOf(RyoMenuEntry.MenuItem(volumeInfoText, false), RyoMenuEntry.MenuItem(saveText) { duty.saveVolume(volume.id) }, RyoMenuEntry.MenuItem(closeText) { duty.closeVolume(volume.id) }))
            })
        }
    }
}