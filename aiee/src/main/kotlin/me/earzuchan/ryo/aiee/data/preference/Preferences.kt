package me.earzuchan.ryo.aiee.data.preference

import androidx.datastore.preferences.core.booleanPreferencesKey
import androidx.datastore.preferences.core.stringPreferencesKey

object RyoPreferences {
    enum class Language { SYSTEM, CHINESE, ENGLISH }

    enum class ThemeMode { SYSTEM, DARK, LIGHT }

    val KEY_LANGUAGE = stringPreferencesKey("language")
    val DEFAULT_LANGUAGE = Language.SYSTEM

    val KEY_THEME_MODE = stringPreferencesKey("theme_mode")
    val DEFAULT_THEME_MODE = ThemeMode.SYSTEM
}
