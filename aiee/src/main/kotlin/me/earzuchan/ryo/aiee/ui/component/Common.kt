package me.earzuchan.ryo.aiee.ui.component

import androidx.compose.animation.Crossfade
import androidx.compose.animation.animateColorAsState
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.animateDpAsState
import androidx.compose.animation.core.tween
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.hoverable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsHoveredAsState
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.foundation.text.BasicTextField
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.focus.onFocusChanged
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.key.*
import androidx.compose.ui.input.pointer.PointerEventType
import androidx.compose.ui.input.pointer.PointerIcon
import androidx.compose.ui.input.pointer.onPointerEvent
import androidx.compose.ui.input.pointer.pointerHoverIcon
import androidx.compose.ui.platform.LocalFocusManager
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.style.TextDecoration
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.ic_close_24px
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.resolve
import me.earzuchan.ryo.aiee.util.PlatformUtils
import me.earzuchan.ryo.aiee.util.UiUtils.only

@Composable
fun HyperlinkText(str: String, linkStr: String, style: TextStyle = MaterialTheme.typography.bodyMedium, color: Color = MaterialTheme.colorScheme.primary) { // 1. 创建交互源，用来监听“悬停”状态
    val interactionSource = remember { MutableInteractionSource() }
    val isHovered by interactionSource.collectIsHoveredAsState()

    Text(
        str, Modifier.hoverable(interactionSource).pointerHoverIcon(PointerIcon.Hand).clickable(remember { MutableInteractionSource() }, null) {
        PlatformUtils.openLink(linkStr)
    }, style = style.copy(color, textDecoration = if (isHovered) TextDecoration.Underline else TextDecoration.None)
    )
}

@OptIn(ExperimentalComposeUiApi::class)
@Composable
fun EditableLabel(label: UiText, editText: String, onEditTextChange: (String) -> Unit, elegant: Boolean = false, modifier: Modifier = Modifier, editable: Boolean = true) {
    val hoverSource = remember { MutableInteractionSource() }
    val focusManager = LocalFocusManager.current
    val inputFocusRequester = remember { FocusRequester() }
    val isHovered by hoverSource.collectIsHoveredAsState()

    var isEditing by remember { mutableStateOf(false) }
    var ignoreInitialFocus by remember { mutableStateOf(false) }
    var isClearing by remember { mutableStateOf(false) }

    val iconButtonSize = if (elegant) 28 else 24
    val holderHeight = if (elegant) 36.dp else 24.dp
    val holderPadding = if (elegant) PaddingValues(horizontal = 16.dp, vertical = 8.dp) else PaddingValues(horizontal = 2.dp)

    val showHover = editable && !isEditing && isHovered

    val targetRadius = if (isEditing || showHover) (if (elegant) 18.dp else 4.dp) else 0.dp
    val animatedRadius by animateDpAsState(targetRadius, tween(200, easing = FastOutSlowInEasing), "RadiusTransition")
    val shape = RoundedCornerShape(animatedRadius)

    val targetColor = when {
        isEditing -> MaterialTheme.colorScheme.onPrimaryContainer.copy(0.12F)
        showHover -> MaterialTheme.colorScheme.onPrimaryContainer.copy(0.08F)
        else -> Color.Transparent
    }

    val animatedColor by animateColorAsState(targetColor, tween(200, easing = FastOutSlowInEasing), "ColorTransition")

    LaunchedEffect(editable) { if (!editable) isEditing = false }

    LaunchedEffect(isEditing) { if (isEditing) inputFocusRequester.requestFocus() }

    Row(modifier = modifier.fillMaxWidth().height(holderHeight).background(animatedColor, shape).only(editable) {
        hoverable(hoverSource).only(!isEditing) { pointerHoverIcon(PointerIcon.Hand) }.clickable(
            remember { MutableInteractionSource() }, null, !isEditing // 保留原来的不显示涟漪设定
        ) {
            ignoreInitialFocus = true // 标记忽略接下来的首次焦点事件
            isEditing = true
        }
    }.padding(holderPadding), verticalAlignment = Alignment.CenterVertically) {
        Crossfade(isEditing && editable, Modifier.fillMaxWidth(), tween(200, easing = FastOutSlowInEasing), "ContentCrossfade") { editing ->
            if (editing) Row(Modifier.fillMaxWidth(), horizontalArrangement = Arrangement.spacedBy(2.dp), verticalAlignment = Alignment.CenterVertically) {
                BasicTextField(
                    editText, onEditTextChange, Modifier.weight(1F).focusRequester(inputFocusRequester).onPreviewKeyEvent {
                        if (it.type == KeyEventType.KeyDown && it.key == Key.Enter) {
                            focusManager.clearFocus(force = true)
                            true
                        } else false
                    }.onFocusChanged { state ->
                        if (state.isFocused) ignoreInitialFocus = false else if (!ignoreInitialFocus && !isClearing) isEditing = false
                    }, singleLine = true, textStyle = MaterialTheme.typography.labelLarge.copy(color = MaterialTheme.colorScheme.onSurfaceVariant)
                )
                Box(Modifier.onPointerEvent(PointerEventType.Press) { isClearing = true }.onPointerEvent(PointerEventType.Release) { isClearing = false }.onPointerEvent(PointerEventType.Exit) { isClearing = false }) {
                    RyoIconButton(Res.drawable.ic_close_24px, iconButtonSize) {
                        onEditTextChange("")
                        isClearing = false
                        inputFocusRequester.requestFocus()
                    }
                }
            } else Text(label.resolve(), Modifier.widthIn(min = 0.dp), MaterialTheme.colorScheme.onSurfaceVariant, maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.labelLarge)
        }
    }
}