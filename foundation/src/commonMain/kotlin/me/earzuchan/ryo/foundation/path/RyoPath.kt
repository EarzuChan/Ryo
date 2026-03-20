package me.earzuchan.ryo.foundation.path

sealed interface RyoPathSegment {
    data class Member(val name: String) : RyoPathSegment

    data class Index(val index: Int) : RyoPathSegment // For List-like
}

data class RyoPath(val segments: List<RyoPathSegment> = emptyList()) {
    fun member(name: String): RyoPath {
        require(name.isNotEmpty()) { "Path member cannot be empty" }
        return RyoPath(segments + RyoPathSegment.Member(name))
    }

    fun index(index: Int): RyoPath {
        require(index >= 0) { "Path index must be >= 0, actual=$index" }
        return RyoPath(segments + RyoPathSegment.Index(index))
    }

    operator fun plus(other: RyoPath): RyoPath = RyoPath(segments + other.segments)

    fun isAffectedBy(mutatedPath: RyoPath): Boolean = if (segments.size <= mutatedPath.segments.size) mutatedPath.segments.take(segments.size) == segments else segments.take(mutatedPath.segments.size) == mutatedPath.segments

    fun asCanonicalString(): String {
        if (segments.isEmpty()) return "/"

        val out = StringBuilder("/")
        segments.forEach { seg ->
            when (seg) {
                is RyoPathSegment.Member -> {
                    if (out.length > 1 && out.last() != '/') out.append('/')
                    out.append(seg.name)
                }

                is RyoPathSegment.Index -> out.append('[').append(seg.index).append(']')
            }
        }

        return out.toString()
    }

    override fun toString(): String = asCanonicalString()

    companion object {
        val ROOT: RyoPath = RyoPath()

        fun parse(raw: String): RyoPath {
            val trimmed = raw.trim()

            val normalized = when {
                trimmed.isEmpty() || trimmed == "/" -> "/"
                trimmed.startsWith("/") -> trimmed
                else -> "/$trimmed"
            }
            if (normalized == "/") return ROOT

            val result = mutableListOf<RyoPathSegment>()
            var cursor = 1
            while (cursor < normalized.length) {
                val memberStart = cursor
                while (cursor < normalized.length && normalized[cursor] != '/' && normalized[cursor] != '[') cursor++
                if (cursor > memberStart) result += RyoPathSegment.Member(normalized.substring(memberStart, cursor))

                while (cursor < normalized.length && normalized[cursor] == '[') {
                    val end = normalized.indexOf(']', cursor + 1)
                    require(end > cursor + 1) { "Invalid path index segment in '$raw' at offset $cursor" }
                    val indexText = normalized.substring(cursor + 1, end)
                    val index = indexText.toIntOrNull() ?: error("Invalid path index '$indexText' in '$raw'")
                    require(index >= 0) { "Path index must be >= 0 in '$raw'" }
                    result += RyoPathSegment.Index(index)
                    cursor = end + 1
                }

                if (cursor < normalized.length) {
                    require(normalized[cursor] == '/') { "Invalid path syntax '$raw' at offset $cursor" }
                    cursor++
                }
            }

            return RyoPath(result)
        }
    }
}
