package me.earzuchan.ryo.foundation.exception

open class RyoException(message: String) : IllegalStateException(message)

class SchemaNotFoundException(modelId: String) : RyoException("ModelSchema not found: $modelId")

class GloryNotFoundException(gloryId: String, wireTypeId: String? = null) : RyoException(if (wireTypeId == null) "Glory not found: $gloryId" else "Glory not found: $gloryId for wireTypeId=$wireTypeId")