package me.earzuchan.ryo.aiee.util

object RyoLog {
    fun d(tag: String, vararg messages: Any?) = printLog("D", tag, messages)

    fun i(tag: String, vararg messages: Any?) = printLog("I", tag, messages)

    fun w(tag: String, vararg messages: Any?) = printLog("W", tag, messages)

    fun e(tag: String, vararg messages: Any?) = if (messages.isNotEmpty() && messages.last() is Throwable) {
        val throwable = messages.last() as Throwable

        // 拼接除了最后一个 Throwable 之外的所有信息
        println("[E] $tag > ${messages.dropLast(1).joinToString(" ") { it?.toString() ?: "null" }}")

        throwable.printStackTrace()  // 输出详细堆栈信息
    } else printLog("E", tag, messages)

    private fun printLog(level: String, tag: String, messages: Array<out Any?>) = println("[$level] $tag > ${messages.joinToString(" ") { it?.toString() ?: "null" }}")
}