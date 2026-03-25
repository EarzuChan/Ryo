package me.earzuchan.ryo.aiee.di

import me.earzuchan.ryo.aiee.data.database.AppDatabase
import me.earzuchan.ryo.aiee.data.repository.RyoPreferencesRepository
import me.earzuchan.ryo.aiee.data.repository.ShortcutOverrideRepository
import me.earzuchan.ryo.aiee.data.repository.WorkspaceRepository
import me.earzuchan.ryo.aiee.data.workspace.WorkspaceRuntime
import me.earzuchan.ryo.aiee.util.MiscUtils
import org.koin.dsl.module

val appModule = module {
    single { MiscUtils.buildAppDatabase() }

    single { MiscUtils.buildAppPreferences() }

    single { get<AppDatabase>().shortcutOverrideDao() }
    single { RyoPreferencesRepository(get()) }
    single { ShortcutOverrideRepository(get()) }
    single { WorkspaceRuntime() }
    single { WorkspaceRepository(get()) }
}
