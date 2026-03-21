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
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.illu_ryo_cutehead
import me.earzuchan.ryo.aiee.ui.component.HyperlinkText
import me.earzuchan.ryo.aiee.util.ResUtils.paint

@Composable
fun AboutDialog() = Column(Modifier.padding(24.dp).width(240.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
    Row(horizontalArrangement = Arrangement.spacedBy(16.dp), verticalAlignment = Alignment.CenterVertically) {
        Box(Modifier.size(48.dp), Alignment.Center) { Image(Res.drawable.illu_ryo_cutehead.paint, "Ryo头像", Modifier.requiredSize(50.dp)) }
        Column(verticalArrangement = Arrangement.spacedBy(2.dp)) {
            Text(BuildConfig.APP_NAME, style = MaterialTheme.typography.headlineSmall, color = MaterialTheme.colorScheme.primary)
            Text("by Earzu Chan\n${BuildConfig.APP_VERSION}", style = MaterialTheme.typography.titleMedium, color = MaterialTheme.colorScheme.onSurface)
        }
    }

    Column(Modifier.padding(start = 64.dp)) {
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text("哈哈，你想", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            HyperlinkText("支持", BuildConfig.REPO_URL)
            Text("吗", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
        Row(verticalAlignment = Alignment.CenterVertically) {
            Text("怎么，你不", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
            HyperlinkText("关注", BuildConfig.AUTHOR_URL)
            Text("吗", style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
        }
    }
}



