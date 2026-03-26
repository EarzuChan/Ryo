package me.earzuchan.ryo.aiee.data.database

import androidx.room.Database
import androidx.room.RoomDatabase
import androidx.room.Entity
import androidx.room.PrimaryKey
import me.earzuchan.ryo.aiee.data.dao.OldShortcutOverrideDao

@Entity(tableName = "shortcut_overrides")
data class OldShortcutOverrideEntity(
    @PrimaryKey val commandId: String,
    val key: String,
    val ctrl: Boolean,
    val alt: Boolean,
    val shift: Boolean,
    val meta: Boolean
)

@Database(entities = [OldShortcutOverrideEntity::class], version = 1, exportSchema = true)
abstract class AppDatabase : RoomDatabase() {
    abstract fun shortcutOverrideDao(): OldShortcutOverrideDao
}

