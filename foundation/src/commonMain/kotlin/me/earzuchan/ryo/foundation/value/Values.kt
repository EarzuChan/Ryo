package me.earzuchan.ryo.foundation.value

import me.earzuchan.ryo.foundation.chamber.Chamber
import me.earzuchan.ryo.foundation.type.ObjectTypeRef
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs

sealed interface RyoValue {
    val typeRef: TypeRef
    val wireTypeId: String get() = typeRef.wireTypeId
}

data class RyoNullValue(override val typeRef: TypeRef) : RyoValue

@ConsistentCopyVisibility
data class RyoScalarValue private constructor(override val typeRef: TypeRef, val value: Any) : RyoValue {
    companion object {
        fun of(typeRef: TypeRef, value: Any): RyoScalarValue {
            validate(typeRef, value)
            return RyoScalarValue(typeRef, value)
        }

        fun infer(value: Any): RyoScalarValue = when (value) {
            is Boolean -> of(TypeRefs.BOOLEAN, value)
            is Byte -> of(TypeRefs.BYTE, value)
            is Char -> of(TypeRefs.CHAR, value)
            is Double -> of(TypeRefs.DOUBLE, value)
            is Float -> of(TypeRefs.FLOAT, value)
            is Int -> of(TypeRefs.INT, value)
            is Long -> of(TypeRefs.LONG, value)
            is Short -> of(TypeRefs.SHORT, value)
            is String -> of(TypeRefs.STRING, value)
            else -> error("Unsupported inferred scalar type: ${value::class}")
        }

        private fun validate(typeRef: TypeRef, value: Any) {
            require(typeRef.wireTypeId in TypeIds.SCALARS) { "Scalar typeRef must be primitive or string. actual=${typeRef.wireTypeId}" }
            when (typeRef.wireTypeId) {
                TypeIds.BOOLEAN -> require(value is Boolean) { "Expected Boolean for ${TypeIds.BOOLEAN}" }
                TypeIds.BYTE -> require(value is Byte) { "Expected Byte for ${TypeIds.BYTE}" }
                TypeIds.CHAR -> require(value is Char) { "Expected Char for ${TypeIds.CHAR}" }
                TypeIds.DOUBLE -> require(value is Double) { "Expected Double for ${TypeIds.DOUBLE}" }
                TypeIds.FLOAT -> require(value is Float) { "Expected Float for ${TypeIds.FLOAT}" }
                TypeIds.INT -> require(value is Int) { "Expected Int for ${TypeIds.INT}" }
                TypeIds.LONG -> require(value is Long) { "Expected Long for ${TypeIds.LONG}" }
                TypeIds.SHORT -> require(value is Short) { "Expected Short for ${TypeIds.SHORT}" }
                TypeIds.STRING -> require(value is String) { "Expected String for ${TypeIds.STRING}" }
                else -> error("Unsupported scalar wireTypeId: ${typeRef.wireTypeId}")
            }
        }
    }
}

data class RyoContainerValue(override val typeRef: TypeRef, val elements: List<RyoValue>) : RyoValue

data class RyoHostedValue(override val typeRef: ObjectTypeRef, val members: Map<String, RyoValue>, val ctorCaseIndex: Int? = null) : RyoValue

// CHECK：为啥还要显式的Kind，不能按T分辨吗
data class RyoSpecialValue<out T : Any>(override val typeRef: TypeRef, val kind: String, val payload: T) : RyoValue

@JvmInline // TIPS：给Jvm看的，其它平台忽视之
value class RyoMeta(val raw: Int) {
    val payload: Int get() = raw ushr SHIFT
    val type: Int get() = raw and MASK

    // 替代之前的构造函数
    companion object {
        const val SHIFT = 2
        const val MASK = 3

        fun create(payload: Int, type: Int): RyoMeta = RyoMeta((payload shl SHIFT) or (type and MASK))

        // 预定义空元数据，避免重复计算
        val NULL = create(0, Chamber.META_TYPE_NULL)
    }
}

@Suppress("ArrayInDataClass")
data class RyoUnknownFrame(val nodeId: Int, val wireTypeId: String, val gloryId: String, val opaquePayload: ByteArray, val metas: List<RyoMeta>)

data class RyoUnknownGraph(val rootId: Int, val frames: List<RyoUnknownFrame>) {
    init {
        val ids = frames.map { it.nodeId }
        require(ids.toSet().size == ids.size) { "Unknown graph contains duplicate frame ids" }
        require(ids.contains(rootId)) { "Unknown graph root frame '$rootId' not found" }
    }

    fun frame(legacyId: Int): RyoUnknownFrame = frames.firstOrNull { it.nodeId == legacyId } ?: error("Unknown graph frame '$legacyId' not found")
}

@Suppress("ArrayInDataClass")
data class RyoUnknownValue(
    override val typeRef: TypeRef,
    val gloryId: String,
    val opaquePayload: ByteArray,
    val metas: List<RyoMeta> = emptyList(),
    val graph: RyoUnknownGraph? = null
) : RyoValue

fun RyoValue?.asNullOrNull(): RyoNullValue? = this as? RyoNullValue // XDDDD，逗我一个猪的命名
fun RyoValue?.asScalarOrNull(): RyoScalarValue? = this as? RyoScalarValue
fun RyoValue?.asHostedOrNull(): RyoHostedValue? = this as? RyoHostedValue
fun RyoValue?.asContainerOrNull(): RyoContainerValue? = this as? RyoContainerValue
fun <T : Any> RyoValue?.asSpecialOrNull(): RyoSpecialValue<T>? = this as? RyoSpecialValue<T>
fun RyoValue?.asUnknownOrNull(): RyoUnknownValue? = this as? RyoUnknownValue
