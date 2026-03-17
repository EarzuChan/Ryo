package me.earzuchan.ryo.foundation.schema

import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs

enum class GloryKind { FIELD, CTOR, CUSTOM }

data class ModelMember(val name: String, val wireTypeId: String, val nullable: Boolean = false) {
    internal val typeRef: TypeRef by lazy(LazyThreadSafetyMode.NONE) { TypeRefs.fromWireTypeId(wireTypeId) }
}

data class CtorCase(val id: String, val args: List<String>)

data class ModelSchema(val modelId: String, val kind: GloryKind, val members: List<ModelMember>, val ctorCases: List<CtorCase> = emptyList(), val customGloryId: String? = null) {
    init {
        require(members.map { it.name }.toSet().size == members.size) { "Duplicate member name in $modelId" }
    }

    fun member(name: String): ModelMember = members.firstOrNull { it.name == name } ?: error("Member $name not found in schema $modelId")
}
