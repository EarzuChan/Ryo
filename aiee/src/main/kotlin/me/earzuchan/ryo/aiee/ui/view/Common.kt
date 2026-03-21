package me.earzuchan.ryo.aiee.ui.view

import androidx.compose.runtime.Composable
import androidx.compose.runtime.key
import me.earzuchan.ryo.aiee.duty.DialogDuty
import me.earzuchan.ryo.aiee.duty.MenuDuty
import me.earzuchan.ryo.aiee.ui.component.RyoMenu
import me.earzuchan.ryo.aiee.ui.dialog.CommonDialog
import me.earzuchan.ryo.aiee.ui.dialog.DialogBase

@Composable
fun AppDialogHostView(dialogDuty: DialogDuty) {
    val dialog = dialogDuty.currentDialog ?: return
    val dialogId = dialogDuty.currentDialogId ?: return

    key(dialogId) {
        DialogBase(
            ctrlShow = dialogDuty.ctrlShow,
            showOverlay = dialog.showOverlay,
            onOverlayClick = dialogDuty::clickOverlayCurrent,
            onOpened = dialogDuty::notifyCurrentOpened,
            onClosed = dialogDuty::notifyCurrentClosed
        ) {
            when (dialog) {
                is DialogDuty.Model.Common -> CommonDialog(dialog, dialogDuty::clickActionCurrent)
                is DialogDuty.Model.Special -> dialog.content(dialogDuty.dialogController)
            }
        }
    }
}

@Composable
fun AppMenuHostView(menuDuty: MenuDuty) {
    val request = menuDuty.renderRequest ?: return
    RyoMenu(menuDuty.expanded, request.anchorX, request.anchorY, request.entries, menuDuty::dismiss, selectedIndex = request.selectedIndex)
}