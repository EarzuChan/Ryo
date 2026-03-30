package me.earzuchan.ryo.aiee.duty

import androidx.compose.runtime.Composable
import com.arkivanov.decompose.ComponentContext as DutyContext
import me.earzuchan.ryo.aiee.ui.page.EditorSessionPage
import me.earzuchan.ryo.aiee.ui.page.EmptyPage
import me.earzuchan.ryo.aiee.ui.page.SettingsPage
import me.earzuchan.ryo.aiee.ui.page.WelcomePage
import androidx.compose.runtime.State
import androidx.compose.runtime.mutableStateOf
import me.earzuchan.ryo.aiee.resources.Res
import me.earzuchan.ryo.aiee.resources.settings
import me.earzuchan.ryo.aiee.resources.welcome
import me.earzuchan.ryo.aiee.ui.UiText

abstract class TabDuty(ctx: DutyContext) : DutyContext by ctx {
    abstract val title: State<UiText>
    abstract val isDirty: State<Boolean>

    @Composable
    abstract fun Render() // 前端调这个来渲染
}

class EmptyTabDuty(ctx: DutyContext) : TabDuty(ctx) {
    override val title = mutableStateOf(UiText.Plain(""))
    override val isDirty = mutableStateOf(false)

    @Composable
    override fun Render() = EmptyPage()
}

class WelcomeTabDuty(ctx: DutyContext) : TabDuty(ctx) {
    override val title = mutableStateOf(UiText.Res(Res.string.welcome))
    override val isDirty = mutableStateOf(false)

    @Composable
    override fun Render() = WelcomePage()
}

class SettingsTabDuty(ctx: DutyContext) : TabDuty(ctx) {
    override val title = mutableStateOf(UiText.Res(Res.string.settings))
    override val isDirty = mutableStateOf(false)

    @Composable
    override fun Render() = SettingsPage()
}

class EditorSessionTabDuty(ctx: DutyContext, navi: MainPanelTabNavis.EditorSession, makeResidential: () -> Unit) : TabDuty(ctx) {
    override val title = mutableStateOf(UiText.Plain("啊一个"))
    override val isDirty = mutableStateOf(false)

    @Composable
    override fun Render() = EditorSessionPage()

    // 其它具体的没写
}
