package me.earzuchan.ryo.aiee.util

import io.github.vinceglb.filekit.FileKit
import io.github.vinceglb.filekit.dialogs.FileKitMode
import io.github.vinceglb.filekit.dialogs.openFilePicker
import io.github.vinceglb.filekit.path
import okio.Path
import okio.Path.Companion.toPath

object FileUtils {
    val Path.nameWithoutExt get() = this.name.substringBefore('.')

    suspend fun openFile() = FileKit.openFilePicker(mode = FileKitMode.Single)?.path?.toPath()
}