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

class OldShortcutDuty(defaultBindings: Map<String, Stroke>) {
    data class Stroke(val key: OldShortcutDuty.Key, val ctrl: Boolean = false, val alt: Boolean = false, val shift: Boolean = false, val meta: Boolean = false) {
        fun matches(event: KeyEvent): Boolean {
            if (event.type != KeyEventType.KeyDown) return false
            val eventKey = OldShortcutDuty.Key.fromSysKey(event.key) ?: return false
            return eventKey == key && event.isCtrlPressed == ctrl && event.isAltPressed == alt && event.isShiftPressed == shift && event.isMetaPressed == meta
        }

        fun displayText(): String = buildList {
            if (ctrl) add("Ctrl")
            if (alt) add("Alt")
            if (shift) add("Shift")
            if (meta) add("Meta")
            add(key.display)
        }.joinToString("+")
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
        data class Conflict(val commandId: String, val stroke: OldShortcutDuty.Stroke) : OverrideResult
        data object Accepted : OverrideResult
        data class Rejected(val conflicts: List<Conflict>) : OverrideResult
    }

    private val defaults = defaultBindings.toMap()
    private val overrides = mutableStateMapOf<String, Stroke>()

    val effectiveBindings: Map<String, Stroke>
        get() {
            val merged = mutableMapOf<String, Stroke>()
            defaults.forEach { (commandId, stroke) -> merged[commandId] = overrides[commandId] ?: stroke }
            overrides.forEach { (commandId, stroke) -> if (commandId !in merged) merged[commandId] = stroke }
            return merged.toMap()
        }

    fun effectiveStroke(commandId: String) = effectiveBindings[commandId]

    fun applyOverrideSnapshot(snapshot: Map<String, Stroke>) {
        overrides.clear()
        overrides.putAll(snapshot)
    }

    fun setOverride(commandId: String, stroke: Stroke?): OverrideResult {
        val conflicts = findConflicts(commandId, stroke)
        if (conflicts.isNotEmpty()) return OverrideResult.Rejected(conflicts)
        if (stroke == null) overrides.remove(commandId) else overrides[commandId] = stroke
        return OverrideResult.Accepted
    }

    fun clearOverride(commandId: String) {
        overrides.remove(commandId)
    }

    fun overrideSnapshot() = overrides.toMap()

    fun resolve(event: KeyEvent): String? = effectiveBindings.entries.firstOrNull { (_, stroke) -> stroke.matches(event) }?.key

    fun findConflicts(commandId: String, stroke: Stroke?): List<OverrideResult.Conflict> = if (stroke == null) emptyList()
    else effectiveBindings.entries.filter { (otherCommandId, otherStroke) -> otherCommandId != commandId && otherStroke == stroke }.map { (otherCommandId, otherStroke) -> OverrideResult.Conflict(otherCommandId, otherStroke) }
}
