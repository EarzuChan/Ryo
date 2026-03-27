package me.earzuchan.ryo.aiee.util

import io.github.vinceglb.filekit.FileKit
import io.github.vinceglb.filekit.PlatformFile
import io.github.vinceglb.filekit.dialogs.FileKitMode
import io.github.vinceglb.filekit.dialogs.openFilePicker
import io.github.vinceglb.filekit.dialogs.openFileSaver
import io.github.vinceglb.filekit.path
import okio.FileSystem
import okio.Path
import okio.Path.Companion.toPath
import java.io.File

object OldFileDialogUtils {
    suspend fun pickVolumePathToOpen() = FileKit.openFilePicker(mode = FileKitMode.Single)?.path?.toPath()?.let(OldPathUtils::canonicalVolumePath)

    suspend fun pickVolumePathToSave(suggestedName: String, currentPath: Path? = null): Path? {
        val (name, ext) = splitSuggestedName(currentPath?.name ?: suggestedName)
        return FileKit.openFileSaver(name, ext, currentPath?.parent?.let(::toPlatformFile))?.path?.toPath()?.let(OldPathUtils::canonicalVolumePath)
    }

    private fun splitSuggestedName(name: String): Pair<String, String> = name.lastIndexOf('.').takeIf { it > 0 && it < name.lastIndex }?.let {
        name.substring(0, it) to name.substring(it + 1)
    } ?: (name to "")

    private fun toPlatformFile(path: Path): PlatformFile = PlatformFile(File(path.toString()))
}

object OldPathUtils {
    fun canonicalVolumePath(path: Path, fileSystem: FileSystem = FileSystem.SYSTEM) = runCatching {
        fileSystem.canonicalize(path)
    }.recoverCatching {
        path.parent?.let { fileSystem.canonicalize(it) / path.name } ?: path
    }.getOrElse { path }

    fun volumePathKey(path: Path, fileSystem: FileSystem = FileSystem.SYSTEM) = canonicalVolumePath(path, fileSystem).toString()
}

object FileUtils {
    val Path.nameWithoutExt get() = this.name.substringBefore('.')
}