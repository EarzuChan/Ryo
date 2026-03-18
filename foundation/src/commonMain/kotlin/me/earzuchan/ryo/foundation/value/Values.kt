package me.earzuchan.ryo.foundation.value

import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.type.ObjectTypeRef
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs

// TIPS：从设计上，Value是希望“创建后（内容）不可变的”。想改内容，建议是通过Copy，懂我意思？

// HACK、CHECK：我感觉仅Scalar可Null有点怪，全部应该都有可Null，或者搞个具有WireTypeId（TypeRef）的RyoNullValue

sealed interface RyoValue {
    val typeRef: TypeRef
    val wireTypeId: String get() = typeRef.wireTypeId
}

@ConsistentCopyVisibility
data class RyoScalarValue private constructor(override val typeRef: TypeRef, val value: Any?) : RyoValue {
    val isNull: Boolean get() = value == null

    companion object {
        fun of(typeRef: TypeRef, value: Any?): RyoScalarValue {
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

        fun nullOf(typeRef: TypeRef): RyoScalarValue = of(typeRef, null)

        private fun validate(typeRef: TypeRef, value: Any?) {
            require(typeRef.wireTypeId in TypeIds.SCALARS) { "Scalar typeRef must be primitive or string. actual=${typeRef.wireTypeId}" }

            if (value == null) {
                require(typeRef.wireTypeId == TypeIds.STRING) { "Null scalar is only valid for ${TypeIds.STRING}. actual=${typeRef.wireTypeId}" }
                return
            }

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

data class RyoContainerValue( // HACK：目前仅支持List，且没有确保成员的不可变性
    override val typeRef: TypeRef,
    val elements: List<RyoValue?>
) : RyoValue

data class RyoHostedValue(
    override val typeRef: ObjectTypeRef,
    val members: Map<String, RyoValue?>,
    val ctorCaseIndex: Int? = null
) : RyoValue

// TIPS：特制值，需要用独立的读写逻辑（即独立的Glory）来进行序列化反序列化
data class RyoSpecialValue<out T : Any>(
    override val typeRef: TypeRef,
    val kind: String,
    val payload: T
) : RyoValue

// The Fallback？
data class RyoUnknownValue(
    override val typeRef: TypeRef,
    val gloryId: String,
    val opaquePayload: ByteArray? = null,
    val metaHeapSlice: IntArray = intArrayOf()
) : RyoValue
