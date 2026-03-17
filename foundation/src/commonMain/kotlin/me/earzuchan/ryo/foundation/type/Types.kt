package me.earzuchan.ryo.foundation.type

/*写在前面：
TypeId是仅仅Schema本身，譬如"XxxModel"，它不能有包装（Wrapping）
WireTypeId就是`原Sengine中的JavaClassName`，故允许有包装，行为参照原版JavaClassName系统
TypeRef就是焕新升级的RyoType，依旧是凭虚御风的`Sengine与我们Neo之间的桥`*/

object TypeIds {
    const val BOOLEAN = "java.lang.Boolean"
    const val BYTE = "java.lang.Byte"
    const val CHAR = "java.lang.Character"
    const val DOUBLE = "java.lang.Double"
    const val FLOAT = "java.lang.Float"
    const val INT = "java.lang.Integer"
    const val LONG = "java.lang.Long"
    const val SHORT = "java.lang.Short"
    const val STRING = "java.lang.String"
    const val OBJECT = "java.lang.Object"

    val PRIMITIVE_SCALARS: Set<String> = setOf(BOOLEAN, BYTE, CHAR, DOUBLE, FLOAT, INT, LONG, SHORT)
    val SCALARS: Set<String> = PRIMITIVE_SCALARS + STRING

    val PRIMITIVE_TO_DESCRIPTOR: Map<String, Char> = mapOf(BOOLEAN to 'Z', BYTE to 'B', CHAR to 'C', DOUBLE to 'D', FLOAT to 'F', INT to 'I', LONG to 'J', SHORT to 'S')

    val DESCRIPTOR_TO_PRIMITIVE: Map<Char, String> = PRIMITIVE_TO_DESCRIPTOR.entries.associate { (wire, desc) -> desc to wire }
}

sealed interface TypeRef {
    val wireTypeId: String
}

@ConsistentCopyVisibility
data class PrimitiveTypeRef internal constructor(override val wireTypeId: String) : TypeRef

object StringTypeRef : TypeRef {
    override val wireTypeId: String = TypeIds.STRING
}

data class ObjectTypeRef(val modelId: String) : TypeRef {
    override val wireTypeId: String = modelId
}

data class ArrayTypeRef(val element: TypeRef) : TypeRef {
    override val wireTypeId: String = when (element) {
        is PrimitiveTypeRef -> "[${TypeRefs.requirePrimitiveShort(element.wireTypeId)}"
        is ArrayTypeRef -> "[${element.wireTypeId}"
        StringTypeRef -> "[L${TypeIds.STRING};"
        else -> "[L${element.wireTypeId};"
    }
}

object TypeRefs {
    private val primitiveWireToShort = TypeIds.PRIMITIVE_TO_DESCRIPTOR
    private val primitiveShortToWire = TypeIds.DESCRIPTOR_TO_PRIMITIVE

    val BOOLEAN: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.BOOLEAN)
    val BYTE: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.BYTE)
    val CHAR: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.CHAR)
    val DOUBLE: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.DOUBLE)
    val FLOAT: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.FLOAT)
    val INT: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.INT)
    val LONG: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.LONG)
    val SHORT: PrimitiveTypeRef = PrimitiveTypeRef(TypeIds.SHORT)
    val STRING: TypeRef = StringTypeRef

    fun objectType(modelId: String): ObjectTypeRef = ObjectTypeRef(modelId)
    fun arrayOf(element: TypeRef): ArrayTypeRef = ArrayTypeRef(element)

    fun fromWireTypeId(wireTypeId: String): TypeRef {
        if (!wireTypeId.startsWith("[")) return when (wireTypeId) {
            in TypeIds.PRIMITIVE_SCALARS -> PrimitiveTypeRef(wireTypeId)
            TypeIds.STRING -> STRING
            else -> ObjectTypeRef(wireTypeId)
        }

        val tail = wireTypeId.substring(1)
        val element = when {
            tail.startsWith("[") -> fromWireTypeId(tail)
            tail.length == 1 -> PrimitiveTypeRef(primitiveShortToWire[tail[0]] ?: error("Unknown primitive short type: $tail"))
            tail.startsWith("L") && tail.endsWith(";") -> {
                val className = tail.substring(1, tail.length - 1)
                if (className == TypeIds.STRING) STRING else ObjectTypeRef(className)
            }

            else -> error("Illegal array wire type id: $wireTypeId")
        }

        return ArrayTypeRef(element)
    }

    internal fun requirePrimitiveShort(wireTypeId: String): Char = primitiveWireToShort[wireTypeId] ?: error("Unsupported primitive wire type: $wireTypeId")
}