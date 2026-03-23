package me.earzuchan.ryo.aiee.util

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalDensity
import androidx.compose.ui.unit.Dp
import androidx.compose.ui.unit.dp
import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import androidx.datastore.preferences.core.Preferences
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.data.RYO_PREFERENCES_NAME
import okio.Path.Companion.toPath
import org.jetbrains.compose.resources.*
import java.awt.Desktop
import java.io.File
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

    // 每次新建
    /*fun buildAppDatabase(): AppDatabase = PlatformFunctions.getAppDatabaseBuilder()
        .setDriver(BundledSQLiteDriver())
        .setQueryCoroutineContext(PlatformFunctions.ioDispatcher)
        .fallbackToDestructiveMigration(true)
        .build()*/

    fun buildAppPreferences(): DataStore<Preferences> = PreferenceDataStoreFactory.createWithPath(
        produceFile = { "${PlatformUtils.appFilesPath}/$RYO_PREFERENCES_NAME".toPath() }
    )
}

object PlatformUtils {
    /*fun getAppDatabaseBuilder(): RoomDatabase.Builder<AppDatabase> {
        val dbFile = File(appDataPath, "databases/$APP_DATABASE_NAME")
        dbFile.parentFile?.mkdirs()
        return Room.databaseBuilder<AppDatabase>(name = dbFile.absolutePath)
    }*/

    val appFilesPath: String get() = File(appDataPath, "files").also { it.mkdirs() }.absolutePath

    private val appDataPath: File get() = File(System.getProperty("user.home"), BuildConfig.APP_ID)

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

object ResUtils {
    // For XML vector img
    val DrawableResource.vector @Composable get() = vectorResource(this)

    // Only for bitmap
    val DrawableResource.image @Composable get() = imageResource(this)

    // For both vector img and bitmap img
    val DrawableResource.paint @Composable get() = painterResource(this)

    @Composable
    fun StringResource.text(vararg format: String): String = stringResource(this, *format)

    val StringResource.text @Composable get() = this.text()
}