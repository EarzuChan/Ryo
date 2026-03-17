package me.earzuchan.ryo.foundation.util

import java.io.ByteArrayOutputStream
import java.util.zip.DataFormatException
import java.util.zip.Deflater
import java.util.zip.Inflater

actual object CompressionUtils {
    actual fun deflate(data: ByteArray): ByteArray {
        if (data.isEmpty()) return data

        val deflater = Deflater(Deflater.DEFAULT_COMPRESSION, false)
        deflater.setInput(data)
        deflater.finish()

        val out = ByteArrayOutputStream(data.size)
        val buffer = ByteArray(8192)
        while (!deflater.finished()) {
            val count = deflater.deflate(buffer)
            out.write(buffer, 0, count)
        }
        deflater.end()

        return out.toByteArray()
    }

    actual fun inflate(data: ByteArray): ByteArray {
        if (data.isEmpty()) return data

        val inflater = Inflater(false)
        inflater.setInput(data)

        val out = ByteArrayOutputStream(data.size * 2)
        val buffer = ByteArray(8192)

        try {
            while (!inflater.finished()) {
                val count = inflater.inflate(buffer)
                if (count > 0) {
                    out.write(buffer, 0, count)
                    continue
                }

                if (inflater.needsDictionary()) throw IllegalArgumentException("Inflater requires dictionary")
                if (inflater.needsInput()) throw IllegalArgumentException("Truncated deflated stream")
            }
        } catch (e: DataFormatException) {
            throw IllegalArgumentException("Invalid deflated data", e)
        } finally {
            inflater.end()
        }

        return out.toByteArray()
    }
}