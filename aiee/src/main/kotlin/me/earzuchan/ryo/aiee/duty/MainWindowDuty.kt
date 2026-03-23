package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import me.earzuchan.ryo.aiee.ui.window.RyoWindowController
import me.earzuchan.ryo.aiee.ui.window.RyoWindowInterop

class MainWindowDuty {
    private var windowController: RyoWindowController? = null

    var lifecycleStage by mutableStateOf(AppLifecycleStage.Running); private set
    var isMaximized by mutableStateOf(false); private set

    val windowInterop = object : RyoWindowInterop {
        override fun attachWindowController(controller: RyoWindowController) {
            windowController = controller
            isMaximized = controller.isMaximized
        }

        override fun detachWindowController(controller: RyoWindowController) {
            if (windowController !== controller) return
            windowController = null
            isMaximized = false
        }

        override fun onWindowMaximizedChanged(isMaximized: Boolean) {
            this@MainWindowDuty.isMaximized = isMaximized
        }
    }

    fun markClosing() {
        lifecycleStage = AppLifecycleStage.Closing
    }

    fun cancelClose() {
        lifecycleStage = AppLifecycleStage.Running
    }

    fun minimizeWindow() = windowController?.minimize()

    fun toggleMaximizeWindow() = windowController?.toggleMaximize()

    fun requestWindowClose(fallback: () -> Unit) = windowController?.requestClose() ?: fallback()
}
