package me.earzuchan.ryo.foundation.util

import java.awt.image.BufferedImage
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import javax.imageio.IIOImage
import javax.imageio.ImageIO
import javax.imageio.ImageWriteParam

actual object ImageCodec {
    actual fun decode(encoded: ByteArray): RgbImage {
        val source = ImageIO.read(ByteArrayInputStream(encoded)) ?: error("Unsupported image format for decode")
        val width = source.width
        val height = source.height
        val channels = if (source.colorModel.hasAlpha()) 4 else 3
        val out = ByteArray(width * height * channels)
        var ptr = 0

        for (y in 0 until height) for (x in 0 until width) {
            val argb = source.getRGB(x, y)
            out[ptr++] = ((argb ushr 16) and 0xFF).toByte()
            out[ptr++] = ((argb ushr 8) and 0xFF).toByte()
            out[ptr++] = (argb and 0xFF).toByte()
            if (channels == 4) out[ptr++] = ((argb ushr 24) and 0xFF).toByte()
        }

        return RgbImage(width, height, channels, out)
    }

    actual fun encode(image: RgbImage, format: ImageFormat, jpegQuality: Float): ByteArray {
        require(jpegQuality in 0f..1f) { "jpegQuality must be within [0,1]" }

        val buffered = toBufferedImage(image, withAlpha = format == ImageFormat.PNG && image.channels == 4)
        val out = ByteArrayOutputStream()

        if (format == ImageFormat.PNG) {
            check(ImageIO.write(buffered, "png", out)) { "No ImageIO PNG writer found" }
            return out.toByteArray()
        }

        val writer = ImageIO.getImageWritersByFormatName("jpeg").asSequence().firstOrNull() ?: error("No ImageIO JPEG writer found")
        val ios = ImageIO.createImageOutputStream(out)
        writer.output = ios
        val params = writer.defaultWriteParam

        if (params.canWriteCompressed()) {
            params.compressionMode = ImageWriteParam.MODE_EXPLICIT
            params.compressionQuality = jpegQuality
        }

        writer.write(null, IIOImage(buffered, null, null), params)
        ios.close()
        writer.dispose()
        return out.toByteArray()
    }

    private fun toBufferedImage(image: RgbImage, withAlpha: Boolean): BufferedImage {
        val type = if (withAlpha) BufferedImage.TYPE_INT_ARGB else BufferedImage.TYPE_INT_RGB
        val buffered = BufferedImage(image.width, image.height, type)
        var ptr = 0

        for (y in 0 until image.height) for (x in 0 until image.width) {
            val r = image.bytes[ptr++].toInt() and 0xFF
            val g = image.bytes[ptr++].toInt() and 0xFF
            val b = image.bytes[ptr++].toInt() and 0xFF
            val a = if (image.channels == 4) image.bytes[ptr++].toInt() and 0xFF else 0xFF
            buffered.setRGB(x, y, (a shl 24) or (r shl 16) or (g shl 8) or b)
        }

        return buffered
    }
}
