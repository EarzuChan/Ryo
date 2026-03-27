package me.earzuchan.ryo.aiee.util

import me.earzuchan.ryo.aiee.data.preference.Preferences
import java.util.Locale

object AppLocaleUtils {
    private val systemLocaleAtStartup: Locale = Locale.getDefault()

    fun resolveLocale(language: Preferences.Language): Locale = when (language) {
        Preferences.Language.SYSTEM -> if (systemLocaleAtStartup.language.startsWith("zh", ignoreCase = true)) Locale.SIMPLIFIED_CHINESE else Locale.ENGLISH
        Preferences.Language.CHINESE -> Locale.SIMPLIFIED_CHINESE
        Preferences.Language.ENGLISH -> Locale.ENGLISH
    }

    fun applyAppLanguage(language: Preferences.Language) = Locale.setDefault(resolveLocale(language))
}
