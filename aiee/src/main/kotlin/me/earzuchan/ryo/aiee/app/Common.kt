package me.earzuchan.ryo.aiee.app

import kotlinx.coroutines.CoroutineScope
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.SupervisorJob

abstract class ScopedService {
   protected val scope = CoroutineScope(SupervisorJob() + Dispatchers.Default)
}