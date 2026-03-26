package me.earzuchan.ryo.aiee.di

import me.earzuchan.ryo.aiee.app.OldAppService
import me.earzuchan.ryo.aiee.app.OldWorkspaceService
import me.earzuchan.ryo.aiee.data.database.AppDatabase
import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository
import me.earzuchan.ryo.aiee.data.repository.OldShortcutOverrideRepository
import me.earzuchan.ryo.aiee.data.repository.FuckedWorkspaceRepository
import me.earzuchan.ryo.aiee.util.DataUtils
import org.koin.dsl.module

val appModule = module {
    single { DataUtils.buildAppPreferences() }

    single { DataUtils.buildAppDatabase() }
    single { get<AppDatabase>().shortcutOverrideDao() }

    single { RyoPreferencesRepository(get()) }
    single { OldShortcutOverrideRepository(get()) }
    single { FuckedWorkspaceRepository() }

    single { OldWorkspaceService(get()) }
    single { OldAppService(get(), get()) }
}
