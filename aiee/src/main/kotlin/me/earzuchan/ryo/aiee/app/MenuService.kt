package me.earzuchan.ryo.aiee.app

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import androidx.compose.ui.unit.Density
import androidx.compose.ui.unit.dp
import me.earzuchan.ryo.aiee.ui.component.RyoMenuEntry

// FIXME：有一个无伤大雅的小问题：就是从表现上看，从一个右键菜单切到另一个，或者是从菜单栏菜单切到右键菜单时，中间没有“旧”菜单关闭、“新”菜单打开的动画，而且“旧”菜单的hover会被“继承”到新菜单
class MenuService {
    data class Request(val anchorX: Int, val anchorY: Int, val entries: List<RyoMenuEntry>, val selectedIndex: Int? = null)

    companion object {
        private val menuPanelPaddingDp = 8.dp
        private val menuItemHeightDp = 28.dp
        private val menuItemTextStartDp = 8.dp
    }

    var activeMenuBarGroupId by mutableStateOf<String?>(null); private set

    private var currentRequest by mutableStateOf<Request?>(null)
    private var cachedRequest by mutableStateOf<Request?>(null)

    val expanded: Boolean get() = currentRequest != null

    val renderRequest: Request? get() = currentRequest ?: cachedRequest

    fun toggleMenuBarGroup(groupId: String, anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) {
        if (expanded && activeMenuBarGroupId == groupId) {
            dismiss()
            return
        }

        openMenuBarGroup(groupId, anchorX, anchorY, entries)
    }

    fun hoverMenuBarGroup(groupId: String, anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) {
        if (activeMenuBarGroupId == null || activeMenuBarGroupId == groupId) return
        openMenuBarGroup(groupId, anchorX, anchorY, entries)
    }

    fun showContextMenu(anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>, selectedIndex: Int? = null) {
        activeMenuBarGroupId = null
        currentRequest = Request(anchorX, anchorY, entries, selectedIndex)
        cachedRequest = currentRequest
    }

    fun showInPlaceSelectMenu(textX: Int, textCenterY: Int, selectedIndex: Int, density: Density, entries: List<RyoMenuEntry>) {
        val resolvedSelectedIndex = selectedIndex.coerceIn(0, entries.lastIndex.coerceAtLeast(0))
        val menuPanelPaddingPx = with(density) { menuPanelPaddingDp.roundToPx() }
        val menuItemHeightPx = with(density) { menuItemHeightDp.roundToPx() }
        val menuItemTextStartPx = with(density) { menuItemTextStartDp.roundToPx() }
        val menuItemCenterOffsetPx = menuPanelPaddingPx + menuItemHeightPx / 2

        val popupX = textX - menuItemTextStartPx
        val popupY = textCenterY - menuItemCenterOffsetPx - resolvedSelectedIndex * menuItemHeightPx

        showContextMenu(popupX, popupY, entries, selectedIndex = resolvedSelectedIndex)
    }

    fun dismiss() {
        currentRequest = null
        activeMenuBarGroupId = null
    }

    private fun openMenuBarGroup(groupId: String, anchorX: Int, anchorY: Int, entries: List<RyoMenuEntry>) {
        activeMenuBarGroupId = groupId
        currentRequest = Request(anchorX, anchorY, entries, null)
        cachedRequest = currentRequest
    }
}