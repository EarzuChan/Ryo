package me.earzuchan.ryo.aiee.app

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.update
import me.earzuchan.ryo.aiee.util.CoroutineScopeOwner
import me.earzuchan.ryo.aiee.util.FileUtils.nameWithoutExt
import me.earzuchan.ryo.foundation.io.loadVolumeChamberFrom
import me.earzuchan.ryo.foundation.io.writeTo
import me.earzuchan.ryo.modern.volume.ManagedVolume
import okio.Path
import java.util.UUID

class WorkspaceService(private val appService: AppService) : CoroutineScopeOwner() {
    // 卷管理

    private val ryo get() = appService.ryo
    private val modernRyo get() = appService.modernRyo

    data class VolumeState(val displayName: String, val volume: ManagedVolume, val filePath: Path? = null, val id: UUID = UUID.randomUUID())

    val VolumeState.isActuallyDirty: Flow<Boolean> get() = volume.isDirty.map { it || filePath == null }

    private val _volumes = MutableStateFlow<List<VolumeState>>(emptyList())
    val volumes: StateFlow<List<VolumeState>> = _volumes.asStateFlow()

    private fun updateVolume(id: UUID, block: (VolumeState) -> VolumeState) = _volumes.update { list ->
        val index = list.indexOfFirst { it.id == id }
        if (index == -1) throw NoSuchElementException("这小子最精了，没有卷也要更新：$id")
        list.toMutableList().apply { this[index] = block(this[index]) }
    }

    fun createVolume(name: String) = _volumes.update { it + VolumeState(name, modernRyo.manage(ryo.createVolumeChamber())) }

    // TIPS：打开和保存，调用弹窗是前端（Duty）行为

    fun openVolume(path: Path, name: String = path.nameWithoutExt) = _volumes.update { it + VolumeState(name, modernRyo.manage(ryo.loadVolumeChamberFrom(path))) }

    fun saveVolume(volumeId: UUID, targetPath: Path? = null) {
        val vol = _volumes.value.find { it.id == volumeId } ?: return
        val finalPath = targetPath ?: vol.filePath ?: return // 既没传新路径也没旧路径则返回

        vol.volume.run {
            chamber.writeTo(finalPath)
            markClean()
        }

        updateVolume(volumeId) { it.copy(filePath = finalPath) }
    }

    fun renameVolume(volumeId: UUID, newName: String) = updateVolume(volumeId) { it.copy(displayName = newName, filePath = null) }

    fun collectGarbageVolume(volumeId: UUID) = _volumes.value.first { it.id == volumeId }.volume.collectGarbage()

    // TIPS：不带脏检测，脏检测请在前台完成
    fun closeVolume(volumeId: UUID) = _volumes.value.first { it.id == volumeId }.let { _volumes.value -= it }

    // TODO：卷还不能克隆，除非底层就绪
    /*fun cloneVolume(volumeId: UUID, newName: String) {
        val source = _volumes.value.find { it.id == volumeId } ?: return
        val cloned = source.copy(displayName = newName, id = UUID.randomUUID(), filePath = null, volume = source.volume.getCloned())
        _volumes.update { it + cloned }
    }*/

    // TIPS：活跃卷是前端概念，理想状态下是当前tab的所属卷，在真条目tab实现之前，fallback到最后一个点击的tree-node

    // 其它

    // 初始化

    init {

    }

    // CLEAR UP
    fun shutdown() = shutdownCoroutineScope()
}