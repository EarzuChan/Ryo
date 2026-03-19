package me.earzuchan.ryo.modern

import me.earzuchan.ryo.foundation.Ryo as FoundationalRyo
import me.earzuchan.ryo.foundation.chamber.TextureChamber
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.value.RyoValue
import me.earzuchan.ryo.modern.session.EditorSession
import me.earzuchan.ryo.modern.texture.ManagedTexture
import me.earzuchan.ryo.modern.volume.ManagedVolume

class ModernizedRyo private constructor(val foundation: FoundationalRyo, internal val runtime: ModernRuntime) {
    fun newSession(value: RyoValue): EditorSession = runtime.newSession(value)

    fun newSession(typeRef: TypeRef): EditorSession = newSession(foundation.initRyoTypeFor(typeRef))
    fun newSession(wireTypeId: String): EditorSession = newSession(TypeRefs.fromWireTypeId(wireTypeId))

    // 管理卷
    fun manage(volume: VolumeChamber): ManagedVolume = ManagedVolume(this, volume)
    fun manage(texture: TextureChamber): ManagedTexture = ManagedTexture(texture)

    class Builder internal constructor(private val foundation: FoundationalRyo) {
        fun build(): ModernizedRyo = ModernizedRyo(foundation, ModernRuntime(foundation))
    }
}

fun FoundationalRyo.modernize(block: ModernizedRyo.Builder.() -> Unit = {}): ModernizedRyo = ModernizedRyo.Builder(this).apply(block).build()

internal class ModernRuntime(val foundation: FoundationalRyo) {

    fun newSession(value: RyoValue): EditorSession = EditorSession(this, value)

    // 装箱助手：标量操作时的底层支撑
    fun scalarValue(typeRef: TypeRef, value: Any): RyoValue = foundation.scalar(typeRef, value)
    fun scalarValue(wireTypeId: String, value: Any): RyoValue = foundation.scalar(wireTypeId, value)
    fun nullValue(typeRef: TypeRef?): RyoValue = foundation.nullValue(typeRef ?: TypeRefs.objectType(TypeIds.OBJECT))

    fun inferScalar(value: Any): RyoValue = foundation.scalar(value)
}
