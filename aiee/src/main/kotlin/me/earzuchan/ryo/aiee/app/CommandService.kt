package me.earzuchan.ryo.aiee.app

import androidx.compose.ui.input.key.KeyEvent
import kotlinx.coroutines.*
import me.earzuchan.ryo.aiee.util.CoroutineScopeOwner
import me.earzuchan.ryo.aiee.util.Disposable
import me.earzuchan.ryo.aiee.util.RyoLog

class CommandService(private val shortcutService: ShortcutService) : CoroutineScopeOwner(Dispatchers.Main.immediate) {
    companion object{
        private const val TAG = "CommandService"
    }

    private val executors = mutableMapOf<String, suspend (Array<out Any?>) -> Any?>()
    private val validators = mutableMapOf<String, () -> Boolean>()

    fun register(commandId: String, defaultShortcut: ShortcutService.Stroke? = null, isEnabled: () -> Boolean = { true }, execute: suspend (args: Array<out Any?>) -> Any?): Disposable {
        RyoLog.d(TAG, "要注册啊一个：$commandId")

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

    // 挂起执行，是给内部互调用使用
    @Suppress("UNCHECKED_CAST")
    suspend fun <T> execute(commandId: String, vararg args: Any?): T {
        if (!canExecute(commandId)) error("不可执行啊一个：$commandId")

        val executor = executors[commandId] ?: error("没注册，你好坏喵：$commandId")

        return executor.invoke(args) as T
    }

    // 发射执行，主要是给UI使用，在主线程工作
    fun dispatch(commandId: String, vararg args: Any?) {
        scope.launch {
            try {
                execute<Any?>(commandId, *args)
            } catch (e: Exception) {
                // HACK: 统一异常处理
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

    // CLEAR UP
    fun shutdown() = shutdownCoroutineScope()
}