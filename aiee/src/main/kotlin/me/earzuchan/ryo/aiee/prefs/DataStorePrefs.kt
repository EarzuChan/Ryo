package me.earzuchan.ryo.aiee.prefs

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import androidx.datastore.preferences.core.stringPreferencesKey
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map

enum class UiLanguage(val stored: String, val label: String) {
    Chinese("zh", "中文 Chinese"),
    English("en", "English 英文");

    companion object {
        fun fromStored(stored: String?) = entries.firstOrNull { it.stored == stored } ?: Chinese
    }
}

enum class ThemeMode(val stored: String, val label: String) {
    FollowSystem("system", "跟随系统"),
    Dark("dark", "黑暗"),
    Light("light", "明亮");

    companion object {
        fun fromStored(stored: String?) = entries.firstOrNull { it.stored == stored } ?: FollowSystem
    }
}

data class AppSettings(val uiLanguage: UiLanguage = UiLanguage.Chinese, val themeMode: ThemeMode = ThemeMode.FollowSystem)

interface DataStorePrefs {
    val settingsFlow: Flow<AppSettings>

    suspend fun setUiLanguage(uiLanguage: UiLanguage)

    suspend fun setThemeMode(themeMode: ThemeMode)
}

class DataStorePrefsImpl(private val dataStore: DataStore<Preferences>) : DataStorePrefs {
    private object Keys {
        val uiLanguage = stringPreferencesKey("ui_language")
        val themeMode = stringPreferencesKey("theme_mode")
    }

    override val settingsFlow = dataStore.data.map { prefs -> AppSettings(UiLanguage.fromStored(prefs[Keys.uiLanguage]), ThemeMode.fromStored(prefs[Keys.themeMode])) }

    override suspend fun setUiLanguage(uiLanguage: UiLanguage) {
        dataStore.edit { prefs -> prefs[Keys.uiLanguage] = uiLanguage.stored }
    }

    override suspend fun setThemeMode(themeMode: ThemeMode) {
        dataStore.edit { prefs -> prefs[Keys.themeMode] = themeMode.stored }
    }
}
