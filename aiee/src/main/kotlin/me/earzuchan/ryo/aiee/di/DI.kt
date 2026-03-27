package me.earzuchan.ryo.aiee.di

import me.earzuchan.ryo.aiee.app.*
import me.earzuchan.ryo.aiee.data.database.Database
import me.earzuchan.ryo.aiee.data.repository.FuckedWorkspaceRepository
import me.earzuchan.ryo.aiee.data.repository.PreferencesRepository
import me.earzuchan.ryo.aiee.data.repository.ShortcutRepository
import me.earzuchan.ryo.aiee.util.DataUtils
import org.koin.dsl.module

val appModule = module {
    single { DataUtils.buildPreferences() }

    single { DataUtils.buildDatabase() }
    single { get<Database>().shortcutOverrideDao() }

    single { PreferencesRepository(get()) }
    single { ShortcutRepository(get()) }
    single { FuckedWorkspaceRepository() }

    single { MenuService() }
    single { DialogService() }
    single { ShortcutService(get()) }
    single { CommandService(get()) }
    single { AppService(get()) }
    single { WorkspaceService(get()) }
    single { OldWorkspaceService(get()) }
}
