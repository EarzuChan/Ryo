package me.earzuchan.ryo.aiee.duty

import me.earzuchan.ryo.aiee.app.AppService
import org.koin.core.component.KoinComponent
import org.koin.core.component.inject
import com.arkivanov.decompose.ComponentContext as DutyContext

class AppDuty(ctx: DutyContext, private val exitApp: () -> Unit) : DutyContext by ctx, KoinComponent {
}