package me.earzuchan.ryo.foundation.util

expect object CompressionUtils {
    fun deflate(data: ByteArray): ByteArray
    fun inflate(data: ByteArray): ByteArray
}