package me.earzuchan.ryo.aiee

import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.ui.unit.DpSize
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.application
import androidx.compose.ui.window.rememberWindowState
import com.arkivanov.decompose.DefaultComponentContext as AncestorDutyContext
import com.arkivanov.decompose.extensions.compose.lifecycle.LifecycleController
import com.arkivanov.essenty.lifecycle.LifecycleRegistry
import io.github.vinceglb.filekit.FileKit
import me.earzuchan.ryo.aiee.di.appModule
import me.earzuchan.ryo.aiee.duty.OldAppDuty
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.illu_ryo_lawnchair
import me.earzuchan.ryo.aiee.ui.AppEnvironment
import me.earzuchan.ryo.aiee.ui.window.MainWindowContent
import me.earzuchan.ryo.aiee.ui.window.RyoWindow
import me.earzuchan.ryo.aiee.util.UiUtils.paint
import org.koin.compose.KoinApplication

fun main() {
    FileKit.init(BuildConfig.APP_ID)

    application {
        KoinApplication(application = { modules(appModule) }) {
            val lifecycle = remember { LifecycleRegistry() }
            val windowState = rememberWindowState(size = DpSize(1280.dp, 820.dp))
            val oldAppDuty = remember { OldAppDuty(AncestorDutyContext(lifecycle), ::exitApplication) }

            LifecycleController(lifecycle, windowState)

            val forceDarkMode by oldAppDuty.forceDarkMode.collectAsState()
            val appLanguage by oldAppDuty.appLanguage.collectAsState()

            AppEnvironment(appLanguage) {
                RyoWindow(oldAppDuty::requestClose, windowState, oldAppDuty.windowTitle, Res.drawable.illu_ryo_lawnchair.paint, ryoWindowInterop = oldAppDuty.windowInterop, forceDarkMode = forceDarkMode) { MainWindowContent(oldAppDuty, it) }
            }
        }
    }
}
