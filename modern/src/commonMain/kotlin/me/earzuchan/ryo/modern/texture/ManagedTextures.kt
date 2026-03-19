package me.earzuchan.ryo.modern.texture

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import me.earzuchan.ryo.foundation.chamber.TextureChamber
import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.util.RgbImage
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.session.FragmentalImageSession

class ManagedTexture internal constructor(private val chamber: TextureChamber) {
    private val revisions = MutableList(chamber.groupCount) { 0L }
    private val _groups = MutableStateFlow(revisions.toList())

    val groups: StateFlow<List<Long>> get() = _groups.asStateFlow()
    val groupCount: Int get() = revisions.size

    fun getGroup(groupIndex: Int): List<RyoValue> = chamber.getGroup(groupIndex)

    fun setGroup(groupIndex: Int, values: List<RyoValue>) {
        chamber.setGroup(groupIndex, values)
        bump(groupIndex)
    }

    fun createGroup(values: List<RyoValue>): Int = chamber.createGroup(values).also { revisions += 0L; publish() }

    fun deleteGroup(groupIndex: Int): Boolean {
        if (!chamber.deleteGroup(groupIndex)) return false
        revisions.removeAt(groupIndex)
        publish()
        return true
    }

    fun cloneGroup(groupIndex: Int): Int = chamber.cloneGroup(groupIndex).also { revisions += 0L; publish() }

    fun checkoutFragmentalImage(groupIndex: Int, valueIndex: Int = 0) = FragmentalImageSession(chamber.getFragmentalImage(groupIndex, valueIndex))

    fun commitFragmentalImage(groupIndex: Int, fi: FragmentalImage, valueIndex: Int = 0) {
        chamber.setFragmentalImage(groupIndex, fi, valueIndex)
        bump(groupIndex)
    }
    fun commitFragmentalImage(groupIndex: Int, handle: FragmentalImageSession, valueIndex: Int = 0) = commitFragmentalImage(groupIndex, handle.rawFi(), valueIndex)

    fun getMainImage(groupIndex: Int, valueIndex: Int = 0): RgbImage = extractToImage(chamber.getFragmentalImage(groupIndex, valueIndex))

    fun setMainImage(groupIndex: Int, image: RgbImage, valueIndex: Int = 0, policy: MipmapPolicy = MipmapPolicy()) = commitFragmentalImage(groupIndex, packToFragmentalImage(image, policy), valueIndex)

    private fun bump(groupIndex: Int) {
        revisions[groupIndex] = revisions[groupIndex] + 1
        publish()
    }

    private fun publish() {
        _groups.value = revisions.toList()
    }
}

