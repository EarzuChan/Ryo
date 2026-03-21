package me.earzuchan.ryo.aiee.di

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.PreferenceDataStoreFactory
import androidx.datastore.preferences.core.Preferences
import me.earzuchan.ryo.aiee.prefs.DataStorePrefs
import me.earzuchan.ryo.aiee.prefs.DataStorePrefsImpl
import org.koin.dsl.module
import java.io.File

val appModule = module {
    single<DataStore<Preferences>> {
        val prefsDir = File(System.getProperty("user.home"), ".ryo/aiee").also(File::mkdirs)
        PreferenceDataStoreFactory.create { File(prefsDir, "app-settings.preferences_pb") }
    }

    single<DataStorePrefs> { DataStorePrefsImpl(get()) }
}
