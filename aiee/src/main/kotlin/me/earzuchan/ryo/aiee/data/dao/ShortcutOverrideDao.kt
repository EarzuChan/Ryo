package me.earzuchan.ryo.aiee.data.dao

import androidx.room.*
import kotlinx.coroutines.flow.Flow
import me.earzuchan.ryo.aiee.app.ShortcutService

@Entity(tableName = "shortcut_overrides")
data class ShortcutOverrideEntity(
    @PrimaryKey val commandId: String,
    val keyName: String,
    val ctrl: Boolean,
    val alt: Boolean,
    val shift: Boolean,
    val meta: Boolean
) {
    // 转换为领域模型
    fun toStroke() = ShortcutService.Stroke(ShortcutService.Key.valueOf(keyName), ctrl, alt, shift, meta)
}

// 领域模型转 Entity 的扩展方法
fun ShortcutService.Stroke.toEntity(commandId: String) = ShortcutOverrideEntity(commandId, key.name, ctrl, alt, shift, meta)

@Dao
interface ShortcutOverrideDao {
    // 核心：返回 Flow，数据库一旦变化，这里会自动推送最新数据
    @Query("SELECT * FROM shortcut_overrides")
    fun observeAll(): Flow<List<ShortcutOverrideEntity>>

    @Insert(onConflict = OnConflictStrategy.REPLACE)
    suspend fun insert(entity: ShortcutOverrideEntity)

    @Query("DELETE FROM shortcut_overrides WHERE commandId = :commandId")
    suspend fun deleteByCommandId(commandId: String)
}