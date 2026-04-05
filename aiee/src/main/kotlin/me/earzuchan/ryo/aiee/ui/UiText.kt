package me.earzuchan.ryo.aiee.ui

import androidx.compose.runtime.Composable
import me.earzuchan.ryo.aiee.util.UiUtils.text
import org.jetbrains.compose.resources.StringResource

sealed interface UiText {
    data class Plain(val value: String) : UiText

    data class Res(val resource: StringResource, val args: List<UiText> = emptyList()) : UiText

    // 现在仅有Res支持嵌入Sub Texts，Plain则不支持，也不支持文本组合（“+”）
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
