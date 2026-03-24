package me.earzuchan.ryo.aiee.ui.panel

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxHeight
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import me.earzuchan.ryo.aiee.duty.AssetsPanelDuty
import me.earzuchan.ryo.aiee.duty.SchemasPanelDuty
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.component.EditableLabel
import me.earzuchan.ryo.aiee.ui.component.TreeNodeModel
import me.earzuchan.ryo.aiee.ui.component.TreeView
import me.earzuchan.ryo.aiee.ui.component.TreeViewState.Companion.rememberTreeViewState
import me.earzuchan.ryo.aiee.util.RyoLog
import org.jetbrains.compose.resources.StringResource

@Composable
fun AssetsPanel(duty: AssetsPanelDuty) {
    val TAG = "AssetsPanelDuty"

    var keyword by remember { mutableStateOf("") }

    PanelScaffold(Res.string.panel_assets_manager, keyword, { keyword = it }) {
        val sampleNodes = remember {
            listOf(TreeNodeModel("卷1", listOf(TreeNodeModel("条目xxx"), TreeNodeModel("条目yyy"))), TreeNodeModel("新卷", emptyList()))
        }
        val treeState = rememberTreeViewState()

        TreeView(sampleNodes, filterText = keyword, state = treeState, onNodeClick = { RyoLog.i(TAG, "Clicked Leaf Path: $it") }, onNodeRightClick = { RyoLog.i(TAG, "Right Clicked Path: $it") })
    }
}

@Composable
fun SchemasPanel(duty: SchemasPanelDuty) {
    // TODO：设计实际Schema管理面板，对接实际逻辑
    PanelScaffold(Res.string.panel_schemas_manager, "嗯嘛", {}) { }
}

@Composable
private fun PanelScaffold(searchHint: StringResource, keyword: String, onKeywordChange: (String) -> Unit, content: @Composable () -> Unit) = Column(Modifier.fillMaxSize()) {
    EditableLabel(UiText.Res(searchHint), keyword, onKeywordChange, true, Modifier.fillMaxWidth())
    Box(Modifier.fillMaxHeight(), Alignment.TopStart) { content() }
}