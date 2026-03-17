package me.earzuchan.ryo.foundation

import me.earzuchan.ryo.foundation.chamber.TextureChamber
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.exception.GloryNotFoundException
import me.earzuchan.ryo.foundation.exception.SchemaNotFoundException
import me.earzuchan.ryo.foundation.glory.CtorGlory
import me.earzuchan.ryo.foundation.glory.FieldGlory
import me.earzuchan.ryo.foundation.glory.Glory
import me.earzuchan.ryo.foundation.glory.GloryIds
import me.earzuchan.ryo.foundation.glory.ObjectArrayGlory
import me.earzuchan.ryo.foundation.glory.PrimitiveArrayGlory
import me.earzuchan.ryo.foundation.glory.ScalarGlory
import me.earzuchan.ryo.foundation.glory.StringArrayGlory
import me.earzuchan.ryo.foundation.io.RyoReader
import me.earzuchan.ryo.foundation.io.RyoWriter
import me.earzuchan.ryo.foundation.schema.GloryKind
import me.earzuchan.ryo.foundation.schema.ModelSchema
import me.earzuchan.ryo.foundation.type.ArrayTypeRef
import me.earzuchan.ryo.foundation.type.ObjectTypeRef
import me.earzuchan.ryo.foundation.type.PrimitiveTypeRef
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.value.RyoContainerValue
import me.earzuchan.ryo.foundation.value.RyoHostedValue
import me.earzuchan.ryo.foundation.value.RyoScalarValue
import me.earzuchan.ryo.foundation.value.RyoUnknownValue
import me.earzuchan.ryo.foundation.value.RyoValue

class Ryo private constructor(internal val runtime: RyoRuntime) {
    fun createVolumeChamber(): VolumeChamber = VolumeChamber(runtime)
    fun createTextureChamber(): TextureChamber = TextureChamber(runtime)

    fun scalar(value: Any): RyoScalarValue = RyoScalarValue.infer(value) // 自动推导，不可传Null
    fun scalar(typeRef: TypeRef, value: Any?): RyoScalarValue = RyoScalarValue.of(typeRef, value)
    fun scalar(wireTypeId: String, value: Any?): RyoScalarValue = scalar(TypeRefs.fromWireTypeId(wireTypeId), value)

    // HACK：我个人认为下面的有点无意义，但AI说留着也无妨，静态类型约束这一块
    fun bool(value: Boolean): RyoScalarValue = RyoScalarValue.of(TypeRefs.BOOLEAN, value)
    fun int(value: Int): RyoScalarValue = RyoScalarValue.of(TypeRefs.INT, value)
    fun long(value: Long): RyoScalarValue = RyoScalarValue.of(TypeRefs.LONG, value)
    fun float(value: Float): RyoScalarValue = RyoScalarValue.of(TypeRefs.FLOAT, value)
    fun double(value: Double): RyoScalarValue = RyoScalarValue.of(TypeRefs.DOUBLE, value)
    fun short(value: Short): RyoScalarValue = RyoScalarValue.of(TypeRefs.SHORT, value)
    fun byte(value: Byte): RyoScalarValue = RyoScalarValue.of(TypeRefs.BYTE, value)
    fun char(value: Char): RyoScalarValue = RyoScalarValue.of(TypeRefs.CHAR, value)
    fun str(value: String?): RyoScalarValue = RyoScalarValue.of(TypeRefs.STRING, value)

    fun nullScalar(typeRef: TypeRef): RyoScalarValue = RyoScalarValue.nullOf(typeRef)
    fun nullScalar(wireTypeId: String): RyoScalarValue = nullScalar(TypeRefs.fromWireTypeId(wireTypeId))

    class HostedBuilder internal constructor() {
        internal val map = linkedMapOf<String, RyoValue?>()

        fun set(name: String, value: RyoValue?) {
            map[name] = value
        }

        fun setScalar(name: String, value: Any) {
            map[name] = RyoScalarValue.infer(value)
        }

        fun setScalar(name: String, typeRef: TypeRef, value: Any?) {
            map[name] = RyoScalarValue.of(typeRef, value)
        }

        fun setScalar(name: String, wireTypeId: String, value: Any?) = setScalar(name, TypeRefs.fromWireTypeId(wireTypeId), value)

        fun setNullScalar(name: String, typeRef: TypeRef) {
            map[name] = RyoScalarValue.nullOf(typeRef)
        }

        fun setNullScalar(name: String, wireTypeId: String) = setNullScalar(name, TypeRefs.fromWireTypeId(wireTypeId))
    }

    fun hosted(modelId: String, ctorCaseIndex: Int? = null, block: HostedBuilder.() -> Unit): RyoHostedValue {
        val b = HostedBuilder()
        b.block()
        return RyoHostedValue(TypeRefs.objectType(modelId), b.map.toMap(), ctorCaseIndex)
    }

    fun container(typeRef: TypeRef, elements: List<RyoValue?>): RyoContainerValue = RyoContainerValue(typeRef, elements)
    fun container(wireTypeId: String, elements: List<RyoValue?>): RyoContainerValue = RyoContainerValue(TypeRefs.fromWireTypeId(wireTypeId), elements)

    class Builder internal constructor() {
        private val schemas = linkedMapOf<String, ModelSchema>()
        private val customGlories = linkedMapOf<String, Glory>()
        private val customMappings = linkedMapOf<String, String>()
        private var unknownTypePolicy: UnknownTypePolicy = UnknownTypePolicy.STRICT
        private var strictSchemaGraph: Boolean = true

        fun unknownTypePolicy(policy: UnknownTypePolicy) = apply { unknownTypePolicy = policy }

        fun strictSchemaGraph(enabled: Boolean) = apply { strictSchemaGraph = enabled }

        fun registerSchema(schema: ModelSchema) = apply {
            require(schema.modelId !in schemas) { "Duplicate schema modelId: ${schema.modelId}" }
            schemas[schema.modelId] = schema
        }

        fun registerSchemas(list: Iterable<ModelSchema>) = apply { list.forEach(::registerSchema) }

        internal fun registerGlory(glory: Glory) = apply { customGlories[glory.id] = glory }

        internal fun mapWireTypeToGlory(wireTypeId: String, gloryId: String) = apply { customMappings[wireTypeId] = gloryId }

        fun build() = Ryo(
            RyoRuntime.build(
                BuildConfig(
                    schemas = schemas.values.toList(),
                    unknownTypePolicy = unknownTypePolicy,
                    strictSchemaGraph = strictSchemaGraph,
                    customGlories = customGlories.values.toList(),
                    customMappings = customMappings.toMap()
                )
            )
        )
    }

    companion object {
        operator fun invoke(block: Builder.() -> Unit = {}): Ryo = Builder().apply(block).build()
        fun builder(): Builder = Builder()
    }
}

enum class UnknownTypePolicy {
    STRICT,
    OPAQUE
}

internal data class BuildConfig(
    val schemas: List<ModelSchema>,
    val unknownTypePolicy: UnknownTypePolicy,
    val strictSchemaGraph: Boolean,
    val customGlories: List<Glory>,
    val customMappings: Map<String, String>
)

internal class RyoRuntime private constructor(val unknownTypePolicy: UnknownTypePolicy) {
    private val schemas = linkedMapOf<String, ModelSchema>()
    private val glories = linkedMapOf<String, Glory>()
    private val wireTypeToGlory = linkedMapOf<String, String>()
    private val scalarBuiltinsByWireType = linkedMapOf<String, ScalarBuiltin>()

    private val inlineWireTypes = TypeIds.PRIMITIVE_SCALARS
    private val stringArrayWireTypeId = TypeRefs.arrayOf(TypeRefs.STRING).wireTypeId
    private val fieldDirectWireTypes = setOf(TypeIds.INT, TypeIds.FLOAT, TypeIds.BOOLEAN)

    private data class ScalarBuiltin(val gloryId: String, val typeRef: TypeRef, val read: RyoReader.() -> Any?, val write: RyoWriter.(Any?) -> Unit) {
        val wireTypeId: String get() = typeRef.wireTypeId
    }

    private data class PrimitiveArrayBuiltin(val gloryId: String, val elementTypeRef: PrimitiveTypeRef, val readElem: RyoReader.() -> Any, val writeElem: RyoWriter.(Any) -> Unit) {
        val wireTypeId: String get() = TypeRefs.arrayOf(elementTypeRef).wireTypeId
    }

    companion object {
        fun build(config: BuildConfig): RyoRuntime {
            val runtime = RyoRuntime(config.unknownTypePolicy)
            runtime.registerBuiltinGlories()

            config.customGlories.forEach(runtime::registerGlory)
            config.customMappings.forEach { (wireTypeId, gloryId) -> runtime.mapWireTypeToGlory(wireTypeId, gloryId) }
            config.schemas.forEach(runtime::registerSchema)

            runtime.validateSchemas(config.strictSchemaGraph)
            return runtime
        }
    }

    fun requireSchema(modelId: String): ModelSchema = schemas[modelId] ?: throw SchemaNotFoundException(modelId)

    fun findGlory(gloryId: String): Glory? = glories[gloryId]

    fun requireGlory(gloryId: String): Glory = glories[gloryId] ?: throw GloryNotFoundException(gloryId)

    fun resolveGloryId(typeRef: TypeRef): String {
        val wireTypeId = typeRef.wireTypeId
        wireTypeToGlory[wireTypeId]?.let { return it }

        if (typeRef is ArrayTypeRef) return when (typeRef.element) {
            is PrimitiveTypeRef -> error("No primitive array glory mapping for wireTypeId=$wireTypeId")
            else -> if (wireTypeId == stringArrayWireTypeId) GloryIds.ARR_STRING else GloryIds.ARR_OBJECT
        }

        error("No glory mapping for wireTypeId=$wireTypeId")
    }

    fun unknownValueFor(wireTypeId: String, gloryId: String, payload: ByteArray? = null, metaHeapSlice: IntArray = intArrayOf()) = when (unknownTypePolicy) {
        UnknownTypePolicy.OPAQUE -> RyoUnknownValue(TypeRefs.fromWireTypeId(wireTypeId), gloryId, payload, metaHeapSlice)
        UnknownTypePolicy.STRICT -> throw GloryNotFoundException(gloryId, wireTypeId)
    }

    fun isInlineWireType(wireTypeId: String): Boolean = wireTypeId in inlineWireTypes

    fun validateForWrite(value: RyoValue) = when (value) {
        is RyoScalarValue -> Unit
        is RyoHostedValue -> validateHosted(value)
        is RyoContainerValue -> validateContainer(value)
        is RyoUnknownValue -> requireNotNull(value.opaquePayload) { "Cannot write unknown value without opaque payload. glory=${value.gloryId}" }
        else -> {}
    }

    fun readDirectScalar(wireTypeId: String, reader: RyoReader): Any? {
        val builtin = scalarBuiltinsByWireType[wireTypeId] ?: error("Unsupported direct scalar wireTypeId: $wireTypeId")
        return builtin.read(reader)
    }

    fun writeDirectScalar(wireTypeId: String, value: Any?, writer: RyoWriter) {
        if (value == null) require(wireTypeId == TypeIds.STRING) { "Null direct scalar is only valid for ${TypeIds.STRING}. actual=$wireTypeId" }
        val builtin = scalarBuiltinsByWireType[wireTypeId] ?: error("Unsupported direct scalar wireTypeId: $wireTypeId")
        builtin.write(writer, value)
    }

    private fun registerSchema(schema: ModelSchema) {
        require(schema.modelId !in schemas) { "Duplicate schema modelId: ${schema.modelId}" }
        schemas[schema.modelId] = schema

        val gloryId = when (schema.kind) {
            GloryKind.FIELD -> GloryIds.FIELD
            GloryKind.CTOR -> GloryIds.CTOR
            GloryKind.CUSTOM -> schema.customGloryId ?: error("customGloryId required for ${schema.modelId}")
        }

        wireTypeToGlory[schema.modelId] = gloryId
    }

    private fun registerGlory(glory: Glory) {
        glories[glory.id] = glory
    }

    private fun mapWireTypeToGlory(wireTypeId: String, gloryId: String) {
        wireTypeToGlory[wireTypeId] = gloryId
    }

    private fun validateHosted(value: RyoHostedValue) {
        val schema = requireSchema(value.typeRef.modelId)
        val knownMembers = schema.members.associateBy { it.name }

        value.members.forEach { (name, memberValue) ->
            val memberSchema = knownMembers[name] ?: error("Unknown member '$name' for model ${schema.modelId}")
            if (memberValue == null) {
                require(memberSchema.nullable) { "Member '$name' is not nullable in model ${schema.modelId}" }
                return@forEach
            }

            val expected = memberSchema.wireTypeId
            require(memberValue.wireTypeId == expected) { "Member '$name' wire mismatch. expected=$expected actual=${memberValue.wireTypeId} model=${schema.modelId}" }
        }
    }

    private fun validateContainer(value: RyoContainerValue) {
        val ref = value.typeRef
        if (ref is ArrayTypeRef) {
            val expectedElementWire = ref.element.wireTypeId
            value.elements.forEach { elem ->
                if (elem == null) return@forEach
                if (expectedElementWire == TypeIds.OBJECT) return@forEach
                require(elem.wireTypeId == expectedElementWire) { "Array element wire mismatch. expected=$expectedElementWire actual=${elem.wireTypeId} array=${ref.wireTypeId}" }
            }
        }
    }

    private fun validateSchemas(strictSchemaGraph: Boolean) = schemas.values.forEach { schema ->
        validateCtorCases(schema)
        validateSchemaAbiConstraints(schema)
        if (!strictSchemaGraph) return@forEach

        schema.members.forEach { member ->
            collectObjectTypeIds(member.typeRef).forEach { objectTypeId ->
                require(objectTypeId in schemas || objectTypeId in wireTypeToGlory) { "Unresolved object type '$objectTypeId' referenced by ${schema.modelId}.${member.name}" }
            }
        }
    }

    private fun validateSchemaAbiConstraints(schema: ModelSchema) = when (schema.kind) {
        GloryKind.FIELD -> schema.members.forEach { member ->
            if (member.wireTypeId in fieldDirectWireTypes) require(!member.nullable) { "FIELD direct member '${schema.modelId}.${member.name}' cannot be nullable for SEngine ABI" }
        }

        GloryKind.CTOR -> {
            val cases = if (schema.ctorCases.isEmpty()) listOf(schema.members.map { it.name }) else schema.ctorCases.map { it.args }
            val ctorArgNames = cases.flatten().toSet()
            ctorArgNames.forEach { name ->
                val member = schema.member(name)
                if (member.wireTypeId in fieldDirectWireTypes) require(!member.nullable) { "CTOR direct member '${schema.modelId}.${member.name}' cannot be nullable for SEngine ABI" }
            }
        }

        GloryKind.CUSTOM -> {
            val customGloryId = requireNotNull(schema.customGloryId) { "customGloryId required for ${schema.modelId}" }
            require(customGloryId in glories) { "Custom glory '$customGloryId' for model ${schema.modelId} is not registered" }
        }
    }

    private fun validateCtorCases(schema: ModelSchema) {
        if (schema.kind != GloryKind.CTOR) return
        val validMembers = schema.members.map { it.name }.toSet()

        schema.ctorCases.forEach { ctor ->
            require(ctor.args.isNotEmpty()) { "Ctor case '${ctor.id}' has no args in model ${schema.modelId}" }
            require(ctor.args.toSet().size == ctor.args.size) { "Ctor case '${ctor.id}' has duplicate args in model ${schema.modelId}" }
            ctor.args.forEach { arg -> require(arg in validMembers) { "Ctor case '${ctor.id}' references unknown member '$arg' in model ${schema.modelId}" } }
        }
    }

    private fun collectObjectTypeIds(typeRef: TypeRef): Set<String> = when (typeRef) {
        is ObjectTypeRef -> setOf(typeRef.modelId)
        is ArrayTypeRef -> collectObjectTypeIds(typeRef.element)
        else -> emptySet()
    }

    private fun registerBuiltinGlories() {
        registerGlory(FieldGlory(this))
        registerGlory(CtorGlory(this))

        val scalarBuiltins = listOf(
            ScalarBuiltin(GloryIds.INT, TypeRefs.INT, { readInt() }, { writeInt(it as Int) }),
            ScalarBuiltin(GloryIds.BOOLEAN, TypeRefs.BOOLEAN, { readBoolean() }, { writeBoolean(it as Boolean) }),
            ScalarBuiltin(GloryIds.FLOAT, TypeRefs.FLOAT, { readFloat() }, { writeFloat(it as Float) }),
            ScalarBuiltin(GloryIds.LONG, TypeRefs.LONG, { readLong() }, { writeLong(it as Long) }),
            ScalarBuiltin(GloryIds.DOUBLE, TypeRefs.DOUBLE, { readDouble() }, { writeDouble(it as Double) }),
            ScalarBuiltin(GloryIds.SHORT, TypeRefs.SHORT, { readShort() }, { writeShort(it as Short) }),
            ScalarBuiltin(GloryIds.BYTE, TypeRefs.BYTE, { readSignedByte().toByte() }, { writeByte(it as Byte) }),
            ScalarBuiltin(GloryIds.CHAR, TypeRefs.CHAR, { readChar() }, { writeChar(it as Char) }),
            ScalarBuiltin(GloryIds.STRING, TypeRefs.STRING, { readString() }, { writeString(it as String?) })
        )
        scalarBuiltins.forEach { b ->
            registerGlory(ScalarGlory(b.gloryId, b.wireTypeId, b.typeRef, b.read, b.write))
            mapWireTypeToGlory(b.wireTypeId, b.gloryId)
            scalarBuiltinsByWireType[b.wireTypeId] = b
        }

        val primitiveArrayBuiltins = listOf(
            PrimitiveArrayBuiltin(GloryIds.ARR_INT, TypeRefs.INT, { readInt() }, { writeInt(it as Int) }),
            PrimitiveArrayBuiltin(GloryIds.ARR_SHORT, TypeRefs.SHORT, { readShort() }, { writeShort(it as Short) }),
            PrimitiveArrayBuiltin(GloryIds.ARR_FLOAT, TypeRefs.FLOAT, { readFloat() }, { writeFloat(it as Float) }),
            PrimitiveArrayBuiltin(GloryIds.ARR_LONG, TypeRefs.LONG, { readLong() }, { writeLong(it as Long) }),
            PrimitiveArrayBuiltin(GloryIds.ARR_DOUBLE, TypeRefs.DOUBLE, { readDouble() }, { writeDouble(it as Double) }),
            PrimitiveArrayBuiltin(GloryIds.ARR_CHAR, TypeRefs.CHAR, { readChar() }, { writeChar(it as Char) }),
            PrimitiveArrayBuiltin(GloryIds.ARR_BYTE, TypeRefs.BYTE, { readSignedByte().toByte() }, { writeByte(it as Byte) }),
            PrimitiveArrayBuiltin(GloryIds.ARR_BOOLEAN, TypeRefs.BOOLEAN, { readBoolean() }, { writeBoolean(it as Boolean) })
        )
        primitiveArrayBuiltins.forEach { b ->
            registerGlory(PrimitiveArrayGlory(b.gloryId, b.elementTypeRef, b.readElem, b.writeElem))
            mapWireTypeToGlory(b.wireTypeId, b.gloryId)
        }

        registerGlory(StringArrayGlory())
        registerGlory(ObjectArrayGlory())
        mapWireTypeToGlory(stringArrayWireTypeId, GloryIds.ARR_STRING)
    }
}
