package me.earzuchan.ryo.aiee.data.database

import androidx.room.Database
import androidx.room.RoomDatabase
import me.earzuchan.ryo.aiee.data.dao.ShortcutOverrideDao
import me.earzuchan.ryo.aiee.data.dao.ShortcutOverrideEntity

@Database(entities = [ShortcutOverrideEntity::class], version = 2, exportSchema = true)
abstract class Database : RoomDatabase() {
    abstract fun shortcutOverrideDao(): ShortcutOverrideDao
}

