package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.setValue
import me.earzuchan.ryo.aiee.ui.component.ButtonType
import me.earzuchan.ryo.aiee.ui.UiText
import org.jetbrains.compose.resources.DrawableResource

class DialogDuty {
    data class DialogAction(
        val text: UiText, val type: ButtonType = ButtonType.Text, val enabled: Boolean = true, val onClick: (() -> Boolean?)? = null
    )

    sealed interface Model {
        val showOverlay: Boolean
        val closeOnOverlayClick: Boolean
        val onOpen: (() -> Unit)?
        val onOpened: (() -> Unit)?
        val onClose: (() -> Unit)?
        val onClosed: (() -> Unit)?

        data class Common(
            val icon: DrawableResource? = null,
            val title: UiText? = null,
            val description: UiText? = null,
            val actions: List<DialogAction> = emptyList(),
            val content: (@Composable () -> Unit)? = null,
            override val showOverlay: Boolean = true,
            override val closeOnOverlayClick: Boolean = false,
            override val onOpen: (() -> Unit)? = null,
            override val onOpened: (() -> Unit)? = null,
            override val onClose: (() -> Unit)? = null,
            override val onClosed: (() -> Unit)? = null
        ) : Model

        data class Special(
            val content: @Composable (DialogController) -> Unit,
            override val showOverlay: Boolean = true,
            override val closeOnOverlayClick: Boolean = false,
            override val onOpen: (() -> Unit)? = null,
            override val onOpened: (() -> Unit)? = null,
            override val onClose: (() -> Unit)? = null,
            override val onClosed: (() -> Unit)? = null
        ) : Model
    }

    class DialogController internal constructor(private val requestClose: () -> Unit) {
        fun close() = requestClose()
    }

    private data class DialogTicket(val id: Long, val model: Model)

    private val queue = ArrayDeque<DialogTicket>()
    private var currentTicket by mutableStateOf<DialogTicket?>(null)
    private var nextTicketId = 1L
    private var currentVisible by mutableStateOf(false)
    private val currentController = DialogController(::requestCloseCurrent)

    val currentDialogId: Long? get() = currentTicket?.id
    val currentDialog: Model? get() = currentTicket?.model
    val ctrlShow: Boolean get() = currentVisible
    val dialogController: DialogController get() = currentController

    private fun order(model: Model) {
        val ticket = DialogTicket(nextTicketId++, model)
        if (currentTicket == null) show(ticket) else queue += ticket
    }

    fun orderCommon(
        icon: DrawableResource? = null,
        headline: UiText? = null,
        description: UiText? = null,
        actions: List<DialogAction> = emptyList(),
        content: (@Composable () -> Unit)? = null,
        showOverlay: Boolean = true,
        closeOnOverlayClick: Boolean = false,
        onOpen: (() -> Unit)? = null,
        onOpened: (() -> Unit)? = null,
        onClose: (() -> Unit)? = null,
        onClosed: (() -> Unit)? = null
    ) = order(Model.Common(icon, headline, description, actions, content, showOverlay, closeOnOverlayClick, onOpen, onOpened, onClose, onClosed))

    fun orderSpecial(
        showOverlay: Boolean = true,
        closeOnOverlayClick: Boolean = false,
        onOpen: (() -> Unit)? = null,
        onOpened: (() -> Unit)? = null,
        onClose: (() -> Unit)? = null,
        onClosed: (() -> Unit)? = null,
        content: @Composable (DialogController) -> Unit
    ) = order(Model.Special(content, showOverlay, closeOnOverlayClick, onOpen, onOpened, onClose, onClosed))

    fun clickOverlayCurrent() {
        val model = currentDialog ?: return

        val canClose = when (model) {
            is Model.Common -> model.closeOnOverlayClick || model.actions.isEmpty()
            is Model.Special -> model.closeOnOverlayClick
        }

        if (canClose) requestCloseCurrent()
    }

    fun clickActionCurrent(action: DialogAction) {
        if (!action.enabled) return

        if (action.onClick?.invoke() != false) requestCloseCurrent()
    }

    fun notifyCurrentOpened() {
        currentDialog?.onOpened?.invoke()
    }

    fun requestCloseCurrent() {
        if (!currentVisible) return
        currentDialog?.onClose?.invoke()
        currentVisible = false
    }

    fun notifyCurrentClosed() {
        val closingTicket = currentTicket ?: return
        if (currentVisible) return
        closingTicket.model.onClosed?.invoke()
        currentTicket = if (queue.isEmpty()) null else queue.removeFirst()
        currentTicket?.also(::show)
    }

    private fun show(ticket: DialogTicket) {
        currentTicket = ticket
        currentVisible = true
        ticket.model.onOpen?.invoke()
    }
}
