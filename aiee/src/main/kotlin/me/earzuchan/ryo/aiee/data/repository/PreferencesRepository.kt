package me.earzuchan.ryo.aiee.data.repository

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import me.earzuchan.ryo.aiee.data.preference.Preferences as RyoPrefs

class PreferencesRepository(private val dataStore: DataStore<Preferences>) {
    fun <T> getPreferenceFlow(key: Preferences.Key<T>, defaultValue: T): Flow<T> = dataStore.data.map { prefs -> prefs[key] ?: defaultValue }

    suspend fun <T> setPreference(key: Preferences.Key<T>, value: T) {
        dataStore.edit { prefs -> prefs[key] = value }
    }

    // SPEC

    val languageFlow: Flow<RyoPrefs.Language> get() = getPreferenceFlow(RyoPrefs.KEY_LANGUAGE, RyoPrefs.DEFAULT_LANGUAGE.name).map { RyoPrefs.Language.valueOf(it) }

    suspend fun setLanguage(lang: RyoPrefs.Language) = setPreference(RyoPrefs.KEY_LANGUAGE, lang.name)

    val themeModeFlow: Flow<RyoPrefs.ThemeMode> get() = getPreferenceFlow(RyoPrefs.KEY_THEME_MODE, RyoPrefs.DEFAULT_THEME_MODE.name).map { RyoPrefs.ThemeMode.valueOf(it) }

    suspend fun setThemeMode(mode: RyoPrefs.ThemeMode) = setPreference(RyoPrefs.KEY_THEME_MODE, mode.name)
}