package me.earzuchan.ryo.aiee.data.database

import androidx.room.Database
import androidx.room.RoomDatabase
import androidx.room.Entity
import androidx.room.PrimaryKey
import me.earzuchan.ryo.aiee.data.dao.ShortcutOverrideDao

@Entity(tableName = "shortcut_overrides")
data class ShortcutOverrideEntity(
    @PrimaryKey val commandId: String,
    val key: String,
    val ctrl: Boolean,
    val alt: Boolean,
    val shift: Boolean,
    val meta: Boolean
)

@Database(entities = [ShortcutOverrideEntity::class], version = 1, exportSchema = true)
abstract class AppDatabase : RoomDatabase() {
    abstract fun shortcutOverrideDao(): ShortcutOverrideDao
}

