package me.earzuchan.ryo.modern.texture

import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.special.FragmentalImage.Encoding
import me.earzuchan.ryo.foundation.special.FragmentalImage.Fragment
import me.earzuchan.ryo.foundation.special.FragmentalImage.Level
import me.earzuchan.ryo.foundation.special.FragmentalImage.PixelFormat
import me.earzuchan.ryo.foundation.util.ImageCodec
import me.earzuchan.ryo.foundation.util.RgbImage

data class MipmapPolicy(val clipSize: Int = 512, val maxLevels: Int = 16, val preferRgb565ForOpaque: Boolean = false) {
    // TODO：未来若需更好的 mip 质量，请在此处添加 `filter`（box/bilinear/nearest），并路由到 downscaleHalf
    init {
        require(clipSize > 0) { "clipSize must be > 0" }
        require(maxLevels > 0) { "maxLevels must be > 0" }
    }
}

internal fun packToFragmentalImage(image: RgbImage, policy: MipmapPolicy = MipmapPolicy()): FragmentalImage {
    val format = selectPixelFormat(image, policy)
    val levels = ArrayList<Level>()
    var current = image
    var levelCount = 0

    while (true) {
        levels += imageToLevel(current, format, policy.clipSize)
        levelCount++
        if ((current.width == 1 && current.height == 1) || levelCount >= policy.maxLevels) break
        current = downscaleHalf(current)
    }

    return FragmentalImage(policy.clipSize, levels)
}

internal fun extractToImage(fi: FragmentalImage): RgbImage {
    val level = fi.levels.firstOrNull() ?: error("FragmentalImage has no levels")
    val assembled = composeLevelBytes(fi.clipSize, level)

    return when (level.format) {
        PixelFormat.RGBA8888 -> RgbImage(level.width, level.height, 4, assembled)
        PixelFormat.RGB888 -> RgbImage(level.width, level.height, 3, assembled)
        PixelFormat.RGB565 -> RgbImage(level.width, level.height, 3, rgb565ToRgb888(assembled))
        else -> error("Unsupported main-image format: ${level.format}")
    }
}

private fun imageToLevel(image: RgbImage, format: PixelFormat, clipSize: Int): Level {
    val levelBytes = toFormatBytes(image, format)
    val cols = clipCount(clipSize, image.width)
    val rows = clipCount(clipSize, image.height)
    val fragments = ArrayList<Fragment>(cols * rows)
    for (r in 0 until rows) for (c in 0 until cols) {
        val x = c * clipSize
        val y = r * clipSize
        val w = minOf(clipSize, image.width - x)
        val h = minOf(clipSize, image.height - y)
        fragments += Fragment(w, h, format, Encoding.RAW, extractFragment(levelBytes, image.width, x, y, w, h, format.bytesPerPixel))
    }
    return Level(image.width, image.height, format, fragments)
}

private fun composeLevelBytes(clipSize: Int, level: Level): ByteArray {
    val out = ByteArray(level.width * level.height * level.format.bytesPerPixel)
    val cols = clipCount(clipSize, level.width)
    level.fragments.forEachIndexed { idx, fragment ->
        val x = (idx % cols) * clipSize
        val y = (idx / cols) * clipSize
        val bytes = when (fragment.encoding) {
            Encoding.RAW -> fragment.bytes
            Encoding.JPEG -> toFormatBytes(ImageCodec.decode(fragment.bytes), fragment.format)
        }
        pasteFragment(out, level.width, x, y, fragment.width, fragment.height, level.format.bytesPerPixel, bytes)
    }
    return out
}

private fun extractFragment(src: ByteArray, srcWidth: Int, x: Int, y: Int, w: Int, h: Int, bpp: Int): ByteArray {
    val out = ByteArray(w * h * bpp)
    for (row in 0 until h) {
        val srcOffset = ((y + row) * srcWidth + x) * bpp
        val dstOffset = row * w * bpp
        src.copyInto(out, dstOffset, srcOffset, srcOffset + (w * bpp))
    }
    return out
}

private fun pasteFragment(dst: ByteArray, dstWidth: Int, x: Int, y: Int, w: Int, h: Int, bpp: Int, fragment: ByteArray) {
    for (row in 0 until h) {
        val dstOffset = ((y + row) * dstWidth + x) * bpp
        val srcOffset = row * w * bpp
        fragment.copyInto(dst, dstOffset, srcOffset, srcOffset + (w * bpp))
    }
}

private fun selectPixelFormat(image: RgbImage, policy: MipmapPolicy): PixelFormat {
    val hasAlpha = image.channels == 4 && !isFullyOpaque(image)
    if (hasAlpha) return PixelFormat.RGBA8888
    return if (policy.preferRgb565ForOpaque) PixelFormat.RGB565 else PixelFormat.RGB888
}

private fun isFullyOpaque(image: RgbImage): Boolean {
    if (image.channels != 4) return true
    var i = 3
    while (i < image.bytes.size) {
        if ((image.bytes[i].toInt() and 0xFF) != 0xFF) return false
        i += 4
    }
    return true
}

private fun toFormatBytes(image: RgbImage, format: PixelFormat): ByteArray = when (format) {
    PixelFormat.RGBA8888 -> when (image.channels) {
        4 -> image.bytes.copyOf()
        3 -> ByteArray(image.width * image.height * 4).also { out ->
            var si = 0
            var di = 0
            while (si < image.bytes.size) {
                out[di++] = image.bytes[si++]
                out[di++] = image.bytes[si++]
                out[di++] = image.bytes[si++]
                out[di++] = 0xFF.toByte()
            }
        }

        else -> error("Unsupported channel count: ${image.channels}")
    }

    PixelFormat.RGB888 -> when (image.channels) {
        3 -> image.bytes.copyOf()
        4 -> ByteArray(image.width * image.height * 3).also { out ->
            var si = 0
            var di = 0
            while (si < image.bytes.size) {
                out[di++] = image.bytes[si++]
                out[di++] = image.bytes[si++]
                out[di++] = image.bytes[si++]
                si++
            }
        }

        else -> error("Unsupported channel count: ${image.channels}")
    }

    PixelFormat.RGB565 -> rgb888To565(if (image.channels == 3) image.bytes else toFormatBytes(image, PixelFormat.RGB888))
    else -> error("Unsupported format conversion: $format")
}

private fun rgb888To565(rgb: ByteArray): ByteArray {
    require(rgb.size % 3 == 0) { "RGB888 byte length must be multiple of 3" }
    val out = ByteArray((rgb.size / 3) * 2)
    var si = 0
    var di = 0
    while (si < rgb.size) {
        val r = rgb[si++].toInt() and 0xFF
        val g = rgb[si++].toInt() and 0xFF
        val b = rgb[si++].toInt() and 0xFF
        val packed = ((r and 0xF8) shl 8) or ((g and 0xFC) shl 3) or ((b and 0xF8) ushr 3)
        out[di++] = ((packed ushr 8) and 0xFF).toByte()
        out[di++] = (packed and 0xFF).toByte()
    }
    return out
}

private fun rgb565ToRgb888(rgb565: ByteArray): ByteArray {
    require(rgb565.size % 2 == 0) { "RGB565 byte length must be multiple of 2" }
    val out = ByteArray((rgb565.size / 2) * 3)
    var si = 0
    var di = 0
    while (si < rgb565.size) {
        val hi = rgb565[si++].toInt() and 0xFF
        val lo = rgb565[si++].toInt() and 0xFF
        val packed = (hi shl 8) or lo
        val r5 = (packed ushr 11) and 0x1F
        val g6 = (packed ushr 5) and 0x3F
        val b5 = packed and 0x1F
        out[di++] = ((r5 * 255 + 15) / 31).toByte()
        out[di++] = ((g6 * 255 + 31) / 63).toByte()
        out[di++] = ((b5 * 255 + 15) / 31).toByte()
    }
    return out
}

private fun downscaleHalf(image: RgbImage): RgbImage {
    // TODO：当前实现类似于最近邻算法；未来或将支持策略驱动的重采样滤波器
    val nw = maxOf(1, image.width / 2)
    val nh = maxOf(1, image.height / 2)
    val channels = image.channels
    val out = ByteArray(nw * nh * channels)
    var di = 0
    for (y in 0 until nh) for (x in 0 until nw) {
        val sx = minOf(image.width - 1, x * 2)
        val sy = minOf(image.height - 1, y * 2)
        val si = (sy * image.width + sx) * channels
        for (c in 0 until channels) out[di++] = image.bytes[si + c]
    }
    return RgbImage(nw, nh, channels, out)
}

private fun clipCount(clipSize: Int, pixels: Int): Int = (pixels / clipSize) + if (pixels % clipSize != 0) 1 else 0
