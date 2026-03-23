package me.earzuchan.ryo.aiee.ui

import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.remember
import androidx.compose.runtime.staticCompositionLocalOf
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences
import me.earzuchan.ryo.aiee.util.AppLocaleUtils

val LocalAppLanguage = staticCompositionLocalOf { RyoPreferences.DEFAULT_LANGUAGE }

@Composable
fun AppEnvironment(appLanguage: RyoPreferences.Language, content: @Composable () -> Unit) {
    // TIPS：在组合阶段先同步Locale，确保本轮stringResource读取到新语言
    remember(appLanguage) { AppLocaleUtils.applyAppLanguage(appLanguage) }

    CompositionLocalProvider(LocalAppLanguage provides appLanguage) { content() }
}
