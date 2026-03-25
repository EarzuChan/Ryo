package me.earzuchan.ryo.aiee.duty

typealias CommandHandler = () -> Unit

class CommandDuty {
    data class Availability(val enabled: Boolean, val reason: String? = null)
    data class Registration(val command: AppCommand, val availability: () -> Availability, val execute: CommandHandler, val defaultShortcut: ShortcutDuty.Stroke? = null)

    private data class Binding(val availability: () -> Availability, val execute: CommandHandler, val defaultShortcut: ShortcutDuty.Stroke?)

    private val bindings = mutableMapOf<AppCommand, Binding>()

    fun register(registration: Registration) {
        bindings[registration.command] = Binding(registration.availability, registration.execute, registration.defaultShortcut)
    }

    fun registerAll(registrations: Iterable<Registration>) = registrations.forEach(::register)

    fun register(command: AppCommand, canExecute: () -> Boolean = { true }, defaultShortcut: ShortcutDuty.Stroke? = null, execute: CommandHandler) =
        registerWithAvailability(command, { Availability(canExecute()) }, defaultShortcut, execute)

    fun registerWithAvailability(command: AppCommand, availability: () -> Availability, defaultShortcut: ShortcutDuty.Stroke? = null, execute: CommandHandler) {
        bindings[command] = Binding(availability, execute, defaultShortcut)
    }

    fun isRegistered(command: AppCommand): Boolean = command in bindings

    fun registeredCommands(): Set<AppCommand> = bindings.keys.toSet()

    fun missingCommands(): Set<AppCommand> = AppCommand.entries.toSet() - bindings.keys

    fun defaultShortcutOf(command: AppCommand): ShortcutDuty.Stroke? = bindings[command]?.defaultShortcut

    fun defaultShortcutsSnapshot(): Map<AppCommand, ShortcutDuty.Stroke> = bindings.mapNotNull { (command, binding) ->
        binding.defaultShortcut?.let { command to it }
    }.toMap()

    fun availability(command: AppCommand) = bindings[command]?.availability?.invoke() ?: Availability(enabled = false)

    fun canExecute(command: AppCommand) = availability(command).enabled

    fun execute(command: AppCommand) {
        val binding = bindings[command] ?: return
        if (!binding.availability().enabled) return
        binding.execute()
    }
}
