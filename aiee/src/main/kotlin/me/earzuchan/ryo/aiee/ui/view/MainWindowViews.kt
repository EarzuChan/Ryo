package me.earzuchan.ryo.aiee.ui.view

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.gestures.detectDragGestures
import androidx.compose.foundation.gestures.detectTapGestures
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
import androidx.compose.ui.text.font.FontStyle
import androidx.compose.ui.unit.IntOffset
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.FrameWindowScope
import com.arkivanov.decompose.extensions.compose.subscribeAsState
import com.arkivanov.decompose.extensions.compose.stack.Children
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.app.CommandService
import me.earzuchan.ryo.aiee.app.MenuService
import me.earzuchan.ryo.aiee.app.ShortcutService
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.duty.AssetsPanelDuty
import me.earzuchan.ryo.aiee.duty.MainPanelDuty
import me.earzuchan.ryo.aiee.duty.SidePanelDuty
import me.earzuchan.ryo.aiee.duty.SchemasPanelDuty
import me.earzuchan.ryo.aiee.duty.MainPanelTabNavis
import me.earzuchan.ryo.aiee.ui.panel.AssetsPanel
import me.earzuchan.ryo.aiee.ui.panel.SchemasPanel
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.RyoIconButton
import me.earzuchan.ryo.aiee.ui.component.RyoButton
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry
import me.earzuchan.ryo.aiee.ui.resolve
import me.earzuchan.ryo.aiee.util.UiUtils.text
import me.earzuchan.ryo.aiee.util.UiUtils.vector
import org.jetbrains.compose.resources.DrawableResource
import org.koin.compose.koinInject
import sh.calvin.reorderable.ReorderableItem
import sh.calvin.reorderable.rememberReorderableLazyListState
import java.awt.Cursor
import kotlin.math.abs
import kotlin.math.roundToInt

@Composable
fun SidePanelView(duty: SidePanelDuty) {
    @Composable
    fun PanelButton(icon: DrawableResource, hint: String, selected: Boolean, onClick: () -> Unit) = RyoIconButton(icon, 48, hint, if (selected) IconButtonDefaults.filledTonalIconButtonColors() else IconButtonDefaults.iconButtonColors(), onClick)

    @Composable
    fun ActionButton(icon: DrawableResource, hint: String, colors: IconButtonColors = IconButtonDefaults.iconButtonColors(), onClick: () -> Unit) = RyoIconButton(icon = icon, dpSize = 48, hintText = hint, colors = colors, onClick = onClick)

    val density = LocalDensity.current
    val panelStack by duty.panelStack.subscribeAsState()
    val activePanelId = panelStack.active.configuration.id

    Row(Modifier.fillMaxHeight()) {
        Column(Modifier.fillMaxHeight().width(64.dp).padding(vertical = 8.dp), horizontalAlignment = Alignment.CenterHorizontally) {
            Column(Modifier.weight(1F), Arrangement.spacedBy(4.dp)) {
                duty.panels.forEach { PanelButton(if (activePanelId == it.id) it.selectedIcon else it.icon, it.title.text, activePanelId == it.id) { duty.focus(it.id) } }
            }

            Column(verticalArrangement = Arrangement.spacedBy(4.dp)) {
                ActionButton(if (duty.expanded) Res.drawable.ic_panel_narrow_24px else Res.drawable.ic_panel_24px, Res.string.side_toggle_panel.text, onClick = duty::toggle)
                ActionButton(Res.drawable.ic_settings_24px, Res.string.settings.text, onClick = duty::mentionSettings)
            }
        }

        if (!duty.expanded) return

        Box(Modifier.fillMaxHeight().width(duty.panelWidthDp.dp).clip(RoundedCornerShape(topStart = 16.dp, topEnd = 16.dp)).background(MaterialTheme.colorScheme.surfaceContainerHigh)) {
            Children(duty.panelStack) { child ->
                Column(Modifier.fillMaxSize().padding(12.dp, 12.dp, 12.dp)) {
                    when (val panel = child.instance) {
                        is AssetsPanelDuty -> AssetsPanel(panel)
                        is SchemasPanelDuty -> SchemasPanel(panel)
                    }
                }
            }
        }

        Spacer(Modifier.fillMaxHeight().width(8.dp).padding(end = 4.dp).pointerHoverIcon(PointerIcon(Cursor(Cursor.E_RESIZE_CURSOR))).pointerInput(Unit) {
            detectDragGestures { change, dragAmount ->
                change.consume()
                duty.resizeBy(with(density) { dragAmount.x.toDp().value })
            }
        })
    }
}

@Composable
@OptIn(ExperimentalComposeUiApi::class)
fun MainPanelView(duty: MainPanelDuty) = Column(Modifier.fillMaxSize().clip(RoundedCornerShape(topStart = 16.dp)).background(MaterialTheme.colorScheme.surfaceContainer)) {
    val menuService = koinInject<MenuService>()

    val stackState by duty.tabStack.subscribeAsState()
    val activeTabId = stackState.active.configuration.takeIf { it !is MainPanelTabNavis.Empty }?.id
    val tabModels  = duty.getUiModels(stackState.items)

    @Composable
    fun TabChip(tab: MainPanelDuty.TabData, selected: Boolean, onSelect: () -> Unit, onDoubleClick: () -> Unit, onClose: () -> Unit, onContextMenu: (anchorX: Int, anchorY: Int) -> Unit, modifier: Modifier) {
        val textColor = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurfaceVariant
        val indicatorColor = if (selected) MaterialTheme.colorScheme.primary else Color.Transparent
        var topLeftInWindow by remember(tab.id) { mutableStateOf(Offset.Zero) }

        @Composable
        @OptIn(ExperimentalComposeUiApi::class)
        fun TabTrailingAction(tab: MainPanelDuty.TabData, selected: Boolean, onClose: () -> Unit) {
            var hovered by remember(tab.id) { mutableStateOf(false) }
            val tint = if (selected) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.onSurfaceVariant
            val showClose = !tab.duty.isDirty.value || hovered

            Box(Modifier.size(24.dp).onPointerEvent(PointerEventType.Enter) { hovered = true }.onPointerEvent(PointerEventType.Exit) { hovered = false }.clip(CircleShape).clickable(onClick = onClose), Alignment.Center) {
                Icon((if (showClose) Res.drawable.ic_tab_close_24px else Res.drawable.ic_tab_unsaved_24px).vector, Res.string.tab_action_cd.text, Modifier.size(24.dp), tint)
            }
        }

        Column(modifier.height(35.dp).width(IntrinsicSize.Max).onGloballyPositioned { topLeftInWindow = it.positionInWindow() }.onPointerEvent(PointerEventType.Press) { event ->
            if (!event.buttons.isSecondaryPressed) return@onPointerEvent
            val localPress = event.changes.firstOrNull()?.position ?: return@onPointerEvent
            onContextMenu((topLeftInWindow.x + localPress.x).roundToInt(), (topLeftInWindow.y + localPress.y).roundToInt())
        }.pointerInput(tab.id) { detectTapGestures({ onDoubleClick() }, onTap = { onSelect() }) }) {
            Spacer(Modifier.fillMaxWidth().height(3.dp))
            Row(Modifier.fillMaxWidth().weight(1F).padding(start = 12.dp, end = 6.dp), Arrangement.spacedBy(6.dp), Alignment.CenterVertically) {
                Text(tab.duty.title.value.resolve(), Modifier, textColor, maxLines = 1, softWrap = false, style = MaterialTheme.typography.labelLarge, fontStyle = if(tab.isResidential) FontStyle.Normal else FontStyle.Italic)
                TabTrailingAction(tab, selected, onClose)
            }
            Box(Modifier.fillMaxWidth().height(2.dp).background(indicatorColor))
        }
    }

    // 以下为Tab Chips
    if (tabModels.isNotEmpty()) Column(Modifier.fillMaxWidth()) {
        val lazyListState = rememberLazyListState()
        val scope = rememberCoroutineScope()
        val reorderState = rememberReorderableLazyListState(lazyListState) { from, to -> duty.moveTab(from.index, to.index) }

        LazyRow(
            Modifier.fillMaxWidth().onPointerEvent(PointerEventType.Scroll) { event ->
                val delta = event.changes.firstOrNull()?.scrollDelta ?: return@onPointerEvent
                val direction = if (abs(delta.x) > abs(delta.y)) delta.x else delta.y
                if (direction == 0F) return@onPointerEvent
                scope.launch { lazyListState.scrollBy(direction * 50F) }
            }, lazyListState
        ) {
            items(tabModels, key = { it.id }) { tab ->
                val hasOtherTabs = tabModels.size > 1
                val hasTabs = tabModels.isNotEmpty()
                val tabTitle = tab.duty.title.value.resolve()
                val closeText = Res.string.tab_context_close_format.text(tabTitle)
                val closeOthersText = Res.string.tab_context_close_others.text
                val closeAllText = Res.string.tab_context_close_all.text

                ReorderableItem(reorderState, key = tab.id) {
                    TabChip(tab, activeTabId == tab.id, { duty.focus(tab.id) }, { duty.makeResidential(tab.id) },{ duty.close(tab.id) }, { anchorX, anchorY ->
                        menuService.showContextMenu(
                            anchorX, anchorY, listOf(
                                RyoMenuEntry.MenuItem(closeText) { duty.close(tab.id) },
                                RyoMenuEntry.MenuItem(closeOthersText, hasOtherTabs) { duty.closeOthers(tab.id) },
                                RyoMenuEntry.MenuItem(closeAllText, hasTabs, onClick = duty::closeAll),
                                // TODO：关左关右
                            )
                        )
                    }, Modifier.draggableHandle())
                }
            }
        }

        HorizontalDivider(color = MaterialTheme.colorScheme.outlineVariant, thickness = 1.dp)
    }

    Box(Modifier.fillMaxSize()) { Children(duty.tabStack) { it.instance.Render() } }
}

@Composable
@OptIn(ExperimentalComposeUiApi::class)
fun FrameWindowScope.AppTopBarView(appDuty: AppDuty, windowControlButtons: @Composable () -> Unit) {
    val commandService = koinInject<CommandService>()
    val shortcutService = koinInject<ShortcutService>()
    val menuService = koinInject<MenuService>()

    data class MenuGroup(val id: String, val label: String, val entries: List<RyoMenuEntry>)

    val activeVolumeState by appDuty.activeVolumeState.collectAsState()
    val hasActiveVolume = activeVolumeState != null
    val activeVolumeName = activeVolumeState?.displayName
    val saveActiveVolumeText = activeVolumeName?.let { Res.string.menu_item_save_volume_format.text(it) } ?: Res.string.menu_item_save_active_volume.text
    val saveActiveVolumeAsText = activeVolumeName?.let { Res.string.menu_item_save_volume_as_format.text(it) } ?: Res.string.menu_item_save_active_volume_as.text
    val closeActiveVolumeText = activeVolumeName?.let { Res.string.menu_item_close_volume_format.text(it) } ?: Res.string.menu_item_close_active_volume.text

    val menuLabel: (String, String) -> String = { title, command -> shortcutService.getEffective(command)?.displayText()?.let { "$title\t$it" } ?: title }

    val menuGroups = listOf(
        MenuGroup(
            "file", Res.string.menu_group_file.text, listOf(
                AppDuty.CMD_OPEN_VOLUME.let { RyoMenuEntry.MenuItem(menuLabel(Res.string.menu_item_open_volume.text, it), commandService.canExecute(it)) { commandService.dispatch(it) } },
                AppDuty.CMD_SAVE_ACTIVE_VOLUME.let { RyoMenuEntry.MenuItem(menuLabel(saveActiveVolumeText, it), hasActiveVolume) { commandService.dispatch(it) } },
                AppDuty.CMD_SAVE_ACTIVE_VOLUME_AS.let { RyoMenuEntry.MenuItem(menuLabel(saveActiveVolumeAsText, it), hasActiveVolume) { commandService.dispatch(it) } },
                AppDuty.CMD_CLOSE_ACTIVE_VOLUME.let { RyoMenuEntry.MenuItem(menuLabel(closeActiveVolumeText, it), hasActiveVolume) { commandService.dispatch(it) } },
                RyoMenuEntry.Divider,
                /*RyoMenuEntry.MenuItem(
                    text = Res.string.menu_item_new.text, children = listOf(
                        RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.welcome.text, OpenWelcomeTabOldCommand), oldAppDuty.canExecuteCommand(OpenWelcomeTabOldCommand)) { oldAppDuty.executeCommand(OpenWelcomeTabOldCommand) },
                        RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_editor_session_page.text, OpenEditorSessionTabOldCommand), oldAppDuty.canExecuteCommand(OpenEditorSessionTabOldCommand)) { oldAppDuty.executeCommand(OpenEditorSessionTabOldCommand) },
                        RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.settings.text, OpenSettingsTabOldCommand), oldAppDuty.canExecuteCommand(OpenSettingsTabOldCommand)) { oldAppDuty.executeCommand(OpenSettingsTabOldCommand) })
                ),
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_restore_closed_tab.text, RestoreClosedTabOldCommand), oldAppDuty.canExecuteCommand(RestoreClosedTabOldCommand)) { oldAppDuty.executeCommand(RestoreClosedTabOldCommand) },
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_close_tab.text, CloseCurrentTabOldCommand), oldAppDuty.canExecuteCommand(CloseCurrentTabOldCommand)) { oldAppDuty.executeCommand(CloseCurrentTabOldCommand) },
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_exit.text, RequestWindowCloseOldCommand), oldAppDuty.canExecuteCommand(RequestWindowCloseOldCommand)) { oldAppDuty.executeCommand(RequestWindowCloseOldCommand) })*/
        ))/*, MenuGroup(
            "edit", Res.string.menu_group_edit.text, listOf(
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_undo.text, UndoOldCommand), oldAppDuty.canExecuteCommand(UndoOldCommand)) { oldAppDuty.executeCommand(UndoOldCommand) },
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_redo.text, RedoOldCommand), oldAppDuty.canExecuteCommand(RedoOldCommand)) { oldAppDuty.executeCommand(RedoOldCommand) },
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_save.text, SaveOldCommand), oldAppDuty.canExecuteCommand(SaveOldCommand)) { oldAppDuty.executeCommand(SaveOldCommand) },
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.menu_item_discard_changes.text, DiscardOldCommand), oldAppDuty.canExecuteCommand(DiscardOldCommand)) { oldAppDuty.executeCommand(DiscardOldCommand) })
        ), MenuGroup(
            "view", Res.string.menu_group_view.text, listOf(
                "toggle_side_panel".let {
                    RyoMenuEntry.MenuItem(
                        menuLabel(if (appDuty.sidePanelDuty.expanded) Res.string.menu_item_collapse_sidebar.text else Res.string.menu_item_expand_sidebar.text, it), commandService.canExecute(it)
                    ) { commandService.executeAsync(it) }
                }, RyoMenuEntry.MenuItem(
                    Res.string.menu_item_switch_panel.text,
                    children = listOf(
                        RyoMenuEntry.MenuItem(Res.string.panel_assets_manager.text, oldAppDuty.canExecuteCommand(FocusAssetsPanelOldCommand)) { oldAppDuty.executeCommand(FocusAssetsPanelOldCommand) },
                        RyoMenuEntry.MenuItem(Res.string.panel_schemas_manager.text, oldAppDuty.canExecuteCommand(FocusSchemasPanelOldCommand)) { oldAppDuty.executeCommand(FocusSchemasPanelOldCommand) })
                ), RyoMenuEntry.Divider, RyoMenuEntry.MenuItem(
                    oldMenuLabel(if (oldAppDuty.isMaximized) Res.string.menu_item_exit_fullscreen.text else Res.string.menu_item_fullscreen.text, ToggleMaximizeWindowOldCommand), oldAppDuty.canExecuteCommand(ToggleMaximizeWindowOldCommand)
                ) { oldAppDuty.executeCommand(ToggleMaximizeWindowOldCommand) })
        ), MenuGroup(
            "help", Res.string.menu_group_help.text, listOf(
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.welcome.text, OpenWelcomeTabOldCommand), oldAppDuty.canExecuteCommand(OpenWelcomeTabOldCommand)) { oldAppDuty.executeCommand(OpenWelcomeTabOldCommand) },
                RyoMenuEntry.MenuItem(oldMenuLabel(Res.string.settings.text, OpenSettingsTabOldCommand), oldAppDuty.canExecuteCommand(OpenSettingsTabOldCommand)) { oldAppDuty.executeCommand(OpenSettingsTabOldCommand) },
                RyoMenuEntry.Divider,
                RyoMenuEntry.MenuItem(Res.string.menu_item_about.text, onClick = oldAppDuty.dialogService::orderAbout)
            )
        )*/
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
                                anchors[group.id]?.also { menuService.hoverMenuBarGroup(group.id, it.x, it.y, group.entries) }
                            }) { anchors[group.id]?.also { menuService.toggleMenuBarGroup(group.id, it.x, it.y, group.entries) } }
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
