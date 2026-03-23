package me.earzuchan.ryo.aiee

import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.remember
import androidx.compose.ui.unit.DpSize
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.application
import androidx.compose.ui.window.rememberWindowState
import com.arkivanov.decompose.DefaultComponentContext
import com.arkivanov.decompose.extensions.compose.lifecycle.LifecycleController
import com.arkivanov.essenty.lifecycle.LifecycleRegistry
import me.earzuchan.ryo.aiee.di.appModule
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.illu_ryo_lawnchair
import me.earzuchan.ryo.aiee.ui.window.MainWindowContent
import me.earzuchan.ryo.aiee.ui.window.RyoWindow
import me.earzuchan.ryo.aiee.util.AppLocaleUtils
import me.earzuchan.ryo.aiee.util.ResUtils.paint
import org.koin.compose.KoinApplication

fun main() = application {
    KoinApplication(application = { modules(appModule) }) {
        val lifecycle = remember { LifecycleRegistry() }
        val windowState = rememberWindowState(size = DpSize(1280.dp, 820.dp))
        val appDuty = remember { AppDuty(DefaultComponentContext(lifecycle), ::exitApplication) }

        LifecycleController(lifecycle, windowState)

        val forceDarkMode by appDuty.forceDarkMode.collectAsState()
        val appLanguage by appDuty.appLanguage.collectAsState()
        LaunchedEffect(appLanguage) { AppLocaleUtils.applyAppLanguage(appLanguage) }

        RyoWindow(appDuty::requestClose, windowState, appDuty.windowTitle, Res.drawable.illu_ryo_lawnchair.paint, ryoWindowInterop = appDuty.windowInterop, forceDarkMode = forceDarkMode) { MainWindowContent(appDuty, it) }
    }
}
