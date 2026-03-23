package me.earzuchan.ryo.aiee.ui

import androidx.compose.runtime.Composable
import me.earzuchan.ryo.aiee.util.ResUtils.text
import org.jetbrains.compose.resources.StringResource

sealed interface UiText {
    data class Plain(val value: String) : UiText

    data class Res(val resource: StringResource, val args: List<UiText> = emptyList()) : UiText
}

@Composable
fun UiText.resolve(): String = when (this) {
    is UiText.Plain -> value

    is UiText.Res -> {
        val resolvedArgs: List<String> = args.map { arg ->
            when (arg) {
                is UiText.Plain -> arg.value
                is UiText.Res -> arg.resolve()
            }
        }

        resource.text(*resolvedArgs.toTypedArray())
    }
}
