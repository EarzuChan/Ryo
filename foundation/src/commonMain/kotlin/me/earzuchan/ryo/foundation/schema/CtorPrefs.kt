package me.earzuchan.ryo.foundation.schema

import me.earzuchan.ryo.foundation.path.RyoPath

sealed interface CtorCaseRef {
    data class Index(val index: Int) : CtorCaseRef

    data class Id(val id: String) : CtorCaseRef
}

data class CtorPrefs(val byPath: Map<RyoPath, CtorCaseRef> = emptyMap(), val byType: Map<String, CtorCaseRef> = emptyMap()) {
    companion object {
        val EMPTY = CtorPrefs()
        fun build(block: Builder.() -> Unit) = Builder().apply(block).build()
    }

    class Builder {
        private val byPath = linkedMapOf<RyoPath, CtorCaseRef>()
        private val byType = linkedMapOf<String, CtorCaseRef>()

        fun path(path: RyoPath, index: Int) = apply { byPath[path] = CtorCaseRef.Index(index) }
        fun path(path: String, index: Int) = path(RyoPath.parse(path), index)
        fun path(path: RyoPath, caseId: String) = apply { byPath[path] = CtorCaseRef.Id(caseId) }
        fun path(path: String, caseId: String) = path(RyoPath.parse(path), caseId)

        fun type(modelId: String, index: Int) = apply { byType[modelId] = CtorCaseRef.Index(index) }
        fun type(modelId: String, caseId: String) = apply { byType[modelId] = CtorCaseRef.Id(caseId) }
        fun build() = CtorPrefs(byPath.toMap(), byType.toMap())
    }
}
