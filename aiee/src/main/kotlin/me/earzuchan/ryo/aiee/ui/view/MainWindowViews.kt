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
import com.arkivanov.decompose.extensions.compose.subscribeAsState
import com.arkivanov.decompose.extensions.compose.stack.Children
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.duty.AppCommand
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.duty.SideWorkspaceDuty
import me.earzuchan.ryo.aiee.duty.TabDuty
import me.earzuchan.ryo.aiee.duty.WorkspaceTabNavi
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.RyoIconButton
import me.earzuchan.ryo.aiee.ui.component.RyoButton
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.resolve
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
    fun PanelButton(icon: DrawableResource, hint: String, selected: Boolean, onClick: () -> Unit) =
        RyoIconButton(icon, 48, hint, if (selected) IconButtonDefaults.filledTonalIconButtonColors() else IconButtonDefaults.iconButtonColors(), onClick)

    @Composable
    fun ActionButton(icon: DrawableResource, hint: String, colors: IconButtonColors = IconButtonDefaults.iconButtonColors(), onClick: () -> Unit) = RyoIconButton(icon = icon, dpSize = 48, hintText = hint, colors = colors, onClick = onClick)

    val density = LocalDensity.current
    val panelStack by sideWorkspaceDuty.panelStack.subscribeAsState()
    val activePanelId = panelStack.active.configuration.id

    Row(Modifier.fillMaxHeight()) {
        Column(Modifier.fillMaxHeight().width(64.dp).padding(vertical = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Column(Modifier.weight(1F), verticalArrangement = Arrangement.spacedBy(4.dp)) {
                sideWorkspaceDuty.panels.forEach { panel ->
                    PanelButton(if (activePanelId == panel.id) panel.selectedIcon else panel.icon, panel.titleRes.text, activePanelId == panel.id) { sideWorkspaceDuty.focusPanel(panel.id) }
                }
            }

            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                ActionButton(if (sideWorkspaceDuty.expanded) Res.drawable.ic_panel_narrow_24px else Res.drawable.ic_panel_24px, Res.string.side_toggle_panel.text, onClick = sideWorkspaceDuty::toggleExpanded)
                ActionButton(Res.drawable.ic_settings_24px, Res.string.side_open_settings.text, onClick = onOpenSettings)
            }
        }

        if (!sideWorkspaceDuty.expanded) return

        Box(Modifier.fillMaxHeight().width(sideWorkspaceDuty.panelWidthDp.dp).clip(RoundedCornerShape(topStart = 16.dp, topEnd = 16.dp)).background(MaterialTheme.colorScheme.surfaceContainerHigh)) {
            Children(sideWorkspaceDuty.panelStack) { child ->
                Column(Modifier.fillMaxSize().padding(horizontal = 16.dp, vertical = 12.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
                    Text(child.instance.titleRes.text, style = MaterialTheme.typography.labelLarge, color = MaterialTheme.colorScheme.onSurfaceVariant)
                    Text(Res.string.side_panel_placeholder.text, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
                }
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
    val tabStack by appDuty.workspaceDuty.tabStack.subscribeAsState()
    val activeTabId = tabStack.active.configuration.takeIf { it !is WorkspaceTabNavi.Empty }?.id
    val tabs = appDuty.tabs

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
                Icon((if (showClose) Res.drawable.ic_tab_close_24px else Res.drawable.ic_tab_unsaved_24px).vector, Res.string.tab_action_cd.text, Modifier.size(24.dp), tint)
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
                Text(tab.title.resolve(), Modifier, textColor, maxLines = 1, softWrap = false, style = MaterialTheme.typography.labelLarge)
                TabTrailingAction(tab, selected, onClose)
            }
            Box(Modifier.fillMaxWidth().height(2.dp).background(indicatorColor))
        }
    }

    // 以下为Tab Chips
    if (tabs.isNotEmpty()) Column(Modifier.fillMaxWidth()) {
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
            items(tabs, key = { it.id }) { tab ->
                val hasOtherTabs = tabs.size > 1
                val hasTabs = tabs.isNotEmpty()
                val menuLabel: (String, AppCommand) -> String = { base, command -> appDuty.shortcutFor(command)?.displayText()?.let { "$base\t$it" } ?: base }
                val tabTitle = tab.title.resolve()
                val closeText = Res.string.tab_context_close_format.text(tabTitle)
                val closeOthersText = Res.string.tab_context_close_others.text
                val closeAllText = Res.string.tab_context_close_all.text
                val newEditorText = menuLabel(Res.string.menu_item_new_editor_session_page.text, AppCommand.OpenEditorSessionTab)
                val canOpenEditor = appDuty.commandDuty.canExecute(AppCommand.OpenEditorSessionTab)

                ReorderableItem(reorderState, key = tab.id) {
                    TabChip(tab, activeTabId == tab.id, { appDuty.selectTab(tab.id) }, { appDuty.requestCloseTab(tab.id) }, { anchorX, anchorY ->
                        appDuty.showContextMenu(
                            anchorX = anchorX,
                            anchorY = anchorY,
                            entries = listOf(
                                RyoMenuEntry.MenuItem(text = closeText, onClick = { appDuty.requestCloseTab(tab.id) }),
                                RyoMenuEntry.MenuItem(text = closeOthersText, enabled = hasOtherTabs, onClick = { appDuty.requestCloseOtherTabs(tab.id) }),
                                RyoMenuEntry.MenuItem(text = closeAllText, enabled = hasTabs, onClick = appDuty::requestCloseAllTabs),
                                RyoMenuEntry.Divider,
                                RyoMenuEntry.MenuItem(
                                    text = newEditorText,
                                    enabled = canOpenEditor,
                                    onClick = { appDuty.commandDuty.execute(AppCommand.OpenEditorSessionTab) }
                                )
                            )
                        )
                    }, Modifier.draggableHandle())
                }
            }
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant, thickness = 1.dp)
    }

    Box(Modifier.fillMaxSize()) { Children(appDuty.workspaceDuty.tabStack) { child -> child.instance.Render(appDuty) } }
}

@Composable
@OptIn(ExperimentalComposeUiApi::class)
fun FrameWindowScope.AppTopBarView(appDuty: AppDuty, windowControlButtons: @Composable () -> Unit) {
    data class MenuGroup(val id: String, val label: String, val entries: List<RyoMenuEntry>)

    val menuLabel: (String, AppCommand) -> String = { base, command -> appDuty.shortcutFor(command)?.displayText()?.let { "$base\t$it" } ?: base }
    val menuGroups = listOf(
        MenuGroup(
            id = "file",
            label = Res.string.menu_group_file.text,
            entries = listOf(
                RyoMenuEntry.MenuItem(
                    text = Res.string.menu_item_new.text,
                    children = listOf(
                        RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_welcome_page.text, AppCommand.OpenWelcomeTab), appDuty.commandDuty.canExecute(AppCommand.OpenWelcomeTab)) { appDuty.commandDuty.execute(AppCommand.OpenWelcomeTab) },
                        RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_editor_session_page.text, AppCommand.OpenEditorSessionTab), appDuty.commandDuty.canExecute(AppCommand.OpenEditorSessionTab)) { appDuty.commandDuty.execute(AppCommand.OpenEditorSessionTab) },
                        RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_settings_page.text, AppCommand.OpenSettingsTab), appDuty.commandDuty.canExecute(AppCommand.OpenSettingsTab)) { appDuty.commandDuty.execute(AppCommand.OpenSettingsTab) }
                    )
                ),
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_restore_closed_tab.text, AppCommand.RestoreClosedTab), appDuty.commandDuty.canExecute(AppCommand.RestoreClosedTab)) { appDuty.commandDuty.execute(AppCommand.RestoreClosedTab) },
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_close_tab.text, AppCommand.CloseCurrentTab), appDuty.commandDuty.canExecute(AppCommand.CloseCurrentTab)) { appDuty.commandDuty.execute(AppCommand.CloseCurrentTab) },
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_exit.text, AppCommand.RequestWindowClose), appDuty.commandDuty.canExecute(AppCommand.RequestWindowClose)) { appDuty.commandDuty.execute(AppCommand.RequestWindowClose) }
            )
        ),
        MenuGroup(
            id = "edit",
            label = Res.string.menu_group_edit.text,
            entries = listOf(
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_undo.text, AppCommand.Undo), appDuty.commandDuty.canExecute(AppCommand.Undo)) { appDuty.commandDuty.execute(AppCommand.Undo) },
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_redo.text, AppCommand.Redo), appDuty.commandDuty.canExecute(AppCommand.Redo)) { appDuty.commandDuty.execute(AppCommand.Redo) },
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_save.text, AppCommand.Save), appDuty.commandDuty.canExecute(AppCommand.Save)) { appDuty.commandDuty.execute(AppCommand.Save) },
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_discard_changes.text, AppCommand.Discard), appDuty.commandDuty.canExecute(AppCommand.Discard)) { appDuty.commandDuty.execute(AppCommand.Discard) }
            )
        ),
        MenuGroup(
            id = "view",
            label = Res.string.menu_group_view.text,
            entries = listOf(
                RyoMenuEntry.MenuItem(
                    menuLabel(if (appDuty.sideWorkspaceDuty.expanded) Res.string.menu_item_collapse_sidebar.text else Res.string.menu_item_expand_sidebar.text, AppCommand.ToggleSidePanel),
                    appDuty.commandDuty.canExecute(AppCommand.ToggleSidePanel)
                ) { appDuty.commandDuty.execute(AppCommand.ToggleSidePanel) },
                RyoMenuEntry.MenuItem(
                    Res.string.menu_item_switch_panel.text,
                    children = listOf(
                        RyoMenuEntry.MenuItem(Res.string.panel_assets_manager.text, appDuty.commandDuty.canExecute(AppCommand.FocusAssetsPanel)) { appDuty.commandDuty.execute(AppCommand.FocusAssetsPanel) },
                        RyoMenuEntry.MenuItem(Res.string.panel_schemas_manager.text, appDuty.commandDuty.canExecute(AppCommand.FocusSchemasPanel)) { appDuty.commandDuty.execute(AppCommand.FocusSchemasPanel) }
                    )
                ),
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(
                    menuLabel(if (appDuty.isMaximized) Res.string.menu_item_exit_fullscreen.text else Res.string.menu_item_fullscreen.text, AppCommand.ToggleMaximizeWindow),
                    appDuty.commandDuty.canExecute(AppCommand.ToggleMaximizeWindow)
                ) { appDuty.commandDuty.execute(AppCommand.ToggleMaximizeWindow) }
            )
        ),
        MenuGroup(
            id = "help",
            label = Res.string.menu_group_help.text,
            entries = listOf(
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_welcome_page.text, AppCommand.OpenWelcomeTab), appDuty.commandDuty.canExecute(AppCommand.OpenWelcomeTab)) { appDuty.commandDuty.execute(AppCommand.OpenWelcomeTab) },
                RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_settings.text, AppCommand.OpenSettingsTab), appDuty.commandDuty.canExecute(AppCommand.OpenSettingsTab)) { appDuty.commandDuty.execute(AppCommand.OpenSettingsTab) },
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(Res.string.menu_item_about.text, onClick = appDuty::showAboutDialog)
            )
        )
    )

    val juche: @Composable () -> Unit = {
        val anchors = remember { mutableStateMapOf<String, IntOffset>() }

        Row(Modifier.fillMaxWidth().height(56.dp)) {
            Box(Modifier.fillMaxHeight().width(64.dp), Alignment.Center) { Icon(Res.drawable.ic_ryo_24px.vector, BuildConfig.APP_NAME, Modifier.size(24.dp), MaterialTheme.colorScheme.onSurfaceVariant) }

            Row(Modifier.fillMaxSize().padding(horizontal = 4.dp), verticalAlignment = Alignment.CenterVertically) {
                // 菜单栏
                Box(Modifier.weight(1F)) {
                    Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(4.dp), verticalAlignment = Alignment.CenterVertically) {
                        menuGroups.forEach { group ->
                            RyoButton(group.label, Modifier.height(36.dp).onGloballyPositioned { coords ->
                                val b = coords.boundsInWindow()
                                anchors[group.id] = IntOffset(b.left.roundToInt(), b.bottom.roundToInt())
                            }.onPointerEvent(PointerEventType.Enter) {
                                anchors[group.id]?.also { appDuty.hoverMenuGroup(group.id, it.x, it.y, group.entries) }
                            }) { anchors[group.id]?.also { appDuty.toggleMenuGroup(group.id, it.x, it.y, group.entries) } }
                        }
                    }
                }

                // 三键（靠注入）
                windowControlButtons()
            }
        }
    }

    if (appDuty.isMaximized) juche() else WindowDraggableArea(content = juche)
}
