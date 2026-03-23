package me.earzuchan.ryo.aiee.di

import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository
import me.earzuchan.ryo.aiee.util.MiscUtils
import org.koin.dsl.module

val appModule = module {
    single { MiscUtils.buildAppPreferences() }

    single { RyoPreferencesRepository(get()) }
}
