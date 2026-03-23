package me.earzuchan.ryo.aiee.ui.window

import androidx.compose.foundation.border
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.Surface
import androidx.compose.runtime.Composable
import androidx.compose.runtime.DisposableEffect
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.Stable
import androidx.compose.runtime.remember
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.painter.Painter
import androidx.compose.ui.unit.dp
import androidx.compose.ui.window.FrameWindowScope
import androidx.compose.ui.window.Window
import androidx.compose.ui.window.WindowPlacement
import androidx.compose.ui.window.WindowState
import io.github.kdroidfilter.platformtools.darkmodedetector.isSystemInDarkMode
import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository
import me.earzuchan.ryo.aiee.resources.*
import me.earzuchan.ryo.aiee.ui.component.RyoIconButton
import me.earzuchan.ryo.aiee.ui.theme.RyoTheme
import me.earzuchan.ryo.aiee.util.ResUtils.text

interface RyoWindowInterop {
    fun attachWindowController(controller: RyoWindowController) = Unit

    fun detachWindowController(controller: RyoWindowController) = Unit

    fun onWindowMaximizedChanged(isMaximized: Boolean) = Unit
}

@Stable
class RyoWindowController internal constructor(private val windowState: WindowState, private val onRequestClose: () -> Unit) {
    val isMaximized: Boolean get() = windowState.placement == WindowPlacement.Maximized

    fun minimize() = run { windowState.isMinimized = true }

    fun toggleMaximize() = run { windowState.placement = if (isMaximized) WindowPlacement.Floating else WindowPlacement.Maximized }

    fun requestClose() = onRequestClose()
}

class RyoWindowScope internal constructor(private val windowController: RyoWindowController) {
    @Composable
    fun WindowControlButtons() = Row {
        RyoIconButton(Res.drawable.ic_minimize_24px, 48, Res.string.window_action_minimize.text, onClick = windowController::minimize)
        RyoIconButton(
            if (windowController.isMaximized) Res.drawable.ic_leave_fullscreen_24px else Res.drawable.ic_fullscreen_24px,
            48,
            if (windowController.isMaximized) Res.string.window_action_restore.text else Res.string.window_action_maximize.text,
            onClick = windowController::toggleMaximize
        )
        RyoIconButton(Res.drawable.ic_close_24px, 48, Res.string.window_action_close.text, onClick = windowController::requestClose)
    }
}

@Composable
fun RyoWindow(onCloseRequest: () -> Unit, windowState: WindowState, title: String, icon: Painter? = null, visible: Boolean = true, ryoWindowInterop: RyoWindowInterop? = null, forceDarkMode: Boolean?, content: @Composable FrameWindowScope.(RyoWindowScope) -> Unit) {
    Window(onCloseRequest, windowState, visible, title, icon, undecorated = true, transparent = true) {
        val windowController = remember(windowState, onCloseRequest) { RyoWindowController(windowState, onCloseRequest) }
        val scope = remember(windowController) { RyoWindowScope(windowController) }

        @Composable
        fun frameModifier(isMaximized: Boolean): Modifier {
            val mod = Modifier.fillMaxSize()
            if (isMaximized) return mod

            val shape = RoundedCornerShape(17.dp)
            return mod.clip(shape).border(1.dp, MaterialTheme.colorScheme.outlineVariant, shape).padding(1.dp)
        }

        DisposableEffect(windowController, ryoWindowInterop) {
            ryoWindowInterop?.attachWindowController(windowController)
            onDispose { ryoWindowInterop?.detachWindowController(windowController) }
        }

        val isMaximized = windowController.isMaximized
        LaunchedEffect(isMaximized, ryoWindowInterop) { ryoWindowInterop?.onWindowMaximizedChanged(isMaximized) }

        RyoTheme(forceDarkMode?: isSystemInDarkMode()) { Surface(frameModifier(isMaximized), color = MaterialTheme.colorScheme.surface) { this@Window.content(scope) } }
    }
}
