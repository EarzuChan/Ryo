package me.earzuchan.ryo.modern.texture

import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import me.earzuchan.ryo.foundation.chamber.TextureChamber
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.ModernizedRyo

// CHECK：这好吗

class ManagedTexture internal constructor(private val modern: ModernizedRyo, private val chamber: TextureChamber) {
    private val revisions = MutableList(chamber.groupCount) { 0L }
    private val _groups = MutableStateFlow(revisions.toList())

    val groups: StateFlow<List<Long>> get() = _groups
    val groupCount: Int get() = revisions.size

    fun openGroup(groupIndex: Int): ImageGroupHandle = ImageGroupHandle(chamber.snapshotGroup(groupIndex), revisions[groupIndex])

    fun setGroup(groupIndex: Int, handle: ImageGroupHandle, expectedBaseRevision: Long? = handle.baseRevision) {
        ensureExpectedRevision(groupIndex, expectedBaseRevision)
        val images = handle.snapshot()
        require(images.isNotEmpty()) { "Image group cannot be empty" }
        chamber.replaceGroup(groupIndex, images.first(), images.drop(1))
        revisions[groupIndex] = revisions[groupIndex] + 1
        publish()
    }

    fun appendGroup(mainImage: RyoValue, mipmaps: List<RyoValue> = emptyList()): Int {
        chamber.createGroup(mainImage, mipmaps)
        revisions += 1L
        publish()
        return revisions.lastIndex
    }

    fun appendGroup(handle: ImageGroupHandle): Int {
        val images = handle.snapshot()
        require(images.isNotEmpty()) { "Image group cannot be empty" }
        return appendGroup(images.first(), images.drop(1))
    }

    fun removeGroup(groupIndex: Int): Boolean {
        if (groupIndex !in revisions.indices) return false
        chamber.deleteGroup(groupIndex)
        revisions.removeAt(groupIndex)
        publish()
        return true
    }

    fun raw(): TextureChamber = chamber

    private fun ensureExpectedRevision(groupIndex: Int, expectedBaseRevision: Long?) {
        if (expectedBaseRevision == null) return
        val current = revisions[groupIndex]
        require(current == expectedBaseRevision) { "Stale image group handle for index $groupIndex. expected=$current actual=$expectedBaseRevision" }
    }

    private fun publish() {
        _groups.value = revisions.toList()
    }
}

// TODO；需要上真正的照片，和联动真正的Mipmap机制。这个是更具侵犯性的设计
class ImageGroupHandle internal constructor(images: List<RyoValue>, val baseRevision: Long) {
    private data class VersionedGroup(val revision: Long, val images: List<RyoValue>)

    private val history = mutableListOf(VersionedGroup(baseRevision, images.toList()))
    private var cursor = 0
    private var nextRevision = baseRevision
    private val _state = MutableStateFlow(history[cursor].images)
    private val _revision = MutableStateFlow(history[cursor].revision)
    private val _mainImage = MutableStateFlow(images.firstOrNull() ?: error("Image group cannot be empty"))

    init {
        require(images.isNotEmpty()) { "Image group cannot be empty" }
    }

    val state: StateFlow<List<RyoValue>> get() = _state
    val revision: StateFlow<Long> get() = _revision
    val mainImage: StateFlow<RyoValue> get() = _mainImage

    fun snapshot(): List<RyoValue> = state.value

    // TODO：remipmap policy。待到秋来九月八（指真正的图片实现后）
    fun replaceMain(image: RyoValue): Long = commit(snapshot().toMutableList().also { it[0] = image })

    fun setLevel(level: Int, image: RyoValue): Long = commit(snapshot().toMutableList().also { require(level in it.indices) { "Invalid mipmap level: $level" }; it[level] = image })

    fun appendMipmap(image: RyoValue): Long = commit(snapshot() + image)

    fun dropMipmaps(): Long = commit(listOf(snapshot().first()))

    fun undo(): Boolean {
        if (cursor == 0) return false
        cursor--
        publish()
        return true
    }

    fun redo(): Boolean {
        if (cursor >= history.lastIndex) return false
        cursor++
        publish()
        return true
    }

    private fun commit(images: List<RyoValue>): Long {
        val next = images.toList()
        if (next == history[cursor].images) return history[cursor].revision
        if (cursor < history.lastIndex) history.subList(cursor + 1, history.size).clear()
        nextRevision++
        history += VersionedGroup(nextRevision, next)
        cursor = history.lastIndex
        if (history.size > HISTORY_LIMIT) {
            history.removeAt(0)
            cursor--
        }
        publish()
        return history[cursor].revision
    }

    private fun publish() {
        val current = history[cursor]
        _state.value = current.images
        _revision.value = current.revision
        _mainImage.value = current.images.first()
    }

    companion object {
        private const val HISTORY_LIMIT = 128
    }
}