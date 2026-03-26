package me.earzuchan.ryo.aiee.ui.window

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.key.onPreviewKeyEvent
import androidx.compose.ui.window.FrameWindowScope
import me.earzuchan.ryo.aiee.duty.OldAppDuty
import me.earzuchan.ryo.aiee.ui.view.AppDialogHostView
import me.earzuchan.ryo.aiee.ui.view.AppMenuHostView
import me.earzuchan.ryo.aiee.ui.view.AppTopBarView
import me.earzuchan.ryo.aiee.ui.view.MainWorkspaceView
import me.earzuchan.ryo.aiee.ui.view.SidePanelView


@Composable
fun FrameWindowScope.MainWindowContent(oldAppDuty: OldAppDuty, ryoWindowScope: RyoWindowScope) = Box(Modifier.fillMaxSize().onPreviewKeyEvent(oldAppDuty::handlePreviewKeyEvent)) { // 传给AppDuty来处理按键事件
    Column(Modifier.fillMaxSize()) {
        AppTopBarView(oldAppDuty,ryoWindowScope::WindowControlButtons)
        Row(Modifier.fillMaxSize()) {
            SidePanelView(oldAppDuty.sidePanelDuty, oldAppDuty::openSettingsTab, oldAppDuty::showContextMenu)
            MainWorkspaceView(oldAppDuty)
        }
    }

    AppMenuHostView(oldAppDuty.menuDuty)
    AppDialogHostView(oldAppDuty.dialogDuty)
}
