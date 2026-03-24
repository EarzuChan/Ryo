package me.earzuchan.ryo.aiee.ui.component

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
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.*
import androidx.compose.ui.Alignment
import androidx.compose.ui.ExperimentalComposeUiApi
import androidx.compose.ui.Modifier
import androidx.compose.ui.focus.FocusRequester
import androidx.compose.ui.focus.focusRequester
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.graphics.SolidColor
import androidx.compose.ui.input.key.*
import androidx.compose.ui.input.pointer.*
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
import me.earzuchan.ryo.aiee.util.UiUtils.vector
import org.jetbrains.compose.resources.DrawableResource
import java.awt.AWTEvent
import java.awt.Toolkit
import java.awt.event.AWTEventListener
import java.awt.event.MouseEvent
import java.awt.event.WindowEvent
import javax.swing.SwingUtilities
import kotlin.concurrent.atomics.AtomicBoolean
import kotlin.concurrent.atomics.ExperimentalAtomicApi

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

@OptIn(ExperimentalComposeUiApi::class, ExperimentalAtomicApi::class)
@Composable
fun EditableLabel(label: UiText, editText: String, onEditTextChange: (String) -> Unit, elegant: Boolean = true, modifier: Modifier = Modifier, editable: Boolean = true) {
    val hoverSource = remember { MutableInteractionSource() }
    val noRippleSource = remember { MutableInteractionSource() }
    val isHovered by hoverSource.collectIsHoveredAsState()

    val inputFocusRequester = remember { FocusRequester() }
    var isEditing by remember { mutableStateOf(false) }

    val showHover = editable && !isEditing && isHovered
    val targetRadius = (if (isEditing || showHover) if (elegant) 18 else 4 else 0).dp
    val animatedRadius by animateDpAsState(targetRadius, tween(300, easing = FastOutSlowInEasing))

    val targetColor = when {
        isEditing -> MaterialTheme.colorScheme.onPrimaryContainer.copy(0.12F)
        showHover -> MaterialTheme.colorScheme.onPrimaryContainer.copy(0.08F)
        else -> Color.Transparent
    }
    val animatedColor by animateColorAsState(targetColor, tween(300, easing = FastOutSlowInEasing))

    LaunchedEffect(editable) { if (!editable) isEditing = false }
    LaunchedEffect(isEditing) { if (isEditing) inputFocusRequester.requestFocus() }

    val insideClickFlag = remember { AtomicBoolean(false) }

    DisposableEffect(isEditing) {
        if (!isEditing) return@DisposableEffect onDispose {}

        val awtEventListener = AWTEventListener { event ->
            if (event is MouseEvent && event.id == MouseEvent.MOUSE_PRESSED) {
                insideClickFlag.store(false)
                SwingUtilities.invokeLater { if (!insideClickFlag.load()) isEditing = false }
            }
            if (event is WindowEvent && event.id == WindowEvent.WINDOW_LOST_FOCUS) isEditing = false
        }

        Toolkit.getDefaultToolkit().addAWTEventListener(awtEventListener, AWTEvent.MOUSE_EVENT_MASK or AWTEvent.WINDOW_FOCUS_EVENT_MASK)
        onDispose { Toolkit.getDefaultToolkit().removeAWTEventListener(awtEventListener) }
    }

    Row(modifier.fillMaxWidth().height(if (elegant) 36.dp else 24.dp).background(animatedColor, RoundedCornerShape(animatedRadius)).only(editable) {
        hoverable(hoverSource).only(!isEditing) { pointerHoverIcon(PointerIcon.Hand) }.clickable(noRippleSource, null) { if (!isEditing) isEditing = true }
    }.pointerInput(Unit) {
        awaitPointerEventScope { while (true) if (awaitPointerEvent(PointerEventPass.Initial).type == PointerEventType.Press) insideClickFlag.store(true) }
    }.padding(horizontal = (if (elegant) 16 else 2).dp), verticalAlignment = Alignment.CenterVertically) {
        if (isEditing && editable) Row(Modifier.fillMaxWidth(), Arrangement.spacedBy(2.dp), Alignment.CenterVertically) {
            BasicTextField(
                editText, onEditTextChange, Modifier.weight(1F).focusRequester(inputFocusRequester).onPreviewKeyEvent {
                    if (it.type == KeyEventType.KeyDown && (it.key == Key.Enter || it.key == Key.Escape)) {
                        isEditing = false
                        true
                    } else false
                }, singleLine = true, textStyle = MaterialTheme.typography.labelLarge.copy(color = MaterialTheme.colorScheme.onSurface), cursorBrush = SolidColor(MaterialTheme.colorScheme.onSurface)
            )
            RyoIconButton(Res.drawable.ic_close_24px, if (elegant) 28 else 24) {
                onEditTextChange("")
                isEditing = false
            }
        } else Text(label.resolve(), Modifier.widthIn(min = 0.dp), color = MaterialTheme.colorScheme.onSurfaceVariant, maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.labelLarge)
    }
}