package me.earzuchan.ryo.aiee.duty

import kotlinx.coroutines.CoroutineScope
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

class PreferencesDuty(private val dutyScope: CoroutineScope, private val prefsRepo: RyoPreferencesRepository) {
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
    }.stateIn(dutyScope, SharingStarted.WhileSubscribed(5000), null)

    init {
        dutyScope.launch { prefsRepo.themeModeFlow().distinctUntilChanged().collect(_appThemeMode::emit) }
        dutyScope.launch { prefsRepo.languageFlow().distinctUntilChanged().collect(_appLanguage::emit) }
    }

    fun setAppThemeMode(mode: RyoPreferences.ThemeMode) = dutyScope.launch {
        if (_appThemeMode.value != mode) _appThemeMode.emit(mode)
        prefsRepo.setThemeMode(mode)
    }

    fun setAppLanguage(lang: RyoPreferences.Language) = dutyScope.launch {
        if (_appLanguage.value != lang) _appLanguage.emit(lang)
        prefsRepo.setLanguage(lang)
    }
}
