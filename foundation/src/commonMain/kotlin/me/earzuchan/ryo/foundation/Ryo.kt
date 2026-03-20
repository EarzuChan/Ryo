package me.earzuchan.ryo.foundation

import me.earzuchan.ryo.foundation.chamber.TextureChamber
import me.earzuchan.ryo.foundation.chamber.VolumeChamber
import me.earzuchan.ryo.foundation.exception.GloryNotFoundException
import me.earzuchan.ryo.foundation.exception.SchemaNotFoundException
import me.earzuchan.ryo.foundation.glory.CtorGlory
import me.earzuchan.ryo.foundation.glory.FieldGlory
import me.earzuchan.ryo.foundation.glory.SpecialGlories
import me.earzuchan.ryo.foundation.glory.Glory
import me.earzuchan.ryo.foundation.glory.GloryIds
import me.earzuchan.ryo.foundation.glory.ObjectArrayGlory
import me.earzuchan.ryo.foundation.glory.PrimitiveArrayGlory
import me.earzuchan.ryo.foundation.glory.ScalarGlory
import me.earzuchan.ryo.foundation.glory.StringArrayGlory
import me.earzuchan.ryo.foundation.io.RyoReader
import me.earzuchan.ryo.foundation.io.RyoWriter
import me.earzuchan.ryo.foundation.path.RyoPath
import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.schema.CtorCaseRef
import me.earzuchan.ryo.foundation.schema.CtorPrefs
import me.earzuchan.ryo.foundation.schema.ModelSchema
import me.earzuchan.ryo.foundation.schema.ModelSchemaJson
import me.earzuchan.ryo.foundation.schema.ModelSchemaNormalizer
import me.earzuchan.ryo.foundation.type.ArrayTypeRef
import me.earzuchan.ryo.foundation.type.ObjectTypeRef
import me.earzuchan.ryo.foundation.type.PrimitiveTypeRef
import me.earzuchan.ryo.foundation.type.StringTypeRef
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.value.RyoContainerValue
import me.earzuchan.ryo.foundation.value.RyoHostedValue
import me.earzuchan.ryo.foundation.value.RyoMeta
import me.earzuchan.ryo.foundation.value.RyoNullValue
import me.earzuchan.ryo.foundation.value.RyoScalarValue
import me.earzuchan.ryo.foundation.value.RyoSpecialValue
import me.earzuchan.ryo.foundation.value.RyoUnknownGraph
import me.earzuchan.ryo.foundation.value.RyoUnknownValue
import me.earzuchan.ryo.foundation.value.RyoValue

class Ryo private constructor(internal val runtime: RyoRuntime) {
    fun createVolumeChamber(): VolumeChamber = VolumeChamber(runtime)
    fun createTextureChamber(): TextureChamber = TextureChamber(runtime)

    fun requireSchema(modelId: String): ModelSchema = runtime.requireSchema(modelId)
    fun initRyoTypeFor(typeRef: TypeRef, ctorPrefs: CtorPrefs=CtorPrefs.EMPTY): RyoValue = runtime.initRyoValueFor(typeRef, ctorPrefs)
    fun initRyoTypeFor(wireTypeId: String, ctorPrefs: CtorPrefs=CtorPrefs.EMPTY): RyoValue = initRyoTypeFor(TypeRefs.fromWireTypeId(wireTypeId), ctorPrefs)
    fun validateRyoValue(value: RyoValue) = runtime.validateRyoValue(value)

    fun scalar(value: Any): RyoScalarValue = RyoScalarValue.infer(value) // TIPS：本方法自动推导
    fun scalar(typeRef: TypeRef, value: Any): RyoScalarValue = RyoScalarValue.of(typeRef, value)
    fun scalar(wireTypeId: String, value: Any): RyoScalarValue = scalar(TypeRefs.fromWireTypeId(wireTypeId), value)
    fun nullValue(typeRef: TypeRef): RyoNullValue = RyoNullValue(typeRef)
    fun nullValue(wireTypeId: String): RyoNullValue = RyoNullValue(TypeRefs.fromWireTypeId(wireTypeId))
    fun value(typeRef: TypeRef, value: Any?): RyoValue = if (value == null) nullValue(typeRef) else scalar(typeRef, value)
    fun value(wireTypeId: String, value: Any?): RyoValue = value(TypeRefs.fromWireTypeId(wireTypeId), value)

    // HACK：我个人认为下面的有点无意义（已有自动推导、传type和null），但AI说留着也无妨，静态类型约束这一块
    fun bool(value: Boolean): RyoScalarValue = RyoScalarValue.of(TypeRefs.BOOLEAN, value)
    fun int(value: Int): RyoScalarValue = RyoScalarValue.of(TypeRefs.INT, value)
    fun long(value: Long): RyoScalarValue = RyoScalarValue.of(TypeRefs.LONG, value)
    fun float(value: Float): RyoScalarValue = RyoScalarValue.of(TypeRefs.FLOAT, value)
    fun double(value: Double): RyoScalarValue = RyoScalarValue.of(TypeRefs.DOUBLE, value)
    fun short(value: Short): RyoScalarValue = RyoScalarValue.of(TypeRefs.SHORT, value)
    fun byte(value: Byte): RyoScalarValue = RyoScalarValue.of(TypeRefs.BYTE, value)
    fun char(value: Char): RyoScalarValue = RyoScalarValue.of(TypeRefs.CHAR, value)
    fun str(value: String): RyoScalarValue = RyoScalarValue.of(TypeRefs.STRING, value)

    class HostedBuilder internal constructor() {
        internal val map = linkedMapOf<String, RyoValue>()

        fun set(name: String, value: RyoValue) {
            map[name] = value
        }

        fun setScalar(name: String, value: Any) {
            map[name] = RyoScalarValue.infer(value)
        }

        fun setScalar(name: String, typeRef: TypeRef, value: Any?) {
            map[name] = if (value == null) RyoNullValue(typeRef) else RyoScalarValue.of(typeRef, value)
        }

        fun setScalar(name: String, wireTypeId: String, value: Any?) = setScalar(name, TypeRefs.fromWireTypeId(wireTypeId), value)

        fun setNull(name: String, typeRef: TypeRef) { map[name] = RyoNullValue(typeRef) }
        fun setNull(name: String, wireTypeId: String) = setNull(name, TypeRefs.fromWireTypeId(wireTypeId))
    }

    fun hosted(modelId: String, ctorCaseIndex: Int? = null, validateNow: Boolean = runtime.validateOnCreate, ctorPrefs: CtorPrefs = CtorPrefs.EMPTY, block: HostedBuilder.() -> Unit) = hosted(TypeRefs.objectType(modelId), ctorCaseIndex, validateNow, ctorPrefs, block)
    fun hosted(typeRef: ObjectTypeRef, ctorCaseIndex: Int? = null, validateNow: Boolean = runtime.validateOnCreate, ctorPrefs: CtorPrefs = CtorPrefs.EMPTY, block: HostedBuilder.() -> Unit): RyoHostedValue {
        val base = runtime.initRyoValueFor(typeRef, ctorPrefs) as? RyoHostedValue ?: error("Cannot init hosted value for ${typeRef.modelId}")
        val b = HostedBuilder()
        b.block()
        val merged = LinkedHashMap(base.members).apply { putAll(b.map) }
        val value = RyoHostedValue(typeRef, merged, ctorCaseIndex ?: base.ctorCaseIndex)
        if (validateNow) runtime.validateRyoValue(value)
        return value
    }

    fun container(typeRef: TypeRef, elements: List<RyoValue>, validateNow: Boolean = runtime.validateOnCreate): RyoContainerValue {
        require(typeRef is ArrayTypeRef) { "Container typeRef must be array. actual=${typeRef.wireTypeId}" }
        val value = RyoContainerValue(typeRef, elements)
        if (validateNow) runtime.validateRyoValue(value)
        return value
    }
    fun container(wireTypeId: String, elements: List<RyoValue>, validateNow: Boolean = runtime.validateOnCreate): RyoContainerValue = container(TypeRefs.fromWireTypeId(wireTypeId), elements, validateNow)

    fun <T : Any> special(typeRef: TypeRef, kind: String, payload: T): RyoSpecialValue<T> = RyoSpecialValue(typeRef, kind, payload)
    fun <T : Any> special(wireTypeId: String, kind: String, payload: T): RyoSpecialValue<T> = RyoSpecialValue(TypeRefs.fromWireTypeId(wireTypeId), kind, payload)

    class Builder internal constructor() {
        private val schemas = linkedMapOf<String, ModelSchema>()
        private val customGlories = linkedMapOf<String, Glory>()
        private val customMappings = linkedMapOf<String, String>()

        private var unknownTypePolicy: UnknownTypePolicy = UnknownTypePolicy.OPAQUE
        private var strictSchemaGraph: Boolean = true
        private var validateOnCreate: Boolean = true
        private var persistentCtorPrefs: CtorPrefs = CtorPrefs.EMPTY

        fun unknownTypePolicy(policy: UnknownTypePolicy) = apply { unknownTypePolicy = policy }
        fun strictSchemaGraph(enabled: Boolean) = apply { strictSchemaGraph = enabled }
        fun validateOnCreate(enabled: Boolean) = apply { validateOnCreate = enabled }
        fun ctorPrefs(prefs: CtorPrefs) = apply { persistentCtorPrefs = prefs }
        fun ctorPrefs(block: CtorPrefs.Builder.() -> Unit) = apply { persistentCtorPrefs = CtorPrefs.build(block) }

        fun registerSchema(schema: ModelSchema) = apply {
            val normalized = ModelSchemaNormalizer.normalize(schema)
            require(normalized.modelId !in schemas) { "Duplicate schema modelId: ${normalized.modelId}" }
            schemas[normalized.modelId] = normalized
        }
        fun registerSchemas(list: Iterable<ModelSchema>) = apply { list.forEach(::registerSchema) }

        fun registerSchemaJson(content: String) = apply { registerSchema(ModelSchemaJson.parseOne(content)) }
        fun registerSchemasJson(content: String) = apply { registerSchemas(ModelSchemaJson.parseList(content)) }

        internal fun registerGlory(glory: Glory) = apply { customGlories[glory.id] = glory }
        internal fun mapWireTypeToGlory(wireTypeId: String, gloryId: String) = apply { customMappings[wireTypeId] = gloryId }

        fun build() = Ryo(
            RyoRuntime.build(
                BuildConfig(
                    schemas = schemas.values.toList(),
                    unknownTypePolicy = unknownTypePolicy,
                    strictSchemaGraph = strictSchemaGraph,
                    validateOnCreate = validateOnCreate,
                    ctorPrefs = persistentCtorPrefs,
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
    STRICT, OPAQUE
}

internal data class BuildConfig(
    val schemas: List<ModelSchema>,
    val unknownTypePolicy: UnknownTypePolicy,
    val strictSchemaGraph: Boolean,
    val validateOnCreate: Boolean,
    val ctorPrefs: CtorPrefs,
    val customGlories: List<Glory>,
    val customMappings: Map<String, String>
)

internal class RyoRuntime private constructor(val unknownTypePolicy: UnknownTypePolicy, val validateOnCreate: Boolean, private val persistentCtorPrefs: CtorPrefs) {
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
            val runtime = RyoRuntime(config.unknownTypePolicy, config.validateOnCreate, config.ctorPrefs)
            runtime.registerBuiltinGlories()

            config.customGlories.forEach(runtime::registerGlory)
            config.customMappings.forEach { (wireTypeId, gloryId) -> runtime.mapWireTypeToGlory(wireTypeId, gloryId) }
            ModelSchemaNormalizer.normalizeAll(config.schemas).forEach(runtime::registerSchema)

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

    fun unknownValueFor(
        wireTypeId: String,
        gloryId: String,
        payload: ByteArray,
        metas: List<RyoMeta>,
        graph: RyoUnknownGraph? = null
    ) = when (unknownTypePolicy) {
        UnknownTypePolicy.OPAQUE -> RyoUnknownValue(TypeRefs.fromWireTypeId(wireTypeId), gloryId, payload, metas, graph)
        UnknownTypePolicy.STRICT -> throw GloryNotFoundException(gloryId, wireTypeId)
    }

    fun isInlineWireType(wireTypeId: String): Boolean = wireTypeId in inlineWireTypes

    fun initRyoValueFor(typeRef: TypeRef, ctorPrefs: CtorPrefs, path: RyoPath = RyoPath.ROOT): RyoValue = when (typeRef) {
        is PrimitiveTypeRef -> when (typeRef.wireTypeId) {
            TypeIds.BOOLEAN -> RyoScalarValue.of(typeRef, false)
            TypeIds.BYTE -> RyoScalarValue.of(typeRef, 0.toByte())
            TypeIds.CHAR -> RyoScalarValue.of(typeRef, '\u0000')
            TypeIds.DOUBLE -> RyoScalarValue.of(typeRef, 0.0)
            TypeIds.FLOAT -> RyoScalarValue.of(typeRef, 0f)
            TypeIds.INT -> RyoScalarValue.of(typeRef, 0)
            TypeIds.LONG -> RyoScalarValue.of(typeRef, 0L)
            TypeIds.SHORT -> RyoScalarValue.of(typeRef, 0.toShort())
            else -> error("Unsupported primitive wireTypeId: ${typeRef.wireTypeId}")
        }

        StringTypeRef -> RyoScalarValue.of(TypeRefs.STRING, "")

        is ArrayTypeRef -> RyoContainerValue(typeRef, emptyList())

        is ObjectTypeRef -> {
            val schema = requireSchema(typeRef.modelId)
            require(schema.kind != ModelSchema.GloryKind.CUSTOM) { "Cannot auto create default value for custom schema ${schema.modelId}" }
            val selectedCaseIndex = selectCtorCaseIndex(schema, path, ctorPrefs)

            val members = linkedMapOf<String, RyoValue>()
            schema.members.forEach { member -> members[member.name] = if (member.nullable) RyoNullValue(member.typeRef) else initRyoValueFor(member.typeRef, ctorPrefs, path.member(member.name)) }
            RyoHostedValue(typeRef, members, if (schema.kind == ModelSchema.GloryKind.CTOR && schema.ctorCases.size > 1) selectedCaseIndex else null)
        }
    }

    fun validateRyoValue(value: RyoValue) = when (value) {
        is RyoNullValue -> Unit
        is RyoScalarValue -> Unit
        is RyoHostedValue -> validateHosted(value)
        is RyoContainerValue -> validateContainer(value)
        is RyoSpecialValue<*> -> Unit
        is RyoUnknownValue -> {
            val graph = value.graph
            if (graph != null) {
                val root = graph.frame(graph.rootId)
                require(root.wireTypeId == value.wireTypeId) { "Unknown graph root wire mismatch. root=${root.wireTypeId} value=${value.wireTypeId}" }
                require(root.gloryId == value.gloryId) { "Unknown graph root glory mismatch. root=${root.gloryId} value=${value.gloryId}" }
            }
            Unit
        }
    }

    fun readDirectScalar(wireTypeId: String, reader: RyoReader): RyoValue {
        val builtin = scalarBuiltinsByWireType[wireTypeId] ?: error("Unsupported direct scalar wireTypeId: $wireTypeId")
        val raw = builtin.read(reader)
        return if (raw == null) RyoNullValue(builtin.typeRef) else RyoScalarValue.of(builtin.typeRef, raw)
    }

    fun writeDirectScalar(wireTypeId: String, value: RyoValue, writer: RyoWriter) {
        val builtin = scalarBuiltinsByWireType[wireTypeId] ?: error("Unsupported direct scalar wireTypeId: $wireTypeId")
        when (value) {
            is RyoNullValue -> {
                require(wireTypeId == TypeIds.STRING) { "Null direct scalar is only valid for ${TypeIds.STRING}. actual=$wireTypeId" }
                builtin.write(writer, null)
            }

            is RyoScalarValue -> builtin.write(writer, value.value)
            else -> error("Direct scalar must be RyoScalarValue or RyoNullValue. actual=${value::class.simpleName}")
        }
    }

    private fun registerSchema(schema: ModelSchema) {
        val normalized = ModelSchemaNormalizer.normalize(schema)
        require(normalized.modelId !in schemas) { "Duplicate schema modelId: ${normalized.modelId}" }
        schemas[normalized.modelId] = normalized

        val gloryId = when (normalized.kind) {
            ModelSchema.GloryKind.FIELD -> GloryIds.FIELD
            ModelSchema.GloryKind.CTOR -> GloryIds.CTOR
            ModelSchema.GloryKind.CUSTOM -> normalized.customGloryId ?: error("customGloryId required for ${normalized.modelId}")
        }

        wireTypeToGlory[normalized.modelId] = gloryId
    }

    private fun registerGlory(glory: Glory) {
        glories[glory.id] = glory
    }

    private fun mapWireTypeToGlory(wireTypeId: String, gloryId: String) {
        wireTypeToGlory[wireTypeId] = gloryId
    }

    private fun validateHosted(value: RyoHostedValue) {
        val schema = requireSchema(value.typeRef.modelId)

        if (schema.kind == ModelSchema.GloryKind.CTOR && schema.ctorCases.size > 1) {
            val ctor = value.ctorCaseIndex
            require(ctor != null) { "Missing ctorCaseIndex for model ${schema.modelId} with ${schema.ctorCases.size} ctor cases" }
            require(ctor in schema.ctorCases.indices) { "Invalid ctorCaseIndex=$ctor for model ${schema.modelId}" }
        }
        val knownMembers = schema.members.associateBy { it.name }
        schema.members.forEach { member ->
            if (member.name in value.members) return@forEach
            require(member.nullable) { "Missing required member '${member.name}' for model ${schema.modelId}" }
        }

        value.members.forEach { (name, memberValue) ->
            val memberSchema = knownMembers[name] ?: error("Unknown member '$name' for model ${schema.modelId}")
            if (memberValue is RyoNullValue) {
                require(memberSchema.nullable) { "Member '$name' is not nullable in model ${schema.modelId}" }
                require(memberValue.wireTypeId == memberSchema.wireTypeId || memberSchema.wireTypeId == TypeIds.OBJECT) { "Null member '$name' wire mismatch. expected=${memberSchema.wireTypeId} actual=${memberValue.wireTypeId} model=${schema.modelId}" }
                return@forEach
            }
            val expected = memberSchema.wireTypeId
            require(memberValue.wireTypeId == expected) { "Member '$name' wire mismatch. expected=$expected actual=${memberValue.wireTypeId} model=${schema.modelId}" }
        }
    }

    private fun validateContainer(value: RyoContainerValue) {
        val ref = value.typeRef
        require(ref is ArrayTypeRef) { "Container typeRef must be array. actual=${ref.wireTypeId}" }
        val expectedElementWire = ref.element.wireTypeId
        value.elements.forEach { elem ->
            if (elem is RyoNullValue) {
                require(expectedElementWire !in TypeIds.PRIMITIVE_SCALARS) { "Primitive array cannot contain null value. array=${ref.wireTypeId}" }
                if (expectedElementWire != TypeIds.OBJECT) require(elem.wireTypeId == expectedElementWire) { "Array null element wire mismatch. expected=$expectedElementWire actual=${elem.wireTypeId} array=${ref.wireTypeId}" }
                return@forEach
            }
            if (expectedElementWire == TypeIds.OBJECT) return@forEach
            require(elem.wireTypeId == expectedElementWire) { "Array element wire mismatch. expected=$expectedElementWire actual=${elem.wireTypeId} array=${ref.wireTypeId}" }
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
        ModelSchema.GloryKind.FIELD -> schema.members.forEach { member ->
            if (member.wireTypeId in fieldDirectWireTypes) require(!member.nullable) { "FIELD direct member '${schema.modelId}.${member.name}' cannot be nullable for SEngine ABI" }
        }

        ModelSchema.GloryKind.CTOR -> {
            val cases = if (schema.ctorCases.isEmpty()) listOf(schema.members.map { it.name }) else schema.ctorCases.map { it.args }
            val ctorArgNames = cases.flatten().toSet()
            ctorArgNames.forEach { name ->
                val member = schema.member(name)
                if (member.wireTypeId in fieldDirectWireTypes) require(!member.nullable) { "CTOR direct member '${schema.modelId}.${member.name}' cannot be nullable for SEngine ABI" }
            }
        }

        ModelSchema.GloryKind.CUSTOM -> {
            val customGloryId = requireNotNull(schema.customGloryId) { "customGloryId required for ${schema.modelId}" }
            require(customGloryId in glories) { "Custom glory '$customGloryId' for model ${schema.modelId} is not registered" }
        }
    }

    private fun validateCtorCases(schema: ModelSchema) {
        if (schema.kind != ModelSchema.GloryKind.CTOR) return
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

    private fun selectCtorCaseIndex(schema: ModelSchema, path: RyoPath, sessionCtorPrefs: CtorPrefs): Int {
        if (schema.kind != ModelSchema.GloryKind.CTOR) return 0
        val cases = schema.ctorCases
        if (cases.size == 1) return 0

        val selected = resolveCtorRef(sessionCtorPrefs.byPath[path], cases, schema, "session.path:$path")
            ?: resolveCtorRef(sessionCtorPrefs.byType[schema.modelId], cases, schema, "session.type:${schema.modelId}")
            ?: resolveCtorRef(persistentCtorPrefs.byPath[path], cases, schema, "persistent.path:$path")
            ?: resolveCtorRef(persistentCtorPrefs.byType[schema.modelId], cases, schema, "persistent.type:${schema.modelId}")
            ?: cases.indexOfFirst { it.id == "default" }.takeIf { it != -1 }
            ?: error("Cannot select ctor case for ${schema.modelId} at path=$path. multiple cases=${cases.map { it.id }}")

        return selected
    }

    private fun resolveCtorRef(ref: CtorCaseRef?, cases: List<ModelSchema.CtorCase>, schema: ModelSchema, source: String): Int? = when (ref) {
        null -> null
        is CtorCaseRef.Index -> ref.index.also { require(it in cases.indices) { "Ctor index out of bounds for ${schema.modelId} from $source: $it / ${cases.size}" } }
        is CtorCaseRef.Id -> cases.indexOfFirst { it.id == ref.id }.takeIf { it >= 0 } ?: error("Ctor id '${ref.id}' not found for ${schema.modelId} from $source. available=${cases.map { it.id }}")
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
        registerGlory(SpecialGlories())
        mapWireTypeToGlory(stringArrayWireTypeId, GloryIds.ARR_STRING)
        mapWireTypeToGlory(FragmentalImage.WIRE_TYPE_ID, GloryIds.FI)
    }
}
