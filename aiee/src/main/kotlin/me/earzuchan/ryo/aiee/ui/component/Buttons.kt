package me.earzuchan.ryo.aiee.ui.component

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.size
import androidx.compose.material3.Button
import androidx.compose.material3.ElevatedButton
import androidx.compose.material3.FilledTonalButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.IconButtonColors
import androidx.compose.material3.IconButtonDefaults
import androidx.compose.material3.LocalMinimumInteractiveComponentSize
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.Text
import androidx.compose.material3.TextButton
import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.util.UiUtils.vector
import org.jetbrains.compose.resources.DrawableResource

enum class ButtonType {
    Text,
    Filled,
    Tonal,
    Outlined,
    Elevated
}

@Composable
fun RyoButton(text: String, modifier: Modifier = Modifier, enabled: Boolean = true, icon: DrawableResource? = null, type: ButtonType = ButtonType.Text, minInteractiveSize: Dp = 0.dp, onClick: () -> Unit) = CompositionLocalProvider(LocalMinimumInteractiveComponentSize provides minInteractiveSize) {
    @Composable
    fun ButtonContent(text: String, icon: DrawableResource?) = Row(horizontalArrangement = Arrangement.spacedBy(8.dp), verticalAlignment = Alignment.CenterVertically) {
        if (icon != null) Icon(icon.vector, null, Modifier.size(18.dp))
        Text(text, maxLines = 1, overflow = TextOverflow.Ellipsis)
    }

    when (type) {
        ButtonType.Text -> TextButton(onClick = onClick, modifier = modifier, enabled = enabled) { ButtonContent(text, icon) }
        ButtonType.Filled -> Button(onClick = onClick, modifier = modifier, enabled = enabled) { ButtonContent(text, icon) }
        ButtonType.Tonal -> FilledTonalButton(onClick = onClick, modifier = modifier, enabled = enabled) { ButtonContent(text, icon) }
        ButtonType.Outlined -> OutlinedButton(onClick = onClick, modifier = modifier, enabled = enabled) { ButtonContent(text, icon) }
        ButtonType.Elevated -> ElevatedButton(onClick = onClick, modifier = modifier, enabled = enabled) { ButtonContent(text, icon) }
    }
}

@Composable
fun RyoIconButton(icon: DrawableResource, dpSize: Int, hintText: String? = null, colors: IconButtonColors = IconButtonDefaults.iconButtonColors(), onClick: () -> Unit) = IconButton(onClick, Modifier.size(dpSize.dp), colors = colors) {
    Icon(icon.vector, hintText)
}