package me.earzuchan.ryo.aiee.app

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob
import kotlinx.coroutines.cancel
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.SharingStarted
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.distinctUntilChanged
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.stateIn
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences.ThemeMode.DARK
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences.ThemeMode.LIGHT
import me.earzuchan.ryo.aiee.data.preference.RyoPreferences.ThemeMode.SYSTEM
import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository

// CHECK：应让本玩意管理：偏好设置（快捷键、语言、偏好编辑器等；这个管理的意思是，你到时App要获取编辑器什么的，也要通过这个中枢）、Ryo实例和Schema

class OldAppService(private val prefsRepo: RyoPreferencesRepository, val oldWorkspaceService: OldWorkspaceService) {
    private val serviceScope = CoroutineScope(SupervisorJob() + Dispatchers.Main.immediate)

    private val _appThemeMode = MutableStateFlow(RyoPreferences.DEFAULT_THEME_MODE)
    private val _appLanguage = MutableStateFlow(RyoPreferences.DEFAULT_LANGUAGE)

    val appThemeMode: StateFlow<RyoPreferences.ThemeMode> = _appThemeMode.asStateFlow()
    val appLanguage: StateFlow<RyoPreferences.Language> = _appLanguage.asStateFlow()

    val forceDarkMode = appThemeMode.map {
        when (it) {
            SYSTEM -> null
            DARK -> true
            LIGHT -> false
        }
    }.stateIn(serviceScope, SharingStarted.WhileSubscribed(5000), null)

    init {
        serviceScope.launch { prefsRepo.themeModeFlow().distinctUntilChanged().collect(_appThemeMode::emit) }
        serviceScope.launch { prefsRepo.languageFlow().distinctUntilChanged().collect(_appLanguage::emit) }
    }

    fun setAppThemeMode(mode: RyoPreferences.ThemeMode) = serviceScope.launch {
        if (_appThemeMode.value != mode) _appThemeMode.emit(mode)
        prefsRepo.setThemeMode(mode)
    }

    fun setAppLanguage(lang: RyoPreferences.Language) = serviceScope.launch {
        if (_appLanguage.value != lang) _appLanguage.emit(lang)
        prefsRepo.setLanguage(lang)
    }

    fun close() {
        serviceScope.cancel()
        oldWorkspaceService.close()
    }
}
