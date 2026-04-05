package me.earzuchan.ryo.aiee.duty

import com.arkivanov.decompose.ComponentContext as DutyContext
import com.arkivanov.essenty.lifecycle.Lifecycle
import com.arkivanov.essenty.lifecycle.doOnDestroy
import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import me.earzuchan.ryo.aiee.app.DialogService
import me.earzuchan.ryo.aiee.app.MenuService
import me.earzuchan.ryo.aiee.util.CoroutineScopeOwner
import me.earzuchan.ryo.aiee.util.Disposable


abstract class EmpoweredDuty(ctx: DutyContext) : DutyContext by ctx, CoroutineScopeOwner(Dispatchers.Main.immediate) {
    private val disposables = mutableListOf<Disposable>()

    init {
        lifecycle.doOnDestroy(::dispose)
    }

    // 这个会被自动调用，非必要别主动调用
    protected fun dispose() {
        disposables.forEach { it.dispose() }
        shutdownCoroutineScope()
    }

    protected fun Disposable.autoDispose() {
        disposables += this
    }
}

interface WindowDutyScope {
    val menuService: MenuService
    val dialogService: DialogService
}

// TIPS：最高指示，把Duty视为主线程对象，Vamos！