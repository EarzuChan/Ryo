package me.earzuchan.ryo.aiee.ui.component

import androidx.compose.animation.*
import androidx.compose.animation.core.MutableTransitionState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.*
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.TransformOrigin
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.onPointerEvent
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalWindowInfo
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.*
import androidx.compose.ui.window.Popup
import androidx.compose.ui.window.PopupPositionProvider
import androidx.compose.ui.window.PopupProperties
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.util.UiUtils.text
import me.earzuchan.ryo.aiee.util.UiUtils.vector

// TIPS：自研黄金级Menu，适用于弹出菜单、右键菜单、选择器菜单

@Composable
@OptIn(ExperimentalComposeUiApi::class)
fun RyoMenu(expanded: Boolean, anchorX: Int?, anchorY: Int?, entries: List<RyoMenuEntry>, onDismissRequest: () -> Unit, selectedIndex: Int? = null, panelWidth: Dp = 200.dp, submenuOverlap: Dp = 8.dp, textStyle: TextStyle? = null) {
    val visibility = remember { MutableTransitionState(false) }
    var cachedPos by remember { mutableStateOf<IntOffset?>(null) }
    val edgePaddingPx = with(LocalDensity.current) { 8.dp.roundToPx() } // 窗口边缘安全距离

    LaunchedEffect(anchorX, anchorY) { if (anchorX != null && anchorY != null) cachedPos = IntOffset(anchorX, anchorY) }
    LaunchedEffect(expanded) { visibility.targetState = expanded }

    val pos = (if (expanded) anchorX?.let { IntOffset(it, anchorY!!) } else cachedPos) ?: return
    if (!visibility.currentState && !visibility.targetState) return

    Popup(
        remember(pos) {
            object : PopupPositionProvider {
                override fun calculatePosition(anchorBounds: IntRect, windowSize: IntSize, layoutDirection: LayoutDirection, popupContentSize: IntSize): IntOffset { // 根菜单防溢出，保留边缘8dp
                    val fitX = pos.x.coerceIn(edgePaddingPx, (windowSize.width - popupContentSize.width - edgePaddingPx).coerceAtLeast(edgePaddingPx))
                    val fitY = pos.y.coerceIn(edgePaddingPx, (windowSize.height - popupContentSize.height - edgePaddingPx).coerceAtLeast(edgePaddingPx))
                    return IntOffset(fitX, fitY)
                }
            }
        }, onDismissRequest, PopupProperties(false, dismissOnClickOutside = true, clippingEnabled = false) // not focusable 是灵魂！允许菜单下控件持续响应 Hover，实现 Mac 级速切手感，Vamos
    ) {
        AnimatedVisibility(
            visibleState = visibility, enter = fadeIn(tween(120)) + scaleIn(tween(120), 0.98F, TransformOrigin(0F, 0F)), exit = fadeOut(tween(90)) + scaleOut(tween(90), 0.98F, TransformOrigin(0F, 0F))
        ) { RyoMenuPanel(entries, panelWidth, submenuOverlap, textStyle, onDismissRequest, selectedIndex = selectedIndex) }
    }
}

@Composable
private fun SubmenuPopup(visible: Boolean, entries: List<RyoMenuEntry>, panelWidth: Dp, overlapPx: Int, panelPaddingPx: Int, edgePaddingPx: Int, textStyle: TextStyle?, onDismissRequest: () -> Unit, idPrefix: String) {
    val visibility = remember { MutableTransitionState(false) }
    visibility.targetState = visible

    if (visibility.currentState || visibility.targetState) Popup(
        remember(overlapPx, panelPaddingPx, edgePaddingPx) {
            object : PopupPositionProvider {
                override fun calculatePosition(anchorBounds: IntRect, windowSize: IntSize, layoutDirection: LayoutDirection, popupContentSize: IntSize): IntOffset { // X轴：带边距的安全碰撞检测与翻转
                    var x = anchorBounds.right - overlapPx
                    if (x + popupContentSize.width > windowSize.width - edgePaddingPx) x = anchorBounds.left - popupContentSize.width + overlapPx
                    x = x.coerceIn(edgePaddingPx, (windowSize.width - popupContentSize.width - edgePaddingPx).coerceAtLeast(edgePaddingPx))

                    // Y轴：精确定位！向上偏移 panelPaddingPx(8dp)，让子菜单的第一个条目和母条目绝对水平对齐
                    var y = anchorBounds.top - panelPaddingPx

                    // Y轴底部超出屏幕判断，如果超了，往上顶（但也要留出 8dp 边距）
                    if (y + popupContentSize.height > windowSize.height - edgePaddingPx) y = windowSize.height - popupContentSize.height - edgePaddingPx // Y轴顶部兜底（万一菜单巨长，连顶部都超了，保证至少离顶边有距离）
                    y = y.coerceAtLeast(edgePaddingPx)

                    return IntOffset(x, y)
                }
            }
        }, properties = PopupProperties(false)
    ) {
        AnimatedVisibility(
            visibility, enter = fadeIn(tween(120)) + scaleIn(tween(120), 0.98F, TransformOrigin(0F, 0F)), exit = fadeOut(tween(90)) + scaleOut(tween(90), 0.98F, TransformOrigin(0F, 0F))
        ) { RyoMenuPanel(entries, panelWidth, with(LocalDensity.current) { overlapPx.toDp() }, textStyle, onDismissRequest, idPrefix) }
    }
}

@OptIn(ExperimentalComposeUiApi::class)
@Composable
private fun RyoMenuPanel(entries: List<RyoMenuEntry>, panelWidth: Dp, submenuOverlap: Dp, textStyle: TextStyle?, onDismissRequest: () -> Unit, idPrefix: String = "root", selectedIndex: Int? = null) {
    var activeSubmenuComposeId by remember { mutableStateOf<String?>(null) }
    val dummyInteraction = remember { MutableInteractionSource() }
    val density = LocalDensity.current
    val overlapPx = with(density) { submenuOverlap.roundToPx() }
    val panelPaddingPx = with(density) { 8.dp.roundToPx() } // 与下方的 padding(vert = 8) 对应
    val edgePaddingPx = panelPaddingPx

    val windowHeightPx = LocalWindowInfo.current.containerSize.height
    val maxHeight = with(density) { windowHeightPx.toDp() } - 16.dp // 留足上下滚动边距

    Surface(Modifier.width(panelWidth).heightIn(max = maxHeight), RoundedCornerShape(4.dp), MaterialTheme.colorScheme.surfaceContainer, shadowElevation = 2.dp) {
        Column(Modifier.verticalScroll(rememberScrollState()).padding(vertical = 8.dp)) {
            entries.forEachIndexed { index, entry ->
                val composeId = "$idPrefix.$index"

                key(composeId) {
                    when (entry) {
                        is RyoMenuEntry.Divider -> HorizontalDivider(Modifier.padding(vertical = 8.dp), color = MaterialTheme.colorScheme.outlineVariant)
                        is RyoMenuEntry.MenuItem -> {
                            val isHovered = activeSubmenuComposeId == composeId
                            val isSelected = idPrefix == "root" && selectedIndex == index
                            val selectedFg = MaterialTheme.colorScheme.onSecondaryContainer
                            val selectedBg = MaterialTheme.colorScheme.secondaryContainer
                            val fg = when {
                                !entry.enabled && isSelected -> selectedFg.copy(0.38F)
                                isSelected -> selectedFg
                                entry.enabled -> MaterialTheme.colorScheme.onSurface
                                else -> MaterialTheme.colorScheme.onSurface.copy(0.38F)
                            }
                            val bg = when {
                                isSelected -> selectedBg
                                isHovered && entry.enabled -> MaterialTheme.colorScheme.onSurface.copy(0.08F)
                                else -> Color.Transparent
                            }
                            val sideFg = when {
                                !entry.enabled && isSelected -> selectedFg.copy(0.5F)
                                isSelected -> selectedFg
                                entry.enabled -> MaterialTheme.colorScheme.onSurfaceVariant
                                else -> MaterialTheme.colorScheme.onSurfaceVariant.copy(0.38F)
                            }
                            val hasSubmenu = entry.children.isNotEmpty()
                            val mainText = entry.text.substringBefore('\t')
                            val shortcutText = if (hasSubmenu) null else entry.text.substringAfter('\t', "").takeIf { it.isNotBlank() }
                            val endPadding = if (hasSubmenu) 2.dp else 8.dp

                            Box(Modifier.fillMaxWidth()) {
                                Row(
                                    Modifier.fillMaxWidth().background(bg).onPointerEvent(PointerEventType.Enter) { if (entry.enabled) activeSubmenuComposeId = composeId }.clickable(dummyInteraction, null, entry.enabled) {
                                        if (!hasSubmenu) {
                                            entry.onClick?.invoke()
                                            onDismissRequest()
                                        } else activeSubmenuComposeId = composeId
                                    }.padding(start = 8.dp, end = endPadding).height(28.dp), horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically
                                ) {
                                    Text(mainText, Modifier.weight(1F), maxLines = 1, overflow = TextOverflow.Ellipsis, style = textStyle ?: MaterialTheme.typography.bodyMedium, color = fg)
                                    if (hasSubmenu) Icon(Res.drawable.ic_arror_right_24px.vector, Res.string.menu_submenu_cd.text, Modifier.size(24.dp), sideFg)
                                    else if (shortcutText != null) Text(shortcutText, maxLines = 1, style = MaterialTheme.typography.bodySmall, color = sideFg)
                                }

                                if (hasSubmenu) SubmenuPopup(isHovered, entry.children, panelWidth, overlapPx, panelPaddingPx, edgePaddingPx, textStyle, onDismissRequest, composeId)
                            }
                        }
                    }
                }
            }
        }
    }
}

sealed interface RyoMenuEntry {
    data class MenuItem(val text: String, val enabled: Boolean = true, val children: List<RyoMenuEntry> = emptyList(), val onClick: (() -> Unit)? = null) : RyoMenuEntry

    object Divider : RyoMenuEntry
}
