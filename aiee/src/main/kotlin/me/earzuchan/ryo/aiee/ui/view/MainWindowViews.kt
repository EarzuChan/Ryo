package me.earzuchan.ryo.aiee.ui.view

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.scrollBy
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.lazy.LazyRow
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.lazy.rememberLazyListState
import androidx.compose.foundation.shape.CircleShape
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.window.WindowDraggableArea
import androidx.compose.material3.HorizontalDivider
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButtonColors
import androidx.compose.material3.IconButtonDefaults
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.geometry.Offset
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.PointerIcon
import androidx.compose.ui.input.pointer.isSecondaryPressed
import androidx.compose.ui.input.pointer.onPointerEvent
import androidx.compose.ui.input.pointer.pointerInput
import androidx.compose.ui.input.pointer.pointerHoverIcon
import androidx.compose.ui.layout.boundsInWindow
import androidx.compose.ui.layout.onGloballyPositioned
import androidx.compose.ui.layout.positionInWindow
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.FrameWindowScope
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.duty.MenuDuty
import me.earzuchan.ryo.aiee.duty.SideWorkspaceDuty
import me.earzuchan.ryo.aiee.duty.TabDuty
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.RyoIconButton
import me.earzuchan.ryo.aiee.ui.component.RyoButton
import me.earzuchan.ryo.aiee.ui.component.RyoMenu
import me.earzuchan.ryo.aiee.ui.page.EditorSessionPage
import me.earzuchan.ryo.aiee.ui.page.EmptyPage
import me.earzuchan.ryo.aiee.ui.page.SettingsPage
import me.earzuchan.ryo.aiee.ui.page.WelcomePage
import me.earzuchan.ryo.aiee.util.ResUtils.text
import me.earzuchan.ryo.aiee.util.ResUtils.vector
import org.jetbrains.compose.resources.DrawableResource
import sh.calvin.reorderable.ReorderableItem
import sh.calvin.reorderable.rememberReorderableLazyListState
import java.awt.Cursor
import kotlin.math.abs
import kotlin.math.roundToInt

@Composable
fun SideWorkspaceView(sideWorkspaceDuty: SideWorkspaceDuty, onOpenSettings: () -> Unit) {
    @Composable
    fun PanelButton(icon: DrawableResource, hint: String, selected: Boolean, onClick: () -> Unit) = RyoIconButton(icon, 48, hint, if (selected) IconButtonDefaults.filledTonalIconButtonColors() else IconButtonDefaults.iconButtonColors(), onClick)

    @Composable
    fun ActionButton(icon: DrawableResource, hint: String, colors: IconButtonColors = IconButtonDefaults.iconButtonColors(), onClick: () -> Unit) = RyoIconButton(icon = icon, dpSize = 48, hintText = hint, colors = colors, onClick = onClick)

    val activePanel = sideWorkspaceDuty.activePanel
    val density = LocalDensity.current

    Row(Modifier.fillMaxHeight()) {
        Column(Modifier.fillMaxHeight().width(64.dp).padding(vertical = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Column(Modifier.weight(1F), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                sideWorkspaceDuty.panels.forEach { panel ->
                    PanelButton(if (sideWorkspaceDuty.activePanelId == panel.id) panel.selectedIcon else panel.icon, panel.title, sideWorkspaceDuty.activePanelId == panel.id) { sideWorkspaceDuty.focusPanel(panel.id) }
                }
            }

            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                ActionButton(if (sideWorkspaceDuty.expanded) Res.drawable.ic_panel_narrow_24px else Res.drawable.ic_panel_24px, "展开收起", onClick = sideWorkspaceDuty::toggleExpanded)
                ActionButton(Res.drawable.ic_settings_24px, "设置", onClick = onOpenSettings)
            }
        }

        if (!sideWorkspaceDuty.expanded) return

        Box(Modifier.fillMaxHeight().width(sideWorkspaceDuty.panelWidthDp.dp).clip(RoundedCornerShape(topStart = 16.dp, topEnd = 16.dp)).background(MaterialTheme.colorScheme.surfaceContainerHigh)) {
            Column(Modifier.fillMaxSize().padding(horizontal = 16.dp, vertical = 12.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                Text(activePanel.title, style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.onSurfaceVariant)
                Text("面板内容区待接入", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            }
        }

        Spacer(Modifier.fillMaxHeight().width(8.dp).padding(end = 4.dp).pointerHoverIcon(PointerIcon(Cursor(Cursor.E_RESIZE_CURSOR))).pointerInput(Unit) {
            detectDragGestures { change, dragAmount ->
                change.consume()
                sideWorkspaceDuty.resizeBy(with(density) { dragAmount.x.toDp().value })
            }
        })
    }
}

@Composable
@OptIn(ExperimentalComposeUiApi::class)
fun MainWorkspaceView(appDuty: AppDuty) = Column(Modifier.fillMaxSize().clip(RoundedCornerShape(topStart = 16.dp)).background(MaterialTheme.colorScheme.surfaceContainer)) {
    @Composable
    fun TabChip(tab: TabDuty.Tab, selected: Boolean, onSelect: () -> Unit, onClose: () -> Unit, onContextMenu: (anchorX: Int, anchorY: Int) -> Unit = { _, _ -> }, modifier: Modifier = Modifier) {
        val textColor = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurfaceVariant
        val indicatorColor = if (selected) MaterialTheme.colorScheme.primary else Color.Transparent
        var topLeftInWindow by remember(tab.id) { mutableStateOf(Offset.Zero) }

        @Composable
        @OptIn(ExperimentalComposeUiApi::class)
        fun TabTrailingAction(tab: TabDuty.Tab, selected: Boolean, onClose: () -> Unit) {
            var hovered by remember(tab.id) { mutableStateOf(false) }
            val tint = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurfaceVariant
            val showClose = !tab.dirty || hovered

            Box(Modifier.size(24.dp).onPointerEvent(PointerEventType.Enter) { hovered = true }.onPointerEvent(PointerEventType.Exit) { hovered = false }.clip(CircleShape).clickable(onClick = onClose), Alignment.Center) {
                Icon((if (showClose) Res.drawable.ic_tab_close_24px else Res.drawable.ic_tab_unsaved_24px).vector, "Tab按钮", Modifier.size(24.dp), tint)
            }
        }

        Column(
            modifier.height(35.dp).width(IntrinsicSize.Max).onGloballyPositioned { topLeftInWindow = it.positionInWindow() }.onPointerEvent(PointerEventType.Press) { event ->
                if (!event.buttons.isSecondaryPressed) return@onPointerEvent
                val localPress = event.changes.firstOrNull()?.position ?: return@onPointerEvent
                onContextMenu((topLeftInWindow.x + localPress.x).roundToInt(), (topLeftInWindow.y + localPress.y).roundToInt())
            }.clickable(onClick = onSelect)
        ) {
            Spacer(Modifier.fillMaxWidth().height(3.dp))
            Row(Modifier.fillMaxWidth().weight(1F).padding(start = 12.dp, end = 6.dp), Arrangement.spacedBy(6.dp), Alignment.CenterVertically) {
                Text(tab.title, Modifier, textColor, maxLines = 1, softWrap = false, style = MaterialTheme.typography.labelLarge)
                TabTrailingAction(tab, selected, onClose)
            }
            Box(Modifier.fillMaxWidth().height(2.dp).background(indicatorColor))
        }
    }

    // 以下为Tab Chips
    if (appDuty.tabs.isNotEmpty()) Column(Modifier.fillMaxWidth()) {
        val lazyListState = rememberLazyListState()
        val scope = rememberCoroutineScope()
        val reorderState = rememberReorderableLazyListState(lazyListState) { from, to -> appDuty.moveTab(from.index, to.index) }

        LazyRow(
            Modifier.fillMaxWidth().onPointerEvent(PointerEventType.Scroll) { event ->
                val delta = event.changes.firstOrNull()?.scrollDelta ?: return@onPointerEvent
                val direction = if (abs(delta.x) > abs(delta.y)) delta.x else delta.y
                if (direction == 0F) return@onPointerEvent
                scope.launch { lazyListState.scrollBy(direction * 50F) }
            }, lazyListState
        ) {
            items(appDuty.tabs, key = { it.id }) { tab ->
                ReorderableItem(reorderState, key = tab.id) {
                    TabChip(tab, appDuty.activeTabId == tab.id, { appDuty.selectTab(tab.id) }, { appDuty.requestCloseTab(tab.id) }, { anchorX, anchorY -> appDuty.showTabContextMenu(tab.id, anchorX, anchorY) }, Modifier.draggableHandle())
                }
            }
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant, thickness = 1.dp)
    }

    // TIPS：下面是真渲染Tab内容，以后要接入真实业务
    Box(Modifier.fillMaxSize()) {
        val activeTab = appDuty.activeTab
        if (activeTab == null) EmptyPage() else when (activeTab.spec) {
            TabDuty.TabSpec.Welcome -> WelcomePage()
            is TabDuty.TabSpec.EditorSession -> EditorSessionPage()
            TabDuty.TabSpec.Settings -> SettingsPage(appDuty)
        }
    }
}

@Composable
@OptIn(ExperimentalComposeUiApi::class)
fun FrameWindowScope.AppTopBarView(appDuty: AppDuty, windowControlButtons: @Composable () -> Unit) = WindowDraggableArea {
    val anchors = remember { mutableStateMapOf<String, IntOffset>() }

    Row(Modifier.fillMaxWidth().height(56.dp)) {
        Box(Modifier.fillMaxHeight().width(64.dp), Alignment.Center) { Icon(Res.drawable.ic_ryo_24px.vector, Res.string.app_name.text, Modifier.size(24.dp), MaterialTheme.colorScheme.onSurfaceVariant) }

        Row(Modifier.fillMaxSize().padding(horizontal = 4.dp), verticalAlignment = Alignment.CenterVertically) {
            // 菜单栏
            Box(Modifier.weight(1F)) {
                Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(4.dp), verticalAlignment = Alignment.CenterVertically) {
                    appDuty.menuGroups.forEach { group ->
                        RyoButton(group.label, Modifier.height(36.dp).onGloballyPositioned { coords ->
                            val b = coords.boundsInWindow()
                            anchors[group.id] = IntOffset(b.left.roundToInt(), b.bottom.roundToInt())
                        }.onPointerEvent(PointerEventType.Enter) {
                            anchors[group.id]?.also { appDuty.hoverMenuGroup(group.id, it.x, it.y) }
                        }) { anchors[group.id]?.also { appDuty.toggleMenuGroup(group.id, it.x, it.y) } }
                    }
                }
            }

            // 三键（靠注入）
            windowControlButtons()
        }
    }
}
