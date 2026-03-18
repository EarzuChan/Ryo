package me.earzuchan.ryo.foundation.glory

import me.earzuchan.ryo.foundation.chamber.Chamber
import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.special.FragmentalImage.Encoding
import me.earzuchan.ryo.foundation.special.FragmentalImage.PixelFormat
import me.earzuchan.ryo.foundation.special.specialValue
import me.earzuchan.ryo.foundation.value.RyoSpecialValue
import me.earzuchan.ryo.foundation.value.RyoValue

internal class SpecialGlories : Glory {
    override val id: String = GloryIds.FI

    override fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue {
        require(FragmentalImage.supportsWireTypeId(declaredWireTypeId)) { "Declared wire mismatch: $declaredWireTypeId is not FI wire type" }

        val clipSize = ctx.reader.readInt()
        val levelCount = ctx.reader.readInt()
        val levels = ArrayList<FragmentalImage.Level>(levelCount)

        repeat(levelCount) {
            val width = ctx.reader.readInt()
            val height = ctx.reader.readInt()
            val format = PixelFormat.fromOrdinal(ctx.reader.readUnsignedByte())
            val cols = clipCount(clipSize, width)
            val rows = clipCount(clipSize, height)
            val fragCount = cols * rows
            val fragments = ArrayList<FragmentalImage.Fragment>(fragCount)

            repeat(fragCount) { idx ->
                val fragWidth = fragmentWidth(idx, cols, clipSize, width)
                val fragHeight = fragmentHeight(idx, cols, clipSize, height)
                if (format.jpegFriendly) {
                    val compressedSize = ctx.reader.readInt()
                    if (compressedSize > 0) {
                        val jpegBytes = ctx.reader.readBytes(compressedSize)
                        fragments += FragmentalImage.Fragment(fragWidth, fragHeight, format, Encoding.JPEG, jpegBytes)
                        return@repeat
                    }
                }

                val rawBytes = ctx.reader.readBytes(fragWidth * fragHeight * format.bytesPerPixel)
                fragments += FragmentalImage.Fragment(fragWidth, fragHeight, format, Encoding.RAW, rawBytes)
            }
            levels += FragmentalImage.Level(width, height, format, fragments)
        }
        return FragmentalImage(clipSize, levels).specialValue()
    }

    override fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String) {
        require(FragmentalImage.supportsWireTypeId(declaredWireTypeId)) { "Declared wire mismatch: $declaredWireTypeId is not FI wire type" }

        val special = value as? RyoSpecialValue<*> ?: error("Expect RyoSpecialValue for ${FragmentalImage.WIRE_TYPE_ID}")
        require(special.kind == FragmentalImage.SPECIAL_KIND) { "Special kind mismatch: ${special.kind}" }
        val image = special.payload as? FragmentalImage ?: error("Special payload is not FragmentalImage")
        ctx.writer.writeInt(image.clipSize)
        ctx.writer.writeInt(image.levels.size)

        image.levels.forEach { level ->
            ctx.writer.writeInt(level.width)
            ctx.writer.writeInt(level.height)
            ctx.writer.writeByte(level.format.ordinal.toByte())
            val cols = clipCount(image.clipSize, level.width)
            val rows = clipCount(image.clipSize, level.height)
            val expectedCount = cols * rows
            require(level.fragments.size == expectedCount) { "Fragment count mismatch. expected=$expectedCount actual=${level.fragments.size}" }
            level.fragments.forEachIndexed { idx, frag ->
                val expectedWidth = fragmentWidth(idx, cols, image.clipSize, level.width)
                val expectedHeight = fragmentHeight(idx, cols, image.clipSize, level.height)
                require(frag.format == level.format) { "Fragment format mismatch at level fragment[$idx]" }
                require(frag.width == expectedWidth && frag.height == expectedHeight) { "Fragment size mismatch at [$idx]. expected=${expectedWidth}x${expectedHeight} actual=${frag.width}x${frag.height}" }

                if (level.format.jpegFriendly) {
                    if (frag.encoding == Encoding.JPEG) {
                        ctx.writer.writeInt(frag.bytes.size)
                        ctx.writer.writeBytes(frag.bytes)
                        return@forEachIndexed
                    }
                    ctx.writer.writeInt(-1)
                } else require(frag.encoding == Encoding.RAW) { "Non JPEG-friendly format only supports RAW. format=${level.format}" }

                require(frag.bytes.size == frag.width * frag.height * level.format.bytesPerPixel) { "Raw fragment byte length mismatch at [$idx]" }
                ctx.writer.writeBytes(frag.bytes)
            }
        }
    }

    private fun clipCount(clipSize: Int, pixels: Int): Int = (pixels / clipSize) + if (pixels % clipSize != 0) 1 else 0

    private fun fragmentWidth(idx: Int, cols: Int, clipSize: Int, levelWidth: Int): Int = minOf(levelWidth - ((idx % cols) * clipSize), clipSize)

    private fun fragmentHeight(idx: Int, cols: Int, clipSize: Int, levelHeight: Int): Int = minOf(levelHeight - ((idx / cols) * clipSize), clipSize)
}
