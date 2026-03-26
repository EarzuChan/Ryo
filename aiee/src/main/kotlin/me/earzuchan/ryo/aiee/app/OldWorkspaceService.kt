package me.earzuchan.ryo.aiee.app

import me.earzuchan.ryo.aiee.data.repository.FuckedWorkspaceRepository
import me.earzuchan.ryo.aiee.util.OldFileDialogUtils
import okio.Path

// CHECK：应让本玩意管理：卷、编辑会话、Tabs（中枢性质的管理）

class OldWorkspaceService(private val repo: FuckedWorkspaceRepository) {
    val state get() = repo.state

    suspend fun openVolume(path: Path) = repo.openVolume(path)

    suspend fun openVolumeByDialog(): Result<Unit> {
        val path = OldFileDialogUtils.pickVolumePathToOpen() ?: return Result.success(Unit)
        return repo.openVolume(path)
    }

    suspend fun saveVolume(volumeId: String): Result<Unit> = state.value.volumes.firstOrNull { it.id == volumeId }?.let {
        if (it.path == null) saveVolumeAsByDialog(volumeId) else repo.saveVolume(volumeId)
    } ?: Result.failure(IllegalStateException("Volume not found: $volumeId"))

    suspend fun saveVolumeAs(volumeId: String, path: Path) = repo.saveVolumeAs(volumeId, path)

    suspend fun saveVolumeAsByDialog(volumeId: String): Result<Unit> {
        val volume = state.value.volumes.firstOrNull { it.id == volumeId } ?: return Result.failure(IllegalStateException("Volume not found: $volumeId"))
        val suggestedName = volume.path?.substringAfterLast('\\')?.substringAfterLast('/') ?: volume.name
        return OldFileDialogUtils.pickVolumePathToSave(suggestedName)?.let { repo.saveVolumeAs(volumeId, it) } ?: Result.success(Unit)
    }

    suspend fun saveActiveVolume(): Result<Unit> {
        val activeVolumeId = state.value.activeVolumeId ?: return Result.failure(IllegalStateException("No active volume"))
        return saveVolume(activeVolumeId)
    }

    suspend fun saveActiveVolumeAsByDialog(): Result<Unit> {
        val activeVolumeId = state.value.activeVolumeId ?: return Result.failure(IllegalStateException("No active volume"))
        return saveVolumeAsByDialog(activeVolumeId)
    }

    suspend fun closeVolume(volumeId: String) = repo.closeVolume(volumeId)

    suspend fun closeActiveVolume(): Result<Unit> {
        val activeVolumeId = state.value.activeVolumeId ?: return Result.failure(IllegalStateException("No active volume"))
        return closeVolume(activeVolumeId)
    }

    fun selectVolume(volumeId: String) = repo.selectVolume(volumeId)

    fun close() = repo.close()
}
