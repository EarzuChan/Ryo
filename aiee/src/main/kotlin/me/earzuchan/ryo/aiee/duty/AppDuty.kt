package me.earzuchan.ryo.aiee.duty

import com.arkivanov.decompose.childContext
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.BuildConfig
import me.earzuchan.ryo.aiee.app.CommandService
import me.earzuchan.ryo.aiee.app.DialogService
import me.earzuchan.ryo.aiee.app.MenuService
import me.earzuchan.ryo.aiee.app.ShortcutService
import me.earzuchan.ryo.aiee.app.WorkspaceService
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.dialog.AboutDialog
import me.earzuchan.ryo.aiee.ui.window.RyoWindowController
import me.earzuchan.ryo.aiee.ui.window.RyoWindowInterop
import me.earzuchan.ryo.aiee.util.FileUtils
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import java.util.*
import com.arkivanov.decompose.ComponentContext as DutyContext

class AppDuty(ctx: DutyContext, private val exitApp: () -> Unit) : KoinComponent, EmpoweredDuty(ctx), WindowDutyScope {
    val workspaceService by inject<WorkspaceService>()
    val commandService by inject<CommandService>()

    override val dialogService = DialogService()
    override val menuService = MenuService()

    val sidePanelDuty = SidePanelDuty(childContext("SidePanel"), this)
    val mainPanelDuty = MainPanelDuty(childContext("MainPanel"))

    // 活跃卷【短期：最后树点击；长期：tab对应卷】，是否应该下放到工作空间？

    private val _activeVolumeId = MutableStateFlow<UUID?>(null)
    val activeVolumeId = _activeVolumeId.asStateFlow()

    fun setActiveVolumeId(id: UUID) {
        _activeVolumeId.value = id
    }

    fun clearActiveVolumeId() {
        _activeVolumeId.value = null
    }

    // 窗口

    fun requestQuit() {
        // TODO：CHECK WORKSPACE
        exitApp()
    }

    val windowTitle = UiText.Plain(BuildConfig.APP_NAME)

    private var windowController: RyoWindowController? = null

    val isMaximized get() = windowController?.isMaximized ?: false

    val windowInterop = object : RyoWindowInterop {
        override fun attachWindowController(controller: RyoWindowController) {
            windowController = controller
        }

        override fun detachWindowController(controller: RyoWindowController) {
            if (windowController == controller) windowController = null
        }
    }

    companion object {
        private const val TAG = "AppDuty"

        const val CMD_OPEN_VOLUME = "$TAG.OpenVolume"
        const val CMD_SAVE_ACTIVE_VOLUME = "$TAG.SaveActiveVolume"
        const val CMD_SAVE_VOLUME = "$TAG.SaveVolume"
        const val CMD_SAVE_ACTIVE_VOLUME_AS = "$TAG.SaveActiveVolumeAs"
        const val CMD_SAVE_VOLUME_AS = "$TAG.SaveVolumeAs"
        const val CMD_CLOSE_ACTIVE_VOLUME = "$TAG.CloseActiveVolume"
        const val CMD_CLOSE_VOLUME = "$TAG.CloseVolume"
        const val CMD_SHOW_ABOUT_DIALOG = "$TAG.ShowAboutDialog"
    }

    init {
        // 命令注册
        commandService.register(
            CMD_OPEN_VOLUME,
            ShortcutService.Stroke(ctrl = true, key = ShortcutService.Key.F6),
            { true }) {
            FileUtils.openFile()?.let { workspaceService.openVolume(it) }
        }.autoDispose()

        commandService.register(
            CMD_SAVE_ACTIVE_VOLUME,
            ShortcutService.Stroke(ctrl = true, key = ShortcutService.Key.F7),
            { _activeVolumeId.value != null }) {
            workspaceService.saveVolume(_activeVolumeId.value!!)
        }.autoDispose()

        commandService.register(CMD_SAVE_VOLUME) {
            val id = it[0] as? UUID ?: error("ID一定要是UUID")
            workspaceService.saveVolume(id)
        }.autoDispose()

        commandService.register(
            CMD_SAVE_ACTIVE_VOLUME_AS,
            ShortcutService.Stroke(ctrl = true, key = ShortcutService.Key.F8),
            { false }) {
            // TODO：接入
        }.autoDispose()

        commandService.register(CMD_SAVE_VOLUME_AS) {
            val id = it[0] as? UUID ?: error("ID一定要是UUID")
            // TODO：接入
        }.autoDispose()

        commandService.register(
            CMD_CLOSE_ACTIVE_VOLUME,
            ShortcutService.Stroke(ctrl = true, key = ShortcutService.Key.F9)
        ) {
            // TODO：检查保存境况
            workspaceService.closeVolume(activeVolumeId.value!!)
        }.autoDispose()

        commandService.register(CMD_CLOSE_VOLUME) {
            // TODO：检查保存境况
            val id = it[0] as? UUID ?: error("ID一定要是UUID")
            workspaceService.closeVolume(id)
        }.autoDispose()

        commandService.register(CMD_SHOW_ABOUT_DIALOG) { dialogService.orderSpecial(closeOnOverlayClick = true) { AboutDialog() } }
            .autoDispose()

        // 刷新
        scope.launch {
            workspaceService.volumes.collect { l -> if (l.none { it.id == _activeVolumeId.value }) clearActiveVolumeId() } // 清理届不到的活跃
        }
    }
}