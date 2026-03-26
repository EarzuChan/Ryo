package me.earzuchan.ryo.aiee.data.dao

import androidx.room.Dao
import androidx.room.Insert
import androidx.room.OnConflictStrategy
import androidx.room.Query
import kotlinx.coroutines.flow.Flow
import me.earzuchan.ryo.aiee.data.database.OldShortcutOverrideEntity

@Dao
interface OldShortcutOverrideDao {
    @Query("SELECT * FROM shortcut_overrides ORDER BY commandId")
    fun observeAll(): Flow<List<OldShortcutOverrideEntity>>

    @Query("SELECT * FROM shortcut_overrides")
    suspend fun getAll(): List<OldShortcutOverrideEntity>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun upsert(override: OldShortcutOverrideEntity)

    @Query("DELETE FROM shortcut_overrides WHERE commandId = :commandId")
    suspend fun deleteByCommand(commandId: String)

    @Query("DELETE FROM shortcut_overrides")
    suspend fun deleteAll()
}

