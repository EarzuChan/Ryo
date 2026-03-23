package me.earzuchan.ryo.aiee.util

import me.earzuchan.ryo.aiee.data.preference.RyoPreferences
import java.util.Locale

object AppLocaleUtils {
    private val systemLocaleAtStartup: Locale = Locale.getDefault()

    fun resolveLocale(language: RyoPreferences.Language): Locale = when (language) {
        RyoPreferences.Language.SYSTEM -> if (systemLocaleAtStartup.language.startsWith("zh", ignoreCase = true)) Locale.SIMPLIFIED_CHINESE else Locale.ENGLISH
        RyoPreferences.Language.CHINESE -> Locale.SIMPLIFIED_CHINESE
        RyoPreferences.Language.ENGLISH -> Locale.ENGLISH
    }

    fun applyAppLanguage(language: RyoPreferences.Language) = Locale.setDefault(resolveLocale(language))
}
