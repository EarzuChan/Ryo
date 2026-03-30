package me.earzuchan.ryo.aiee.util

import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import androidx.room.Room
import androidx.sqlite.driver.bundled.BundledSQLiteDriver
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.data.RYO_DATABASE_NAME
import me.earzuchan.ryo.aiee.data.RYO_PREFERENCES_NAME
import me.earzuchan.ryo.aiee.data.database.Database
import okio.FileSystem
import okio.Path
import okio.Path.Companion.toPath
import java.awt.Desktop
import java.net.URI
import javax.swing.SwingUtilities

abstract class CoroutineObject {
    protected val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)

    fun destroy() = scope.cancel() // TODO：啊你怎么不取消！干！
}

object DataUtils {
    fun buildDatabase() = Room.databaseBuilder<Database>((PlatformUtils.appDatabasePath / RYO_DATABASE_NAME).toString()).setDriver(BundledSQLiteDriver()).setQueryCoroutineContext(Dispatchers.IO).fallbackToDestructiveMigration(true).build()

    fun buildPreferences() = PreferenceDataStoreFactory.createWithPath(produceFile = { PlatformUtils.appFilesPath / RYO_PREFERENCES_NAME })
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
