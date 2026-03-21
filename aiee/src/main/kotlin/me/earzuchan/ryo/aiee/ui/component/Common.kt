package me.earzuchan.ryo.aiee.ui.component

import androidx.compose.foundation.clickable
import androidx.compose.foundation.hoverable
import androidx.compose.foundation.interaction.MutableInteractionSource
import androidx.compose.foundation.interaction.collectIsHoveredAsState
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.input.pointer.PointerIcon
import androidx.compose.ui.input.pointer.pointerHoverIcon
import androidx.compose.ui.text.TextStyle
import androidx.compose.ui.text.style.TextDecoration
import me.earzuchan.ryo.aiee.util.PlatformUtils

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