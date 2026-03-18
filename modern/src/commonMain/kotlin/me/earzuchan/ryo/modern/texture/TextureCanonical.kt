package me.earzuchan.ryo.modern.texture

import me.earzuchan.ryo.foundation.util.RgbImage
import me.earzuchan.ryo.foundation.special.fragmentalImageOrNull
import me.earzuchan.ryo.modern.ModernizedRyo

fun ManagedTexture.isCanonical(): Boolean = groupCount == 1 && getGroup(0).size == 1 && getGroup(0)[0].fragmentalImageOrNull() != null

fun ManagedTexture.requireCanonicalMainImage(): RgbImage {
    require(isCanonical()) { "Texture is not canonical (expect exactly 1 group with exactly 1 FragmentalImage)" }
    return getMainImage(0, 0)
}

fun ManagedTexture.setCanonicalMainImage(image: RgbImage, policy: MipmapPolicy = MipmapPolicy()) {
    require(isCanonical()) { "Texture is not canonical (expect exactly 1 group with exactly 1 FragmentalImage)" }
    setMainImage(0, image, 0, policy)
}

fun ModernizedRyo.createCanonicalTexture(image: RgbImage, policy: MipmapPolicy = MipmapPolicy()): ManagedTexture {
    val chamber = foundation.createTextureChamber()
    chamber.createFiGroup(packToFragmentalImage(image, policy))
    return manage(chamber)
}
