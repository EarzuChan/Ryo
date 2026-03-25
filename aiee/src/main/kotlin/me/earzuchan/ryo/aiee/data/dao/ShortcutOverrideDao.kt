package me.earzuchan.ryo.aiee.data.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import kotlinx.coroutines.flow.Flow
import me.earzuchan.ryo.aiee.data.database.ShortcutOverrideEntity

@Dao
interface ShortcutOverrideDao {
    @Query("SELECT * FROM shortcut_overrides ORDER BY commandId")
    fun observeAll(): Flow<List<ShortcutOverrideEntity>>

    @Query("SELECT * FROM shortcut_overrides")
    suspend fun getAll(): List<ShortcutOverrideEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(override: ShortcutOverrideEntity)

    @Query("DELETE FROM shortcut_overrides WHERE commandId = :commandId")
    suspend fun deleteByCommand(commandId: String)

    @Query("DELETE FROM shortcut_overrides")
    suspend fun deleteAll()
}

