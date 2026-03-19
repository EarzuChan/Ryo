package me.earzuchan.ryo.foundation.io

import okio.Buffer

class RyoWriter(private val buf: Buffer = Buffer()) {
    fun writeInt(v: Int) = apply { buf.writeInt(v) }
    fun writeShort(v: Short) = apply { buf.writeShort(v.toInt()) }
    fun writeLong(v: Long) = apply { buf.writeLong(v) }
    fun writeByte(v: Byte) = apply { buf.writeByte(v.toInt()) }
    fun writeBoolean(v: Boolean) = apply { buf.writeByte(if (v) 1 else 0) }
    fun writeFloat(v: Float) = apply { buf.writeInt(v.toBits()) }
    fun writeDouble(v: Double) = apply { buf.writeLong(v.toBits()) }
    fun writeChar(v: Char) = apply { buf.writeShort(v.code) }
    fun writeBytes(v: ByteArray) = apply { buf.write(v) }

    fun writeFixedString(v: String) = apply { buf.write(v.encodeToByteArray()) }

    fun writeString(v: String?) = apply {
        when {
            v == null -> writeInt(-1)
            v.isEmpty() -> writeInt(0)
            else -> {
                val bytes = v.encodeToByteArray()
                writeInt(bytes.size)
                writeBytes(bytes)
            }
        }
    }

    fun toByteArray(): ByteArray = buf.snapshot().toByteArray()
}

class RyoReader(bytes: ByteArray) {
    private val buf = Buffer().write(bytes)

    fun readInt(): Int = buf.readInt()
    fun readShort(): Short = buf.readShort()
    fun readLong(): Long = buf.readLong()
    fun readUnsignedByte(): Int = buf.readByte().toInt() and 0xFF
    fun readSignedByte(): Int = buf.readByte().toInt()
    fun readBoolean(): Boolean = readUnsignedByte() != 0
    fun readFloat(): Float = Float.fromBits(readInt())
    fun readDouble(): Double = Double.fromBits(readLong())
    fun readChar(): Char = readShort().toInt().toChar()
    fun readBytes(count: Int): ByteArray = buf.readByteArray(count.toLong())
    fun readAllBytes(): ByteArray = buf.readByteArray()

    fun checkFixedString(expected: String): Boolean {
        val expectedBytes = expected.encodeToByteArray()
        val actualBytes = readBytes(expectedBytes.size)
        return actualBytes.contentEquals(expectedBytes)
    }

    fun readString(): String? {
        val len = readInt()

        return when {
            len < 0 -> null
            len == 0 -> ""
            else -> buf.readUtf8(len.toLong())
        }
    }
}
