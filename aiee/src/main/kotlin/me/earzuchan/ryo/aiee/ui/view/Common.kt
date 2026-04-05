package me.earzuchan.ryo.aiee.ui.view

import androidx.compose.runtime.Composable
import androidx.compose.runtime.key
import me.earzuchan.ryo.aiee.app.DialogService
import me.earzuchan.ryo.aiee.app.MenuService
import me.earzuchan.ryo.aiee.ui.component.RyoMenu
import me.earzuchan.ryo.aiee.ui.dialog.CommonDialog
import me.earzuchan.ryo.aiee.ui.dialog.DialogBase
import org.koin.compose.koinInject

@Composable
fun DialogHostView(dialogService: DialogService) {
    val dialog = dialogService.currentDialog ?: return
    val dialogId = dialogService.currentDialogId ?: return

    key(dialogId) {
        DialogBase(dialogService.ctrlShow, dialog.showOverlay, dialogService::clickOverlayCurrent, dialogService::notifyCurrentOpened, dialogService::notifyCurrentClosed) {
            when (dialog) {
                is DialogService.Model.Common -> CommonDialog(dialog, dialogService::clickActionCurrent)

                is DialogService.Model.Special -> dialog.content(dialogService.dialogController)
            }
        }
    }
}

@Composable
fun MenuHostView(menuService: MenuService) {
    val request = menuService.renderRequest ?: return
    RyoMenu(menuService.expanded, request.anchorX, request.anchorY, request.entries, menuService::dismiss, request.selectedIndex)
}