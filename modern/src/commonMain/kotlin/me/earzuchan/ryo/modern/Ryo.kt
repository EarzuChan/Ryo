package me.earzuchan.ryo.modern

import me.earzuchan.ryo.foundation.Ryo as FoundationalRyo
import me.earzuchan.ryo.foundation.chamber.TextureChamber
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.value.RyoScalarValue
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.session.EditorSession
import me.earzuchan.ryo.modern.texture.ManagedTexture
import me.earzuchan.ryo.modern.volume.ManagedVolume

class ModernizedRyo private constructor(val foundation: FoundationalRyo, internal val runtime: ModernRuntime) {
    fun newSession(value: RyoValue): EditorSession = runtime.newSession(value)

    fun newSession(typeRef: TypeRef): EditorSession = newSession(foundation.newValue(typeRef))
    fun newSession(wireTypeId: String): EditorSession = newSession(TypeRefs.fromWireTypeId(wireTypeId))

    // 管理大盘
    fun manage(volume: VolumeChamber): ManagedVolume = ManagedVolume(this, volume)
    fun manage(texture: TextureChamber): ManagedTexture = ManagedTexture(this, texture)

    class Builder internal constructor(private val foundation: FoundationalRyo) {
        // 未来在这里注册 Projection Schema

        fun build(): ModernizedRyo = ModernizedRyo(foundation, ModernRuntime(foundation))
    }
}

fun FoundationalRyo.modernize(block: ModernizedRyo.Builder.() -> Unit = {}): ModernizedRyo = ModernizedRyo.Builder(this).apply(block).build()

internal class ModernRuntime(val foundation: FoundationalRyo) {

    fun newSession(value: RyoValue): EditorSession = EditorSession(this, value)

    // 装箱助手：标量操作时的底层支撑
    fun scalarValue(typeRef: TypeRef, value: Any?): RyoScalarValue = foundation.scalar(typeRef, value)
    fun scalarValue(wireTypeId: String, value: Any?): RyoScalarValue = foundation.scalar(wireTypeId, value)

    fun inferScalar(value: Any): RyoScalarValue = foundation.scalar(value)
}