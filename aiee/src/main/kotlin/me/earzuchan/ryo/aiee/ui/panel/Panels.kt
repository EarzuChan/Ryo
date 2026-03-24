package me.earzuchan.ryo.aiee.ui.panel

import androidx.compose.foundation.background
import androidx.compose.foundation.clickable
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.text.style.TextOverflow
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.duty.AssetsPanelDuty
import me.earzuchan.ryo.aiee.duty.SchemasPanelDuty
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.component.EditableLabel
import me.earzuchan.ryo.aiee.util.UiUtils.text
import org.jetbrains.compose.resources.StringResource

@Composable
fun AssetsPanel(duty: AssetsPanelDuty, modifier: Modifier = Modifier) {
    val state = duty.state

    PanelScaffold(Res.string.panel_assets_search_hint, state.keyword, duty::search, Res.string.panel_assets_count_format.text(state.entries.size.toString()), modifier) {
        if (state.entries.isEmpty()) PanelEmpty(Res.string.panel_assets_empty.text) else LazyColumn(Modifier.fillMaxSize(), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            items(state.entries, key = { it.id }) { entry ->
                PanelEntry(entry.name, entry.kind, state.selectedId == entry.id) { duty.select(entry.id) }
            }
        }
    }
}

@Composable
fun SchemasPanel(duty: SchemasPanelDuty, modifier: Modifier = Modifier) {
    val state = duty.state

    PanelScaffold(Res.string.panel_schemas_search_hint, state.keyword, duty::search, Res.string.panel_schemas_count_format.text(state.entries.size.toString()), modifier) {
        if (state.entries.isEmpty()) PanelEmpty(Res.string.panel_schemas_empty.text) else LazyColumn(Modifier.fillMaxSize(), verticalArrangement = Arrangement.spacedBy(6.dp)) {
            items(state.entries, key = { it.id }) { entry ->
                val subtitle = "${entry.kind} · ${Res.string.panel_schema_member_count_format.text(entry.memberCount.toString())}"
                PanelEntry(entry.modelId, subtitle, state.selectedId == entry.id) { duty.select(entry.id) }
            }
        }
    }
}

@Composable
private fun PanelScaffold(searchHint: StringResource, keyword: String, onKeywordChange: (String) -> Unit, statsText: String, modifier: Modifier = Modifier, content: @Composable () -> Unit) = Column(
    modifier.fillMaxSize(),
    verticalArrangement = Arrangement.spacedBy(10.dp)
) {
    EditableLabel(UiText.Res(searchHint), keyword, onKeywordChange, true, Modifier.fillMaxWidth())
    Text(statsText, style = MaterialTheme.typography.labelMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
    Box(modifier = Modifier.weight(1F), contentAlignment = Alignment.TopStart) { content() }
}

@Composable
private fun PanelEntry(title: String, subtitle: String, selected: Boolean, onClick: () -> Unit) = Column(
    Modifier.fillMaxWidth().background(if (selected) MaterialTheme.colorScheme.secondaryContainer else MaterialTheme.colorScheme.surface, RoundedCornerShape(12.dp)).clickable(onClick = onClick).padding(horizontal = 12.dp, vertical = 10.dp),
    verticalArrangement = Arrangement.spacedBy(2.dp)
) {
    Text(title, maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.bodyMedium, color = if (selected) MaterialTheme.colorScheme.onSecondaryContainer else MaterialTheme.colorScheme.onSurface)
    Text(subtitle, maxLines = 1, overflow = TextOverflow.Ellipsis, style = MaterialTheme.typography.bodySmall, color = if (selected) MaterialTheme.colorScheme.onSecondaryContainer else MaterialTheme.colorScheme.onSurfaceVariant)
}

@Composable
private fun PanelEmpty(text: String) = Box(Modifier.fillMaxSize().padding(top = 28.dp), Alignment.TopCenter) {
    Text(text, style = MaterialTheme.typography.bodyMedium, color = MaterialTheme.colorScheme.onSurfaceVariant)
}
