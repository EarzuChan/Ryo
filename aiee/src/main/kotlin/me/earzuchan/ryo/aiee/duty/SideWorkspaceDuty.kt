package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.arkivanov.decompose.ComponentContext
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.ic_list_24px
import me.earzuchan.ryo.aiee.resources.ic_list_filled_24px
import me.earzuchan.ryo.aiee.resources.ic_schemas_24px
import me.earzuchan.ryo.aiee.resources.ic_schemas_filled_24px
import org.jetbrains.compose.resources.DrawableResource

class SideWorkspaceDuty(ctx: ComponentContext) : ComponentContext by ctx {
    companion object {
        private const val DEFAULT_WIDTH_DP = 280F
        private const val COLLAPSE_THRESHOLD_DP = 80F
        private const val MAX_WIDTH_DP = 560F
    }

    data class Panel(val id: String, val title: String, val icon: DrawableResource, val selectedIcon: DrawableResource = icon)

    val panels = listOf(Panel("assets", "资产管理器", Res.drawable.ic_list_24px, Res.drawable.ic_list_filled_24px), Panel("schemas", "Schema管理器", Res.drawable.ic_schemas_24px, Res.drawable.ic_schemas_filled_24px))

    var expanded by mutableStateOf(true)

    var panelWidthDp by mutableStateOf(DEFAULT_WIDTH_DP); private set

    private var collapsedByDrag by mutableStateOf(false)

    var activePanelId by mutableStateOf(panels.first().id)

    val activePanel: Panel get() = panels.firstOrNull { it.id == activePanelId } ?: panels.first()

    fun focusPanel(id: String) {
        if (panels.none { it.id == id }) return
        activePanelId = id
        if (!expanded && collapsedByDrag) panelWidthDp = DEFAULT_WIDTH_DP
        collapsedByDrag = false
        expanded = true
    }

    fun toggleExpanded() {
        if (expanded) return run { expanded = false }
        if (collapsedByDrag) panelWidthDp = DEFAULT_WIDTH_DP
        collapsedByDrag = false
        expanded = true
    }

    fun resizeBy(deltaDp: Float) {
        val targetWidth = panelWidthDp + deltaDp
        if (targetWidth <= COLLAPSE_THRESHOLD_DP) return run {
            collapsedByDrag = true
            expanded = false
        }
        panelWidthDp = targetWidth.coerceAtMost(MAX_WIDTH_DP)
        collapsedByDrag = false
        expanded = true
    }
}
