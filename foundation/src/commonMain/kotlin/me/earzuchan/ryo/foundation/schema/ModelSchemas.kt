package me.earzuchan.ryo.foundation.schema

import kotlinx.serialization.Serializable
import kotlinx.serialization.json.Json
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs

@Serializable
data class ModelSchema(
    val modelId: String,
    val kind: GloryKind,
    val members: List<ModelMember>,
    val ctorCases: List<CtorCase> = emptyList(),
    val customGloryId: String? = null
) {
    @Serializable
    enum class GloryKind { FIELD, CTOR, CUSTOM }

    @Serializable
    data class ModelMember(val name: String, val wireTypeId: String, val nullable: Boolean = false) {
        internal val typeRef: TypeRef by lazy(LazyThreadSafetyMode.NONE) { TypeRefs.fromWireTypeId(wireTypeId) }
    }

    @Serializable
    data class CtorCase(val id: String, val args: List<String>)

    init {
        require(members.isNotEmpty()) { "Schema '$modelId' must contain at least one member" }
        require(members.map { it.name }.toSet().size == members.size) { "Duplicate member name in $modelId" }
    }

    fun member(name: String): ModelMember = members.firstOrNull { it.name == name } ?: error("Member $name not found in schema $modelId")

    // TIPS：兜底
    fun normalized(): ModelSchema {
        val normalizedCases = if (kind == GloryKind.CTOR && ctorCases.isEmpty()) listOf(CtorCase("default", members.map { it.name })) else ctorCases
        return copy(ctorCases = normalizedCases)
    }
}

object ModelSchemaJson {
    private val json = Json { ignoreUnknownKeys = false; prettyPrint = false; isLenient = false }
    fun parseOne(content: String): ModelSchema = json.decodeFromString(ModelSchema.serializer(), content).normalized()
    fun parseList(content: String): List<ModelSchema> = json.decodeFromString(ListSerializer, content).map(ModelSchema::normalized)
    fun toJson(schema: ModelSchema): String = json.encodeToString(ModelSchema.serializer(), schema.normalized())
    fun toJson(schemas: List<ModelSchema>): String = json.encodeToString(ListSerializer, schemas.map(ModelSchema::normalized))
    private val ListSerializer = kotlinx.serialization.builtins.ListSerializer(ModelSchema.serializer())
}

class ModelSchemaBuilder internal constructor(private val modelId: String, private val kind: ModelSchema.GloryKind) {
    private val members = mutableListOf<ModelSchema.ModelMember>()
    private val ctorCases = mutableListOf<ModelSchema.CtorCase>()
    private var customGloryId: String? = null

    fun member(name: String, wireTypeId: String, nullable: Boolean = false) = apply { members += ModelSchema.ModelMember(name, wireTypeId, nullable) }
    fun ctorCase(id: String, vararg args: String) = apply { ctorCases += ModelSchema.CtorCase(id, args.toList()) }
    fun customGlory(gloryId: String) = apply { customGloryId = gloryId }
    fun build(): ModelSchema = ModelSchema(modelId, kind, members.toList(), ctorCases.toList(), customGloryId).normalized()
}

fun modelSchema(modelId: String, kind: ModelSchema.GloryKind, block: ModelSchemaBuilder.() -> Unit): ModelSchema = ModelSchemaBuilder(modelId, kind).apply(block).build()
fun modelSchemas(block: MutableList<ModelSchema>.() -> Unit): List<ModelSchema> = mutableListOf<ModelSchema>().apply(block).map(ModelSchema::normalized)
