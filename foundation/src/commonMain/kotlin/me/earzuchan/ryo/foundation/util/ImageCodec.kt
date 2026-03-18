package me.earzuchan.ryo.foundation.util

enum class ImageFormat { PNG, JPEG }

@Suppress("ArrayInDataClass")
data class RgbImage(val width: Int, val height: Int, val channels: Int, val bytes: ByteArray) {
    init {
        require(width > 0) { "width must be > 0" }
        require(height > 0) { "height must be > 0" }
        require(channels == 3 || channels == 4) { "channels must be 3(RGB) or 4(RGBA)" }
        require(bytes.size == width * height * channels) { "bytes length mismatch. expected=${width * height * channels} actual=${bytes.size}" }
    }
}

expect object ImageCodec {
    fun decode(encoded: ByteArray): RgbImage
    fun encode(image: RgbImage, format: ImageFormat, jpegQuality: Float = 0.92f): ByteArray
}
