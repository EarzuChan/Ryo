package me.earzuchan.ryo.aiee.util

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import org.jetbrains.compose.resources.DrawableResource
import org.jetbrains.compose.resources.StringResource
import org.jetbrains.compose.resources.imageResource
import org.jetbrains.compose.resources.painterResource
import org.jetbrains.compose.resources.stringResource
import org.jetbrains.compose.resources.vectorResource
import java.awt.Desktop
import java.net.URI
import javax.swing.SwingUtilities

object RyoLog {
    fun d(tag: String, vararg messages: Any?) = printLog("D", tag, messages)

    fun i(tag: String, vararg messages: Any?) = printLog("I", tag, messages)

    fun w(tag: String, vararg messages: Any?) = printLog("W", tag, messages)

    fun e(tag: String, vararg messages: Any?) = if (messages.isNotEmpty() && messages.last() is Throwable) {
        val throwable = messages.last() as Throwable

        // 拼接除了最后一个 Throwable 之外的所有信息
        println("[E] $tag > ${messages.dropLast(1).joinToString(" ") { it?.toString() ?: "null" }}")

        throwable.printStackTrace()  // 输出详细堆栈信息
    } else printLog("E", tag, messages)

    private fun printLog(level: String, tag: String, messages: Array<out Any?>) = println("[$level] $tag > ${messages.joinToString(" ") { it?.toString() ?: "null" }}")
}

object PlatformUtils {
    fun <T> runOnSwingUiThread(block: () -> T): T {
        if (SwingUtilities.isEventDispatchThread()) return block()

        var error: Throwable? = null
        var result: T? = null

        SwingUtilities.invokeAndWait {
            try {
                result = block()
            } catch (e: Throwable) {
                error = e
            }
        }

        error?.also { throw it }

        @Suppress("UNCHECKED_CAST")
        return result as T
    }

    fun openLink(url: String) {
        if (!Desktop.isDesktopSupported()) return
        runCatching { Desktop.getDesktop().browse(URI(url)) }
    }
}

object ComposeUtils {
    @Composable
    inline fun Modifier.only(condition: Boolean, elseBlock: @Composable Modifier.() -> Modifier = { this }, ifBlock: @Composable Modifier.() -> Modifier): Modifier = if (condition) ifBlock() else elseBlock()

    inline fun Color.opacity(opacity: Float): Color {
        val newAlpha = alpha * opacity
        return this.copy(newAlpha)
    }

    inline val Int.dpPx: Float @Composable get() = this.dp.px

    inline val Dp.px: Float @Composable get() = LocalDensity.current.run { this@px.toPx() }
}

object ResUtils {
    val DrawableResource.vector @Composable get() = vectorResource(this)

    val DrawableResource.image @Composable get() = imageResource(this)

    val DrawableResource.paint @Composable get() = painterResource(this)

    @Composable
    fun StringResource.text(vararg format: String): String = stringResource(this, *format)

    val StringResource.text @Composable get() = this.text()
}