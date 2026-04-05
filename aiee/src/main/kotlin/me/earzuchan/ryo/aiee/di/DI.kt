package me.earzuchan.ryo.aiee.di

import me.earzuchan.ryo.aiee.app.*
import me.earzuchan.ryo.aiee.data.database.Database
import me.earzuchan.ryo.aiee.data.repository.PreferencesRepository
import me.earzuchan.ryo.aiee.data.repository.ShortcutRepository
import me.earzuchan.ryo.aiee.duty.AppDuty
import me.earzuchan.ryo.aiee.util.DataUtils
import org.koin.dsl.module
import org.koin.dsl.onClose

val appModule = module {
    single { DataUtils.buildPreferences() }

    single { DataUtils.buildDatabase() }
    single { get<Database>().shortcutOverrideDao() }

    single { PreferencesRepository(get()) }
    single { ShortcutRepository(get()) }

    single { ShortcutService(get()) } onClose {it?.shutdown()}
    single { CommandService(get()) } onClose {it?.shutdown()}
    single { AppService(get()) } onClose {it?.shutdown()}
    single { WorkspaceService(get()) } onClose {it?.shutdown()}
}