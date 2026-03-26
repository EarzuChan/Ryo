package me.earzuchan.ryo.aiee.util

import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import me.earzuchan.ryo.aiee.ui.LocalAppLanguage
import org.jetbrains.compose.resources.DrawableResource
import org.jetbrains.compose.resources.StringResource
import org.jetbrains.compose.resources.imageResource
import org.jetbrains.compose.resources.painterResource
import org.jetbrains.compose.resources.stringResource
import org.jetbrains.compose.resources.vectorResource

object UiUtils {
    @Composable
    inline fun Modifier.only(condition: Boolean, elseBlock: @Composable Modifier.() -> Modifier = { this }, ifBlock: @Composable Modifier.() -> Modifier): Modifier = if (condition) ifBlock() else elseBlock()

    // For XML vector img
    val DrawableResource.vector @Composable get() = vectorResource(this)

    // Only for bitmap, maybe a bit useless?
    val DrawableResource.image @Composable get() = imageResource(this)

    // For both vector img and bitmap img
    val DrawableResource.paint @Composable get() = painterResource(this)

    @Composable
    fun StringResource.text(vararg format: String): String {
        LocalAppLanguage.current // For trigger the StrLang refresh
        return stringResource(this, *format)
    }

    val StringResource.text @Composable get() = this.text()
}
