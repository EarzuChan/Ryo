package me.earzuchan.ryo.aiee.ui.dialog

import androidx.compose.foundation.Image
import androidx.compose.foundation.layout.*
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.HyperlinkText
import me.earzuchan.ryo.aiee.util.ResUtils.paint
import me.earzuchan.ryo.aiee.util.ResUtils.text

@Composable
fun AboutDialog() = Column(Modifier.padding(24.dp).width(240.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
    Row(horizontalArrangement = Arrangement.spacedBy(16.dp), verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.size(48.dp), Alignment.Center) { Image(Res.drawable.illu_ryo_cutehead.paint, Res.string.about_avatar_cd.text, Modifier.requiredSize(50.dp)) }
        Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
            Text(BuildConfig.APP_NAME, style = MaterialTheme.typography.headlineSmall, color = MaterialTheme.colorScheme.primary)
            Text(Res.string.about_author_version_format.text(BuildConfig.APP_AUTHOR, BuildConfig.APP_VER), style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.onSurface)
        }
    }

    Column(Modifier.padding(start = 64.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(Res.string.about_support_prefix.text, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            HyperlinkText(Res.string.about_support_link_text.text, BuildConfig.REPO_URL)
            Text(Res.string.about_support_suffix.text, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text(Res.string.about_follow_prefix.text, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            HyperlinkText(Res.string.about_follow_link_text.text, BuildConfig.AUTHOR_URL)
            Text(Res.string.about_follow_suffix.text, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}
