package me.earzuchan.ryo.aiee.ui.window

import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.input.key.onPreviewKeyEvent
import androidx.compose.ui.window.FrameWindowScope
import me.earzuchan.ryo.aiee.app.CommandService
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.ui.view.AppDialogHostView
import me.earzuchan.ryo.aiee.ui.view.AppMenuHostView
import me.earzuchan.ryo.aiee.ui.view.AppTopBarView
import me.earzuchan.ryo.aiee.ui.view.MainPanelView
import me.earzuchan.ryo.aiee.ui.view.SidePanelView
import org.koin.compose.koinInject


@Composable
fun FrameWindowScope.MainWindowContent(appDuty: AppDuty, ryoWindowScope: RyoWindowScope) {
    val commandService = koinInject<CommandService>()

    Box(Modifier.fillMaxSize().onPreviewKeyEvent(commandService::handleKeyEvent)) {
        Column(Modifier.fillMaxSize()) {
            AppTopBarView(appDuty,ryoWindowScope::WindowControlButtons)
            Row(Modifier.fillMaxSize()) {
                SidePanelView(appDuty.sidePanelDuty)
                MainPanelView(appDuty.mainPanelDuty)
            }
        }

        AppMenuHostView()
        AppDialogHostView()
    }
}
