package me.earzuchan.ryo.modern.session

import kotlinx.coroutines.flow.Flow
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.flow.map
import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.util.RgbImage
import me.earzuchan.ryo.modern.texture.MipmapPolicy
import me.earzuchan.ryo.modern.texture.extractToImage
import me.earzuchan.ryo.modern.texture.packToFragmentalImage

class FragmentalImageSession internal constructor(initialFi: FragmentalImage) {
    private val history = mutableListOf(initialFi)
    private var cursor = 0

    private val _raw = MutableStateFlow(initialFi)
    val raw: StateFlow<FragmentalImage> = _raw.asStateFlow()

    val state: Flow<RgbImage> = raw.map(::extractToImage)


    fun snapshot(): RgbImage = extractToImage(raw.value)

    fun rawFi(): FragmentalImage = raw.value

    fun setRawFi(fi: FragmentalImage) = commitLocal(fi)

    fun setMainImage(image: RgbImage, policy: MipmapPolicy = MipmapPolicy()) = commitLocal(packToFragmentalImage(image, policy))

    fun undo(): Boolean {
        if (cursor == 0) return false
        cursor--
        _raw.value = history[cursor]
        return true
    }

    fun redo(): Boolean {
        if (cursor >= history.lastIndex) return false
        cursor++
        _raw.value = history[cursor]
        return true
    }

    private fun commitLocal(fi: FragmentalImage) {
        if (fi == history[cursor]) return

        if (cursor < history.lastIndex) {
            history.subList(cursor + 1, history.size).clear()
        }

        history += fi
        cursor = history.lastIndex

        if (history.size > HISTORY_LIMIT) {
            history.removeAt(0)
            cursor--
        }
        _raw.value = fi
    }

    companion object {
        private const val HISTORY_LIMIT = 128
    }
}