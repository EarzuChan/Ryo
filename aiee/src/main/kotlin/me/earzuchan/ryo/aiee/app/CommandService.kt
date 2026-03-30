package me.earzuchan.ryo.aiee.app

import androidx.compose.ui.input.key.KeyEvent
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.util.CoroutineObject

class CommandService(private val shortcutService: ShortcutService) : CoroutineObject() {
    private val executors = mutableMapOf<String, suspend () -> Unit>()

    private val validators = mutableMapOf<String, () -> Boolean>()

    // 注册业务逻辑闭包
    fun register(commandId: String, defaultShortcut: ShortcutService.Stroke? = null, isEnabled: () -> Boolean = { true }, execute: suspend () -> Unit) {
        executors[commandId] = execute
        validators[commandId] = isEnabled
        defaultShortcut?.let { shortcutService.setDefault(commandId, it) }
    }

    fun canExecute(commandId: String) = validators[commandId]?.invoke() ?: false

    suspend fun execute(commandId: String) = if (canExecute(commandId)) executeInternal(commandId) else error("目前不可执行$commandId")

    fun dispatch(commandId: String) {
        scope.launch { execute(commandId) }
    }

    private suspend fun executeInternal(commandId: String) = executors[commandId]?.invoke()

    // 供 Compose Window 拦截 KeyEvent
    fun handleKeyEvent(event: KeyEvent): Boolean {
        val commandId = shortcutService.resolve(event) ?: return false

        if (!canExecute(commandId)) return false
        scope.launch { executeInternal(commandId) }
        return true
    }
}