package me.earzuchan.ryo.aiee.duty

typealias CommandHandler = () -> Unit

class CommandDuty {
    data class Availability(val enabled: Boolean, val reason: String? = null)

    private data class Binding(val availability: () -> Availability, val execute: CommandHandler)

    private val bindings = mutableMapOf<AppCommand, Binding>()

    fun register(command: AppCommand, canExecute: () -> Boolean = { true }, execute: CommandHandler) = registerWithAvailability(command, { Availability(canExecute()) }, execute)

    fun registerWithAvailability(command: AppCommand, availability: () -> Availability, execute: CommandHandler) {
        bindings[command] = Binding(availability, execute)
    }

    fun availability(command: AppCommand) = bindings[command]?.availability?.invoke() ?: Availability(enabled = false, reason = "命令未注册")

    fun canExecute(command: AppCommand) = availability(command).enabled

    fun execute(command: AppCommand) {
        val binding = bindings[command] ?: return
        if (!binding.availability().enabled) return
        binding.execute()
    }
}
