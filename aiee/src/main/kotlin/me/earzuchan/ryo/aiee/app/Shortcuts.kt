package me.earzuchan.ryo.aiee.app

import androidx.compose.ui.input.key.Key as SysKey
import androidx.compose.ui.input.key.KeyEvent
import androidx.compose.ui.input.key.KeyEventType
import androidx.compose.ui.input.key.isAltPressed
import androidx.compose.ui.input.key.isCtrlPressed
import androidx.compose.ui.input.key.isMetaPressed
import androidx.compose.ui.input.key.isShiftPressed
import androidx.compose.ui.input.key.key
import androidx.compose.ui.input.key.type
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch
import me.earzuchan.ryo.aiee.data.repository.ShortcutRepository
import kotlin.collections.component1
import kotlin.collections.component2

class ShortcutService(private val shortcutRepo: ShortcutRepository): ScopedService() {
    data class Stroke(val key: Key, val ctrl: Boolean = false, val alt: Boolean = false, val shift: Boolean = false, val meta: Boolean = false) {
        fun matches(event: KeyEvent): Boolean {
            if (event.type != KeyEventType.KeyDown) return false
            val eventKey = Key.fromSysKey(event.key) ?: return false
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
        A("A"), B("B"), C("C"), D("D"), E("E"), F("F"), G("G"), H("H"), I("I"), J("J"), K("K"), L("L"), M("M"), N("N"), O("O"), P("P"), Q("Q"), R("R"), S("S"), T("T"), U("U"), V("V"), W("W"), X("X"), Y("Y"), Z("Z"), Comma(","), F1("F1"), F2("F2"), F3("F3"), F4("F4"), F5("F5"), F6(
            "F6"
        ),
        F7("F7"), F8("F8"), F9("F9"), F10("F10"), F11("F11");

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
                SysKey.F1 -> F1
                SysKey.F2 -> F2
                SysKey.F3 -> F3
                SysKey.F4 -> F4
                SysKey.F5 -> F5
                SysKey.F6 -> F6
                SysKey.F7 -> F7
                SysKey.F8 -> F8
                SysKey.F9 -> F9
                SysKey.F10 -> F10
                SysKey.F11 -> F11
                else -> null
            }
        }
    }

    sealed interface SetStrokeResult {
        data class Conflict(val commandId: String, val stroke: Stroke) : SetStrokeResult
        data object Accepted : SetStrokeResult
        data class Rejected(val conflicts: List<Conflict>) : SetStrokeResult
    }

    init {
        scope.launch {
            shortcutRepo.overridesFlow.collect {
                _overrides.value = it
            }
        }
    }

    private val _defaults = MutableStateFlow<Map<String, Stroke>>(emptyMap())
    val defaults: StateFlow<Map<String, Stroke>> = _defaults.asStateFlow()

    private val _overrides = MutableStateFlow<Map<String, Stroke>>(emptyMap())
    val overrides: StateFlow<Map<String, Stroke>> = _overrides.asStateFlow()

    val effective: Map<String, Stroke>
        get() {
            val merged = mutableMapOf<String, Stroke>()
            merged.putAll(_defaults.value)
            merged.putAll(_overrides.value)
            return merged
        }

    fun getEffective(commandId: String) = effective[commandId]

    private fun findOverrideConflicts(commandId: String, stroke: Stroke) = effective.entries.filter { (otherCmd, otherStroke) -> otherCmd != commandId && otherStroke == stroke }.map { (otherCommandId, otherStroke) -> SetStrokeResult.Conflict(otherCommandId, otherStroke) }

    fun setOverride(commandId: String, stroke: Stroke): SetStrokeResult {
        val conflicts = findOverrideConflicts(commandId, stroke)
        if (conflicts.isNotEmpty()) return SetStrokeResult.Rejected(conflicts)
        _overrides.value += (commandId to stroke)
        scope.launch { shortcutRepo.saveOverride(commandId, stroke) }
        return SetStrokeResult.Accepted
    }

    fun clearOverride(commandId: String) {
        _overrides.value -= commandId
        scope.launch { shortcutRepo.removeOverride(commandId) }
    }

    private fun findDefaultConflicts(commandId: String, stroke: Stroke) = _defaults.value.entries.filter { (otherCmd, otherStroke) -> otherCmd != commandId && otherStroke == stroke }.map { (otherCmd, otherStroke) -> SetStrokeResult.Conflict(otherCmd, otherStroke) }

    fun setDefault(commandId: String, stroke: Stroke): SetStrokeResult {
        val conflicts = findDefaultConflicts(commandId, stroke)
        if (conflicts.isNotEmpty()) return SetStrokeResult.Rejected(conflicts)
        _overrides.value += (commandId to stroke)
        return SetStrokeResult.Accepted
    }

    fun clearDefaultFor(commandId: String) {
        _overrides.value -= commandId
    }

    fun resolve(event: KeyEvent): String? = effective.entries.firstOrNull { (_, stroke) -> stroke.matches(event) }?.key
}