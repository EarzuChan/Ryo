package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.mutableStateMapOf
import androidx.compose.ui.input.key.KeyEvent
import androidx.compose.ui.input.key.KeyEventType
import androidx.compose.ui.input.key.isAltPressed
import androidx.compose.ui.input.key.isCtrlPressed
import androidx.compose.ui.input.key.isMetaPressed
import androidx.compose.ui.input.key.isShiftPressed
import androidx.compose.ui.input.key.key
import androidx.compose.ui.input.key.type
import androidx.compose.ui.input.key.Key as SysKey

class ShortcutDuty(defaultBindings: Map<AppCommand, Stroke>) {
    // 一套快捷键
    data class Stroke(val key: ShortcutDuty.Key, val ctrl: Boolean = false, val alt: Boolean = false, val shift: Boolean = false, val meta: Boolean = false) {
        fun matches(event: KeyEvent): Boolean {
            if (event.type != KeyEventType.KeyDown) return false
            val eventKey = ShortcutDuty.Key.fromSysKey(event.key) ?: return false
            return eventKey == key && event.isCtrlPressed == ctrl && event.isAltPressed == alt && event.isShiftPressed == shift && event.isMetaPressed == meta
        }

        fun displayText(): String {
            val chunks = buildList {
                if (ctrl) add("Ctrl")
                if (alt) add("Alt")
                if (shift) add("Shift")
                if (meta) add("Meta")
                add(key.display)
            }

            return chunks.joinToString("+")
        }
    }

    enum class Key(val display: String) {
        A("A"),
        B("B"),
        C("C"),
        D("D"),
        E("E"),
        F("F"),
        G("G"),
        H("H"),
        I("I"),
        J("J"),
        K("K"),
        L("L"),
        M("M"),
        N("N"),
        O("O"),
        P("P"),
        Q("Q"),
        R("R"),
        S("S"),
        T("T"),
        U("U"),
        V("V"),
        W("W"),
        X("X"),
        Y("Y"),
        Z("Z"),
        Comma(","),
        F11("F11");

        companion object {
            fun fromSysKey(key: SysKey) = when (key) {
                SysKey.A -> A
                SysKey.B -> B
                SysKey.C -> C
                SysKey.D -> D
                SysKey.E -> E
                SysKey.F -> F
                SysKey.G -> G
                SysKey.H -> H
                SysKey.I -> I
                SysKey.J -> J
                SysKey.K -> K
                SysKey.L -> L
                SysKey.M -> M
                SysKey.N -> N
                SysKey.O -> O
                SysKey.P -> P
                SysKey.Q -> Q
                SysKey.R -> R
                SysKey.S -> S
                SysKey.T -> T
                SysKey.U -> U
                SysKey.V -> V
                SysKey.W -> W
                SysKey.X -> X
                SysKey.Y -> Y
                SysKey.Z -> Z
                SysKey.Comma -> Comma
                SysKey.F11 -> F11
                else -> null
            }
        }
    }

    sealed interface OverrideResult {
        data class Conflict(val command: AppCommand, val stroke: ShortcutDuty.Stroke)

        object Accepted : OverrideResult
        data class Rejected(val conflicts: List<Conflict>) : OverrideResult
    }

    private val defaults = defaultBindings.toMap()
    private val overrides = mutableStateMapOf<AppCommand, Stroke>()

    val effectiveBindings: Map<AppCommand, Stroke>
        get() {
            val merged = mutableMapOf<AppCommand, Stroke>()
            defaults.forEach { (command, stroke) -> merged[command] = overrides[command] ?: stroke }
            overrides.forEach { (command, stroke) -> if (command !in merged) merged[command] = stroke }
            return merged.toMap()
        }

    fun effectiveStroke(command: AppCommand) = effectiveBindings[command]

    fun commandDisplay(command: AppCommand) = effectiveStroke(command)?.displayText()

    fun applyOverrideSnapshot(snapshot: Map<AppCommand, Stroke>) {
        overrides.clear()
        overrides.putAll(snapshot)
    }

    fun overrideOf(command: AppCommand): Stroke? = overrides[command]

    fun setOverride(command: AppCommand, stroke: Stroke?): OverrideResult {
        val conflicts = findConflicts(command, stroke)
        if (conflicts.isNotEmpty()) return OverrideResult.Rejected(conflicts)
        if (stroke == null) overrides.remove(command) else overrides[command] = stroke
        return OverrideResult.Accepted
    }

    fun clearOverride(command: AppCommand) {
        overrides.remove(command)
    }

    fun overrideSnapshot() = overrides.toMap()

    fun resolve(event: KeyEvent): AppCommand? = effectiveBindings.entries.firstOrNull { (_, stroke) -> stroke.matches(event) }?.key

    fun findConflicts(command: AppCommand, stroke: Stroke?): List<OverrideResult.Conflict> = if (stroke == null) emptyList()
    else effectiveBindings.entries.filter { (otherCommand, otherStroke) -> otherCommand != command && otherStroke == stroke }.map { (otherCmd, otherStroke) -> OverrideResult.Conflict(otherCmd, otherStroke) }
}
