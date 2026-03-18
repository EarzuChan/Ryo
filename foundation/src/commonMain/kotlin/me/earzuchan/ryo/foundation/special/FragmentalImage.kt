package me.earzuchan.ryo.foundation.special

import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.value.RyoSpecialValue
import me.earzuchan.ryo.foundation.value.RyoValue

data class FragmentalImage(val clipSize: Int, val levels: List<Level>) {
    init {
        require(clipSize > 0) { "clipSize must be > 0" }
        require(levels.isNotEmpty()) { "FragmentalImage must have at least 1 level" }
    }

    data class Level(val width: Int, val height: Int, val format: PixelFormat, val fragments: List<Fragment>) {
        init {
            require(width > 0) { "Level.width must be > 0" }
            require(height > 0) { "Level.height must be > 0" }
            require(fragments.isNotEmpty()) { "Level.fragments must not be empty" }
        }
    }

    @Suppress("ArrayInDataClass")
    data class Fragment(val width: Int, val height: Int, val format: PixelFormat, val encoding: Encoding, val bytes: ByteArray) {
        init {
            require(width > 0) { "Fragment.width must be > 0" }
            require(height > 0) { "Fragment.height must be > 0" }
            require(bytes.isNotEmpty()) { "Fragment.bytes must not be empty" }
        }
    }

    enum class Encoding { RAW, JPEG }

    enum class PixelFormat(val bytesPerPixel: Int) {
        Alpha(1),
        Intensity(1),
        LuminanceAlpha(2),
        RGB565(2),
        RGBA4444(2),
        RGB888(3),
        RGBA8888(4);

        val jpegFriendly: Boolean get() = this == RGB565 || this == RGB888

        companion object {
            fun fromOrdinal(ordinal: Int): PixelFormat = entries.getOrNull(ordinal) ?: error("Unsupported Pixmap.Format ordinal: $ordinal")
        }
    }

    companion object {
        const val WIRE_TYPE_ID = "sengine.graphics2d.texturefile.FIFormat\$FragmentedImageData"

        const val SPECIAL_KIND = "fragmental-image"

        val TYPE_REF = TypeRefs.objectType(WIRE_TYPE_ID)

        fun supportsWireTypeId(wireTypeId: String): Boolean = wireTypeId == WIRE_TYPE_ID
    }
}

fun FragmentalImage.specialValue() = RyoSpecialValue(FragmentalImage.TYPE_REF, FragmentalImage.SPECIAL_KIND, this)

fun RyoValue?.fragmentalImageOrNull(): FragmentalImage? {
    val special = this as? RyoSpecialValue<*> ?: return null
    if (special.kind != FragmentalImage.SPECIAL_KIND || !FragmentalImage.supportsWireTypeId(special.wireTypeId)) return null
    return special.payload as? FragmentalImage
}

fun RyoValue?.requireFragmentalImage(): FragmentalImage = fragmentalImageOrNull() ?: error("Value is not FragmentalImage special value")