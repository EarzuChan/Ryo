package me.earzuchan.ryo.aiee.ui.page

import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.layout.width
import androidx.compose.material3.Icon
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.util.UiUtils.vector
import me.earzuchan.ryo.aiee.util.UiUtils.text

@Composable
private fun PlaceholderPage(title: String, subtitle: String) = Box(Modifier.fillMaxSize().padding(horizontal = 24.dp, vertical = 16.dp), Alignment.TopStart) {
    Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
        Text(title, style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.primary, fontWeight = FontWeight.Medium)
        Text(subtitle, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
    }
}

@Composable
fun WelcomePage() = PlaceholderPage(Res.string.page_welcome_title.text, Res.string.page_welcome_subtitle.text)

@Composable
fun EditorSessionPage() = PlaceholderPage(Res.string.page_editor_session_title.text, Res.string.page_editor_session_subtitle.text)

@Composable
fun EmptyPage() = Box(Modifier.fillMaxSize(), Alignment.Center) { Icon(Res.drawable.illu_ryo_mark.vector, Res.string.empty_page_illustration_cd.text, Modifier.width(192.dp), MaterialTheme.colorScheme.outline) }

