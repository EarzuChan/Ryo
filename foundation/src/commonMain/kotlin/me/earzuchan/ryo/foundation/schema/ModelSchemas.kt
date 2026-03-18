package me.earzuchan.ryo.foundation.schema

import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs

// HACK：会不会过于耦合？
data class ModelSchema(val modelId: String, val kind: GloryKind, val members: List<ModelMember>, val ctorCases: List<CtorCase> = emptyList(), val customGloryId: String? = null) {
    enum class GloryKind { FIELD, CTOR, CUSTOM }

    data class ModelMember(val name: String, val wireTypeId: String, val nullable: Boolean = false) {
        internal val typeRef: TypeRef by lazy(LazyThreadSafetyMode.NONE) { TypeRefs.fromWireTypeId(wireTypeId) }
    }

    data class CtorCase(val id: String, val args: List<String>)

    init {
        require(members.map { it.name }.toSet().size == members.size) { "Duplicate member name in $modelId" }
    }

    // TODO：升级一下，如果ctor kind不设置ctorCases，就自动把members按顺序变为一个default ctor

    fun member(name: String): ModelMember = members.firstOrNull { it.name == name } ?: error("Member $name not found in schema $modelId")
}
