package me.earzuchan.ryo.aiee.data.repository

import androidx.datastore.core.DataStore
import androidx.datastore.preferences.core.Preferences
import androidx.datastore.preferences.core.edit
import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.map
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences

class RyoPreferencesRepository(private val dataStore: DataStore<Preferences>) {
    fun <T> getPreferenceFlow(key: Preferences.Key<T>, defaultValue: T): Flow<T> = dataStore.data.map { prefs -> prefs[key] ?: defaultValue }

    suspend fun <T> setPreference(key: Preferences.Key<T>, value: T) {
        dataStore.edit { prefs -> prefs[key] = value }
    }

    // SPEC

    fun languageFlow(): Flow<RyoPreferences.Language> = getPreferenceFlow(
        RyoPreferences.KEY_LANGUAGE, RyoPreferences.DEFAULT_LANGUAGE.name
    ).map { RyoPreferences.Language.valueOf(it) }

    suspend fun setLanguage(lang: RyoPreferences.Language) = setPreference(RyoPreferences.KEY_LANGUAGE, lang.name)

    fun themeModeFlow(): Flow<RyoPreferences.ThemeMode> = getPreferenceFlow(
        RyoPreferences.KEY_THEME_MODE, RyoPreferences.DEFAULT_THEME_MODE.name
    ).map { RyoPreferences.ThemeMode.valueOf(it) }

    suspend fun setThemeMode(mode: RyoPreferences.ThemeMode) = setPreference(RyoPreferences.KEY_THEME_MODE, mode.name)
}