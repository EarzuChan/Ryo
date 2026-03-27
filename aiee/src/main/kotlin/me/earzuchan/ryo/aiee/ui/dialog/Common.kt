package me.earzuchan.ryo.aiee.ui.dialog

import androidx.compose.animation.AnimatedVisibility
import androidx.compose.animation.core.FastOutSlowInEasing
import androidx.compose.animation.core.LinearOutSlowInEasing
import androidx.compose.animation.core.MutableTransitionState
import androidx.compose.animation.core.tween
import androidx.compose.animation.fadeIn
import androidx.compose.animation.fadeOut
import androidx.compose.animation.scaleIn
import androidx.compose.animation.scaleOut
import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.BoxScope
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.IntrinsicSize
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.size
import androidx.compose.foundation.layout.width
import androidx.compose.foundation.layout.widthIn
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.platform.LocalWindowInfo
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.app.DialogService
import me.earzuchan.ryo.aiee.ui.component.RyoButton
import me.earzuchan.ryo.aiee.ui.resolve
import me.earzuchan.ryo.aiee.util.UiUtils.vector
import androidx.compose.ui.window.Popup
import androidx.compose.ui.window.PopupProperties
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.util.UiUtils.text

@Composable
fun DialogBase(ctrlShow: Boolean, showOverlay: Boolean = true, onOverlayClick: () -> Unit = {}, onOpened: () -> Unit = {}, onClosed: () -> Unit = {}, content: @Composable BoxScope.() -> Unit) {
    val visibility = remember { MutableTransitionState(false) }
    val overlayInteraction = remember { MutableInteractionSource() }
    val dialogInteraction = remember { MutableInteractionSource() }
    val density = LocalDensity.current
    val windowSize = LocalWindowInfo.current.containerSize
    val popupWidth = with(density) { windowSize.width.coerceAtLeast(1).toDp() }
    val popupHeight = with(density) { windowSize.height.coerceAtLeast(1).toDp() }
    var openedOnce by remember { mutableStateOf(false) }

    LaunchedEffect(ctrlShow) { visibility.targetState = ctrlShow }

    LaunchedEffect(visibility.currentState, visibility.targetState, visibility.isIdle) {
        if (!visibility.isIdle) return@LaunchedEffect
        if (visibility.currentState && visibility.targetState) {
            openedOnce = true
            onOpened()
        } else if (!visibility.currentState && !visibility.targetState && openedOnce) onClosed()
    }

    if (!visibility.currentState && !visibility.targetState) return

    Popup(Alignment.TopStart, properties = PopupProperties(true, false, false, false)) {
        Box(Modifier.width(popupWidth).height(popupHeight), contentAlignment = Alignment.Center) {
            AnimatedVisibility(
                visibility, enter = fadeIn(tween(250, easing = FastOutSlowInEasing)), exit = fadeOut(tween(200, easing = LinearOutSlowInEasing))
            ) {
                Box(Modifier.fillMaxSize().background(if (showOverlay) MaterialTheme.colorScheme.scrim.copy(alpha = 0.4F) else Color.Transparent).clickable(overlayInteraction, null, onClick = onOverlayClick))
            }

            AnimatedVisibility(
                visibleState = visibility,
                enter = fadeIn(tween(250, easing = FastOutSlowInEasing)) + scaleIn(tween(250, easing = FastOutSlowInEasing), 0.98F),
                exit = fadeOut(tween(200, easing = LinearOutSlowInEasing)) + scaleOut(tween(200, easing = LinearOutSlowInEasing), 0.98F)
            ) {
                Surface(
                    Modifier.padding(horizontal = 32.dp, vertical = 72.dp).clip(RoundedCornerShape(28.dp)).clickable(dialogInteraction, null, onClick = {}), color = MaterialTheme.colorScheme.surfaceContainerHigh, shadowElevation = 6.dp
                ) { content() }
            }
        }
    }
}

@Composable
fun CommonDialog(model: DialogService.Model.Common, onActionClick: (DialogService.DialogAction) -> Unit) = Column(Modifier.width(IntrinsicSize.Max).widthIn(min = 240.dp, max = 560.dp)) {
    val tFdp = 24.dp

    Column(Modifier.fillMaxWidth().padding(horizontal = tFdp).padding(top = tFdp), verticalArrangement = Arrangement.spacedBy(16.dp)) { // 有icon时，居中title
        if (model.icon != null) Column(Modifier.fillMaxWidth(), verticalArrangement = Arrangement.spacedBy(16.dp), Alignment.CenterHorizontally) {
            Icon(model.icon.vector, Res.string.dialog_icon_cd.text, Modifier.size(tFdp), tint = MaterialTheme.colorScheme.secondary)
            model.title?.resolve()?.takeIf(String::isNotBlank)?.also { Text(it, color = MaterialTheme.colorScheme.onSurface, style = MaterialTheme.typography.headlineSmall) }
        } else model.title?.resolve()?.takeIf(String::isNotBlank)?.also { Text(it, color = MaterialTheme.colorScheme.onSurface, style = MaterialTheme.typography.headlineSmall) }
        model.description?.resolve()?.takeIf(String::isNotBlank)?.also { Text(it, color = MaterialTheme.colorScheme.onSurfaceVariant, style = MaterialTheme.typography.bodyMedium) }
        model.content?.invoke()
    }

    if (model.actions.isNotEmpty()) Row(Modifier.fillMaxWidth().padding(horizontal = tFdp, vertical = tFdp), horizontalArrangement = Arrangement.spacedBy(8.dp, Alignment.End), verticalAlignment = Alignment.CenterVertically) {
        model.actions.forEach { action -> RyoButton(action.text.resolve(), type = action.type, enabled = action.enabled) { onActionClick(action) } }
    } else Spacer(Modifier.height(tFdp))
}
