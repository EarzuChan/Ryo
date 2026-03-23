package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import com.arkivanov.decompose.ComponentContext as DutyContext
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.page.EditorSessionPage
import me.earzuchan.ryo.aiee.ui.page.EmptyPage
import me.earzuchan.ryo.aiee.ui.page.SettingsPage
import me.earzuchan.ryo.aiee.ui.page.WelcomePage

abstract class TabDuty(ctx: DutyContext, val navi: WorkspaceTabNavi, initialTitle: UiText, initialDirty: Boolean = false) : DutyContext by ctx {
    data class State(val title: UiText, val dirty: Boolean)

    sealed interface Intent {
        data object Save : Intent
        data object Discard : Intent
        data object Undo : Intent
        data object Redo : Intent
    }

    sealed interface Effect {
        data object NoOp : Effect
        data object Saved : Effect
        data object Discarded : Effect
    }

    data class Tab(val id: String, val navi: WorkspaceTabNavi, val title: UiText, val dirty: Boolean = false)

    var title by mutableStateOf(initialTitle)
    var dirty by mutableStateOf(initialDirty)

    val state get() = State(title, dirty)

    open fun canUndo() = false
    open fun undo() {}
    open fun canRedo() = false
    open fun redo() {}
    open fun canSave() = false
    open fun save() {}
    open fun canDiscard() = false
    open fun discard() {}

    open fun dispatch(intent: Intent): Effect = when (intent) {
        Intent.Save -> if (canSave()) {
            save()
            Effect.Saved
        } else Effect.NoOp

        Intent.Discard -> if (canDiscard()) {
            discard()
            Effect.Discarded
        } else Effect.NoOp

        Intent.Undo -> {
            if (canUndo()) undo()
            Effect.NoOp
        }

        Intent.Redo -> {
            if (canRedo()) redo()
            Effect.NoOp
        }
    }

    @Composable
    abstract fun Render(appDuty: AppDuty)
}

class EmptyTabDuty(ctx: DutyContext) : TabDuty(ctx, WorkspaceTabNavi.Empty, UiText.Plain("__empty__")) {
    @Composable
    override fun Render(appDuty: AppDuty) = EmptyPage()
}

class WelcomeTabDuty(ctx: DutyContext, title: UiText) : TabDuty(ctx, WorkspaceTabNavi.Welcome, title) {
    @Composable
    override fun Render(appDuty: AppDuty) = WelcomePage()
}

class SettingsTabDuty(ctx: DutyContext, title: UiText) : TabDuty(ctx, WorkspaceTabNavi.Settings, title) {
    @Composable
    override fun Render(appDuty: AppDuty) = SettingsPage(appDuty)
}

class EditorSessionTabDuty(ctx: DutyContext, navi: WorkspaceTabNavi.EditorSession, title: UiText, initialDirty: Boolean = false) : TabDuty(ctx, navi, title, initialDirty) {
    override fun canSave() = dirty

    override fun save() {
        dirty = false
    }

    override fun canDiscard() = dirty

    override fun discard() {
        dirty = false
    }

    @Composable
    override fun Render(appDuty: AppDuty) = EditorSessionPage()
}
