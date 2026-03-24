package me.earzuchan.ryo.aiee.ui.component

import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.animateFloatAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.*
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsHoveredAsState
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.LazyListState
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.draw.rotate
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.PointerButton
import androidx.compose.ui.input.pointer.PointerIcon
import androidx.compose.ui.input.pointer.pointerHoverIcon
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.entries
import me.earzuchan.ryo.aiee.resources.entry
import me.earzuchan.ryo.aiee.resources.ic_chevron_24px
import me.earzuchan.ryo.aiee.resources.ic_file_24px
import me.earzuchan.ryo.aiee.resources.ic_file_filled_24px
import me.earzuchan.ryo.aiee.resources.matches
import me.earzuchan.ryo.aiee.ui.component.TreeViewState.Companion.rememberTreeViewState
import me.earzuchan.ryo.aiee.util.UiUtils.text
import me.earzuchan.ryo.aiee.util.UiUtils.vector

// TIPS：叫做View，实际上这是个不可分的最小单位，即视为组件，Vamos！

data class TreeNodeModel(val name: String, val children: List<TreeNodeModel>? = null)

class TreeViewState(val lazyListState: LazyListState) {
    var nonExpandedNodePaths by mutableStateOf<Set<List<Int>>>(emptySet())
    var pathOfLastClickedNode by mutableStateOf<List<Int>>(emptyList())
    var pendingScrollRequest by mutableStateOf<List<Int>?>(null); internal set

    fun clearLastClicked() {
        pathOfLastClickedNode = emptyList()
    }

    fun locatePath(path: List<Int>) {
        val newNonExpanded = nonExpandedNodePaths.toMutableSet()
        for (depth in 1 until path.size) newNonExpanded.remove(path.subList(0, depth))
        nonExpandedNodePaths = newNonExpanded
        pathOfLastClickedNode = path
        pendingScrollRequest = path
    }

    companion object {
        @Composable
        fun rememberTreeViewState() = remember { TreeViewState(LazyListState(0, 0)) }
    }
}

private data class InternalTreeNode(val name: String, val level: Int, val isStem: Boolean, val indexPath: List<Int>, val expanded: Boolean, val childrenCount: Int, val stableKey: String) // 预计算 Key

@OptIn(ExperimentalFoundationApi::class)
@Composable
fun TreeView(nodes: List<TreeNodeModel>, modifier: Modifier = Modifier, filterText: String = "", indent: Int = 24, state: TreeViewState = rememberTreeViewState(), onNodeClick: (path: List<Int>) -> Unit = {}, onNodeRightClick: (path: List<Int>) -> Unit = {}) { // 将过滤逻辑提取出来，并优化计算
    val searched by derivedStateOf { filterText.isNotBlank() }

    val processedTree by remember(nodes, filterText, state.nonExpandedNodePaths) {
        derivedStateOf {
            fun process(treeNodes: List<TreeNodeModel>?, level: Int = 0, parentPath: List<Int> = emptyList()): List<InternalTreeNode> {
                val result = mutableListOf<InternalTreeNode>()

                treeNodes?.forEachIndexed { index, node ->
                    val isStem = node.children != null
                    val path = parentPath + index
                    val expanded = isStem && !state.nonExpandedNodePaths.contains(path)

                    // 预判：如果过滤词为空，则全部显示；否则判断当前或子项是否命中
                    val matches = filterText.isBlank() || node.name.contains(filterText, ignoreCase = true)
                    val shouldTake = isStem || matches

                    if (shouldTake) {
                        var displayChildrenCount = 0
                        var flattenedChildren: List<InternalTreeNode> = emptyList()

                        if (isStem) if (expanded) {
                            // 避免两回计算：展开状态下：先递归处理子树
                            flattenedChildren = process(node.children, level + 1, path)

                            // 超绝复用，直接从刚刚处理好的展开列表中找：递归返回的列表里，层级l+1的就是被保留下来的直接子节点；这样完美避免了再次遍历去匹配 contains 字符串。
                            displayChildrenCount = if (filterText.isBlank()) node.children.size ?: 0 else flattenedChildren.count { it.level == level + 1 }
                        } else displayChildrenCount = (if (filterText.isBlank()) node.children.size else node.children.count { child -> child.children != null || child.name.contains(filterText, ignoreCase = true) } ?: 0)
                        // 修复性能隐患：避免乱深度递归：折叠状态下，原代码即使折叠也会递归整个树去算 process；优化后，遇到折叠节点绝不深层递归，只轻量地看一眼它的第一层直接子节点来算 count 即可

                        result.add(InternalTreeNode(node.name, level, isStem, path, expanded, displayChildrenCount, path.joinToString(",")))

                        // 如果是展开状态，把刚刚计算好的后代节点追加到扁平列表中
                        if (isStem && expanded) result.addAll(flattenedChildren)
                    }
                }

                return result
            }

            process(nodes)
        }
    }

    // 滚动优化：使用 LaunchedEffect 监听请求，但避免过度依赖 processedTree 的高频变化
    LaunchedEffect(state.pendingScrollRequest) {
        state.pendingScrollRequest?.let { target ->
            val idx = processedTree.indexOfFirst { it.indexPath == target }
            if (idx != -1) {
                state.lazyListState.animateScrollToItem(idx)
                state.pendingScrollRequest = null
            }
        }
    }

    LazyColumn(modifier.fillMaxHeight(), state.lazyListState) {
        items(processedTree, { it.stableKey }, { it.isStem }) { node ->
            val isSelected = state.pathOfLastClickedNode == node.indexPath
            val hoverSource = remember { MutableInteractionSource() }
            val isHovered by hoverSource.collectIsHoveredAsState()

            // 动画部分
            val animatedRadius by animateDpAsState(if (isSelected || isHovered) 18.dp else 0.dp, tween(300, easing = FastOutSlowInEasing))
            val targetColor = when {
                isSelected -> MaterialTheme.colorScheme.primaryContainer
                isHovered -> MaterialTheme.colorScheme.onPrimaryContainer.copy(0.08F)
                else -> Color.Transparent
            }
            val animatedColor by animateColorAsState(targetColor, tween(300, easing = FastOutSlowInEasing))
            val contentColor = if (isSelected) MaterialTheme.colorScheme.onPrimaryContainer else MaterialTheme.colorScheme.onSurfaceVariant

            Row(Modifier.fillMaxWidth().height(36.dp).clip(RoundedCornerShape(animatedRadius)).background(animatedColor).hoverable(hoverSource).pointerHoverIcon(PointerIcon.Hand).onClick(matcher = PointerMatcher.mouse(PointerButton.Primary)) {
                state.pathOfLastClickedNode = node.indexPath
                if (node.isStem) {
                    val set = state.nonExpandedNodePaths.toMutableSet()
                    if (node.expanded) set.add(node.indexPath) else set.remove(node.indexPath)
                    state.nonExpandedNodePaths = set
                } else onNodeClick(node.indexPath)
            }.onClick(matcher = PointerMatcher.mouse(PointerButton.Secondary)) { onNodeRightClick(node.indexPath) }.padding(start = (node.level * indent + 12).dp, end = 24.dp), Arrangement.spacedBy(12.dp), Alignment.CenterVertically) {
                val iconRotation by animateFloatAsState(if (node.expanded) 90f else 0f, tween(300))

                Icon((if (node.isStem) Res.drawable.ic_chevron_24px else if (isSelected) Res.drawable.ic_file_filled_24px else Res.drawable.ic_file_24px).vector, null, Modifier.rotate(iconRotation), contentColor)

                Text(node.name, Modifier.weight(1f), contentColor, style = MaterialTheme.typography.labelLarge, maxLines = 1, overflow = TextOverflow.Ellipsis)

                Text(if (node.isStem) (if (searched) Res.string.matches else Res.string.entries).text(node.childrenCount.toString()) else Res.string.entry.text, color = contentColor, style = MaterialTheme.typography.labelLarge)
            }
        }
    }
}