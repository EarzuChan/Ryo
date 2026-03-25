package me.earzuchan.ryo.aiee.util

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import androidx.room.Room
import androidx.sqlite.driver.bundled.BundledSQLiteDriver
import kotlinx.coroutines.Dispatchers
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.data.RYO_DATABASE_NAME
import me.earzuchan.ryo.aiee.data.RYO_PREFERENCES_NAME
import me.earzuchan.ryo.aiee.data.database.AppDatabase
import me.earzuchan.ryo.aiee.ui.LocalAppLanguage
import okio.FileSystem
import okio.Path
import okio.Path.Companion.toPath
import org.jetbrains.compose.resources.*
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

object MiscUtils {
    private const val TAG = "MiscUtils"

    fun buildAppDatabase() = Room.databaseBuilder<AppDatabase>((PlatformUtils.appDatabasePath / RYO_DATABASE_NAME).toString()).setDriver(BundledSQLiteDriver()).setQueryCoroutineContext(Dispatchers.IO).fallbackToDestructiveMigration(true).build()

    fun buildAppPreferences() = PreferenceDataStoreFactory.createWithPath(produceFile = { PlatformUtils.appFilesPath / RYO_PREFERENCES_NAME })
}

object PlatformUtils {
    private val fs = FileSystem.SYSTEM

    private val appDataPath: Path = System.getProperty("user.home").toPath() / BuildConfig.APP_ID

    val appFilesPath: Path by lazy { (appDataPath / "files").also { fs.createDirectories(it) } }

    val appDatabasePath: Path by lazy { (appDataPath / "databases").also { fs.createDirectories(it) } }

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

        @Suppress("UNCHECKED_CAST") return result as T
    }


    fun openLink(url: String) {
        if (!Desktop.isDesktopSupported()) return
        runCatching { Desktop.getDesktop().browse(URI(url)) }
    }
}

object UiUtils {
    @Composable
    inline fun Modifier.only(
        condition: Boolean, elseBlock: @Composable Modifier.() -> Modifier = { this }, ifBlock: @Composable Modifier.() -> Modifier
    ): Modifier = if (condition) ifBlock() else elseBlock()

    // For XML vector img
    val DrawableResource.vector @Composable get() = vectorResource(this)

    // Only for bitmap
    val DrawableResource.image @Composable get() = imageResource(this)

    // For both vector img and bitmap img
    val DrawableResource.paint @Composable get() = painterResource(this)

    @Composable
    fun StringResource.text(vararg format: String): String {
        LocalAppLanguage.current
        return stringResource(this, *format)
    }

    val StringResource.text @Composable get() = this.text()
}
