package me.earzuchan.ryo.aiee.app

import androidx.compose.ui.input.key.KeyEvent
import kotlinx.coroutines.*
import me.earzuchan.ryo.aiee.util.CoroutineObject
import me.earzuchan.ryo.aiee.util.Disposable
import me.earzuchan.ryo.aiee.util.RyoLog

class CommandService(private val shortcutService: ShortcutService) : CoroutineObject() {
    companion object{
        private const val TAG = "CommandService"
    }

    private val executors = mutableMapOf<String, suspend (Array<out Any?>) -> Any?>()
    private val validators = mutableMapOf<String, () -> Boolean>()

    fun register(
        commandId: String,
        defaultShortcut: ShortcutService.Stroke? = null,
        isEnabled: () -> Boolean = { true },
        execute: suspend (args: Array<out Any?>) -> Any?
    ): Disposable {
        if (executors.containsKey(commandId)) error("不可把【${commandId}】注册两回啊两回")

        executors[commandId] = execute
        validators[commandId] = isEnabled
        defaultShortcut?.let { shortcutService.setDefault(commandId, it) }

        return Disposable {
            executors.remove(commandId)
            validators.remove(commandId)
            shortcutService.clearDefault(commandId)
        }
    }

    fun canExecute(commandId: String) = validators[commandId]?.invoke() ?: false

    // 挂起执行
    @Suppress("UNCHECKED_CAST")
    suspend fun <T> execute(commandId: String, vararg args: Any?): T {
        if (!canExecute(commandId)) error("Command not enabled or not found: $commandId")

        val executor = executors[commandId] ?: error("Command not registered: $commandId")

        return executor.invoke(args) as T
    }

    // 发射执行
    fun dispatch(commandId: String, vararg args: Any?) {
        scope.launch {
            try {
                execute<Any?>(commandId, *args)
            } catch (e: Exception) {
                // TODO: 统一异常处理，比如在 UI 弹出 Error Notification
                RyoLog.e(TAG, "卡了：$commandId\n${e.stackTraceToString()}")
            }
        }
    }


    // 供 Compose Window 拦截 KeyEvent
    fun handleKeyEvent(event: KeyEvent): Boolean {
        val commandId = shortcutService.resolve(event) ?: return false
        if (!canExecute(commandId)) return false

        // 快捷键触发通常没有特定参数
        dispatch(commandId)
        return true
    }
}