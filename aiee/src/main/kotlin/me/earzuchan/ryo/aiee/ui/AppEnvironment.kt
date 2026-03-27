package me.earzuchan.ryo.aiee.ui

import androidx.compose.runtime.Composable
import androidx.compose.runtime.CompositionLocalProvider
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.remember
import androidx.compose.runtime.staticCompositionLocalOf
import me.earzuchan.ryo.aiee.app.AppService
import me.earzuchan.ryo.aiee.data.preference.Preferences
import me.earzuchan.ryo.aiee.util.AppLocaleUtils
import org.koin.compose.koinInject

val LocalAppLanguage = staticCompositionLocalOf { Preferences.DEFAULT_LANGUAGE }

// TIPS：在组合阶段先同步Locale，确保本轮stringResource读取到新语言
@Composable
fun AppEnvironment(content: @Composable () -> Unit) {
    val appLanguage by koinInject<AppService>().appLanguage.collectAsState()

    remember(appLanguage) { AppLocaleUtils.applyAppLanguage(appLanguage) }

    CompositionLocalProvider(LocalAppLanguage provides appLanguage) { content() }
}
