package me.earzuchan.ryo.aiee.app

import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.data.preference.Preferences
import me.earzuchan.ryo.aiee.data.preference.Preferences.ThemeMode.*
import me.earzuchan.ryo.aiee.data.repository.PreferencesRepository
import me.earzuchan.ryo.aiee.util.CoroutineScopeOwner
import me.earzuchan.ryo.foundation.Ryo
import me.earzuchan.ryo.modern.modernize

// CHECK：应让本玩意管理：偏好设置（快捷键、语言、偏好编辑器等；这个管理的意思是，你到时App要获取编辑器什么的，也要通过这个中枢）、Ryo实例和Schema

class AppService(private val prefsRepo: PreferencesRepository): CoroutineScopeOwner() {
    // PREFS

    val appThemeMode = prefsRepo.themeModeFlow.stateIn(scope, SharingStarted.WhileSubscribed(5000), Preferences.DEFAULT_THEME_MODE)

    val appLanguage = prefsRepo.languageFlow.stateIn(scope, SharingStarted.WhileSubscribed(5000), Preferences.DEFAULT_LANGUAGE)

    val forceDarkMode = prefsRepo.themeModeFlow.map {
        when (it) {
            SYSTEM -> null
            DARK -> true
            LIGHT -> false
        }
    }.stateIn(scope, SharingStarted.WhileSubscribed(5000), null)

    fun setAppThemeMode(mode: Preferences.ThemeMode) = scope.launch { prefsRepo.setThemeMode(mode) }

    fun setAppLanguage(lang: Preferences.Language) = scope.launch { prefsRepo.setLanguage(lang) }

    // RYO CORE

    val ryo = Ryo()
    val modernRyo = ryo.modernize()

    // CLEAR UP
    fun shutdown() = shutdownCoroutineScope()
}