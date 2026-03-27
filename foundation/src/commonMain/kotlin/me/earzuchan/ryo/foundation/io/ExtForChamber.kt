package me.earzuchan.ryo.foundation.io

import me.earzuchan.ryo.foundation.Ryo
import me.earzuchan.ryo.foundation.chamber.Chamber
import me.earzuchan.ryo.foundation.chamber.TextureChamber
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import okio.FileSystem
import okio.Path

// TIPS：为了快捷而搞的扩展方法
fun Chamber.loadFrom(path: Path, fileSystem: FileSystem = FileSystem.SYSTEM): Chamber {
    val bytes = fileSystem.read(path) { readByteArray() }
    return loadFromBytes(bytes)
}

fun Chamber.writeTo(path: Path, fileSystem: FileSystem = FileSystem.SYSTEM, allowDeflate: Boolean = true) {
    val bytes = saveToBytes(allowDeflate)
    fileSystem.write(path) { write(bytes) }
}

fun Ryo.loadVolumeChamberFrom(path: Path, fileSystem: FileSystem = FileSystem.SYSTEM) = createVolumeChamber().apply { loadFrom(path, fileSystem) }

fun Ryo.loadTextureChamberFrom(path: Path, fileSystem: FileSystem = FileSystem.SYSTEM) = createTextureChamber().apply { loadFrom(path, fileSystem) }