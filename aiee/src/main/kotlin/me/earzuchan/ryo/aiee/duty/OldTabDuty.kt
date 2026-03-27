package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import me.earzuchan.ryo.aiee.app.AppService
import me.earzuchan.ryo.aiee.app.DialogService
import me.earzuchan.ryo.aiee.app.MenuService
import com.arkivanov.decompose.ComponentContext as DutyContext
import me.earzuchan.ryo.aiee.ui.UiText
import me.earzuchan.ryo.aiee.ui.page.EditorSessionPage
import me.earzuchan.ryo.aiee.ui.page.EmptyPage
import me.earzuchan.ryo.aiee.ui.page.SettingsPage
import me.earzuchan.ryo.aiee.ui.page.WelcomePage
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject

abstract class OldTabDuty(ctx: DutyContext, val navi: WorkspaceTabNavis, initialTitle: UiText, initialDirty: Boolean = false) : DutyContext by ctx {
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

    data class Tab(val id: String, val navi: WorkspaceTabNavis, val title: UiText, val dirty: Boolean = false)

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
    abstract fun Render()
}

class EmptyOldTabDuty(ctx: DutyContext) : OldTabDuty(ctx, WorkspaceTabNavis.Empty, UiText.Plain("__empty__")) {
    @Composable
    override fun Render() = EmptyPage()
}

class WelcomeOldTabDuty(ctx: DutyContext, title: UiText) : OldTabDuty(ctx, WorkspaceTabNavis.Welcome, title) {
    @Composable
    override fun Render() = WelcomePage()
}

class SettingsOldTabDuty(ctx: DutyContext, title: UiText) : OldTabDuty(ctx, WorkspaceTabNavis.Settings, title){
    @Composable
    override fun Render() = SettingsPage()
}

class EditorSessionOldTabDuty(ctx: DutyContext, navi: WorkspaceTabNavis.EditorSession, title: UiText, initialDirty: Boolean = false) : OldTabDuty(ctx, navi, title, initialDirty) {
    override fun canSave() = dirty

    override fun save() {
        dirty = false
    }

    override fun canDiscard() = dirty

    override fun discard() {
        dirty = false
    }

    @Composable
    override fun Render() = EditorSessionPage()
}
