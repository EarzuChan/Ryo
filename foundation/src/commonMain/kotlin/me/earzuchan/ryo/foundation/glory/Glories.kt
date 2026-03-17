package me.earzuchan.ryo.foundation.glory

import me.earzuchan.ryo.foundation.RyoRuntime
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.chamber.Chamber
import me.earzuchan.ryo.foundation.io.RyoReader
import me.earzuchan.ryo.foundation.io.RyoWriter
import me.earzuchan.ryo.foundation.schema.CtorCase
import me.earzuchan.ryo.foundation.schema.GloryKind
import me.earzuchan.ryo.foundation.schema.ModelSchema
import me.earzuchan.ryo.foundation.type.ObjectTypeRef
import me.earzuchan.ryo.foundation.type.PrimitiveTypeRef
import me.earzuchan.ryo.foundation.type.TypeRef
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.value.RyoContainerValue
import me.earzuchan.ryo.foundation.value.RyoHostedValue
import me.earzuchan.ryo.foundation.value.RyoScalarValue
import me.earzuchan.ryo.foundation.value.RyoValue

object GloryIds {
    const val FIELD = "sengine.mass.serializers.FieldSerializer"
    const val CTOR = "sengine.mass.serializers.MassSerializableSerializer"

    const val INT = "sengine.mass.serializers.DefaultSerializers\$IntSerializer"
    const val BOOLEAN = "sengine.mass.serializers.DefaultSerializers\$BooleanSerializer"
    const val STRING = "sengine.mass.serializers.DefaultSerializers\$StringSerializer"
    const val FLOAT = "sengine.mass.serializers.DefaultSerializers\$FloatSerializer"
    const val LONG = "sengine.mass.serializers.DefaultSerializers\$LongSerializer"
    const val DOUBLE = "sengine.mass.serializers.DefaultSerializers\$DoubleSerializer"
    const val SHORT = "sengine.mass.serializers.DefaultSerializers\$ShortSerializer"
    const val BYTE = "sengine.mass.serializers.DefaultSerializers\$ByteSerializer"
    const val CHAR = "sengine.mass.serializers.DefaultSerializers\$CharSerializer"

    const val ARR_INT = "sengine.mass.serializers.DefaultArraySerializers\$IntArraySerializer"
    const val ARR_SHORT = "sengine.mass.serializers.DefaultArraySerializers\$ShortArraySerializer"
    const val ARR_FLOAT = "sengine.mass.serializers.DefaultArraySerializers\$FloatArraySerializer"
    const val ARR_LONG = "sengine.mass.serializers.DefaultArraySerializers\$LongArraySerializer"
    const val ARR_DOUBLE = "sengine.mass.serializers.DefaultArraySerializers\$DoubleArraySerializer"
    const val ARR_CHAR = "sengine.mass.serializers.DefaultArraySerializers\$CharArraySerializer"
    const val ARR_BYTE = "sengine.mass.serializers.DefaultArraySerializers\$ByteArraySerializer"
    const val ARR_BOOLEAN = "sengine.mass.serializers.DefaultArraySerializers\$BooleanArraySerializer"
    const val ARR_STRING = "sengine.mass.serializers.DefaultArraySerializers\$StringArraySerializer"
    const val ARR_OBJECT = "sengine.mass.serializers.DefaultArraySerializers\$ObjectArraySerializer"
}

internal interface Glory {
    val id: String
    fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue
    fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String)
}

internal class ScalarGlory(
    override val id: String,
    private val wireTypeId: String,
    private val typeRef: TypeRef,
    private val r: RyoReader.() -> Any?,
    private val w: RyoWriter.(Any?) -> Unit
) : Glory {
    override fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue {
        require(declaredWireTypeId == wireTypeId) { "Declared wire mismatch: $declaredWireTypeId != $wireTypeId" }
        return RyoScalarValue.of(typeRef, ctx.reader.r())
    }

    override fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String) {
        require(declaredWireTypeId == wireTypeId) { "Declared wire mismatch: $declaredWireTypeId != $wireTypeId" }
        val scalar = value as? RyoScalarValue ?: error("Expect RyoScalarValue for $wireTypeId")
        ctx.writer.w(scalar.value)
    }
}

internal class PrimitiveArrayGlory(
    override val id: String,
    private val elementTypeRef: PrimitiveTypeRef,
    private val readElem: RyoReader.() -> Any,
    private val writeElem: RyoWriter.(Any) -> Unit
) : Glory {
    private val arrayWireTypeId: String = TypeRefs.arrayOf(elementTypeRef).wireTypeId

    override fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue {
        require(declaredWireTypeId == arrayWireTypeId)
        val n = ctx.reader.readInt()
        val items = ArrayList<RyoValue?>(n)
        repeat(n) { items += RyoScalarValue.of(elementTypeRef, ctx.reader.readElem()) }
        return RyoContainerValue(TypeRefs.fromWireTypeId(arrayWireTypeId), items)
    }

    override fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String) {
        require(declaredWireTypeId == arrayWireTypeId)
        val arr = value as? RyoContainerValue ?: error("Expect RyoContainerValue for $arrayWireTypeId")
        ctx.writer.writeInt(arr.elements.size)
        arr.elements.forEach { e ->
            val s = e as? RyoScalarValue ?: error("Expect scalar element in $arrayWireTypeId")
            val v = requireNotNull(s.value) { "Primitive array cannot contain null element. array=$arrayWireTypeId" }
            ctx.writer.writeElem(v)
        }
    }
}

internal class StringArrayGlory : Glory {
    override val id: String = GloryIds.ARR_STRING
    private val stringArrayWireTypeId: String = TypeRefs.arrayOf(TypeRefs.STRING).wireTypeId

    override fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue {
        require(declaredWireTypeId == stringArrayWireTypeId)
        val n = ctx.reader.readInt()
        val items = ArrayList<RyoValue?>(n)
        repeat(n) { items += RyoScalarValue.of(TypeRefs.STRING, ctx.reader.readString()) }
        return RyoContainerValue(TypeRefs.fromWireTypeId(stringArrayWireTypeId), items)
    }

    override fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String) {
        require(declaredWireTypeId == stringArrayWireTypeId)
        val arr = value as? RyoContainerValue ?: error("Expect RyoContainerValue for $stringArrayWireTypeId")
        ctx.writer.writeInt(arr.elements.size)
        arr.elements.forEach { e ->
            val s = e as? RyoScalarValue ?: error("Expect scalar string element")
            ctx.writer.writeString(s.value as String?)
        }
    }
}

internal class ObjectArrayGlory : Glory {
    override val id: String = GloryIds.ARR_OBJECT

    override fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue {
        val n = ctx.reader.readInt()
        val items = ArrayList<RyoValue?>(n)
        repeat(n) { items += ctx.chamber.readChild(ctx) }
        return RyoContainerValue(TypeRefs.fromWireTypeId(declaredWireTypeId), items)
    }

    override fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String) {
        val arr = value as? RyoContainerValue ?: error("Expect RyoContainerValue for object array")
        ctx.writer.writeInt(arr.elements.size)
        arr.elements.forEach { ctx.chamber.writeChild(ctx, it) }
    }
}

internal class FieldGlory(private val ryo: RyoRuntime) : Glory {
    override val id: String = GloryIds.FIELD

    // TIPS：legacy行为对齐：字段模式仅支持 int/float/bool
    private val directWireTypes = setOf(TypeIds.INT, TypeIds.FLOAT, TypeIds.BOOLEAN)

    override fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue {
        val schema = ryo.requireSchema(declaredWireTypeId)
        require(schema.kind == GloryKind.FIELD) { "Schema $declaredWireTypeId is not FIELD" }

        val map = LinkedHashMap<String, RyoValue?>()
        schema.members.forEach { m ->
            val memberWire = m.wireTypeId
            map[m.name] = if (memberWire in directWireTypes) RyoScalarValue.of(m.typeRef, ryo.readDirectScalar(memberWire, ctx.reader)) else ctx.chamber.readChild(ctx)
        }

        return RyoHostedValue(ObjectTypeRef(schema.modelId), map)
    }

    override fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String) {
        val schema = ryo.requireSchema(declaredWireTypeId)
        require(schema.kind == GloryKind.FIELD) { "Schema $declaredWireTypeId is not FIELD" }
        val hosted = value as? RyoHostedValue ?: error("Expect RyoHostedValue for $declaredWireTypeId")

        schema.members.forEach { m ->
            val mv = hosted.members[m.name]
            if (mv == null && !m.nullable) error("Member '${m.name}' is not nullable in model ${schema.modelId}")
            val memberWire = m.wireTypeId
            if (memberWire in directWireTypes) {
                val s = mv as? RyoScalarValue ?: error("Direct scalar member '${m.name}' cannot be null in FIELD model ${schema.modelId}")
                ryo.writeDirectScalar(memberWire, s.value, ctx.writer)
            } else ctx.chamber.writeChild(ctx, mv)
        }
    }
}

internal class CtorGlory(private val ryo: RyoRuntime) : Glory {
    override val id: String = GloryIds.CTOR

    // TIPS：legacy行为对齐：构造器模式直接包含字符串
    private val directWireTypes = setOf(TypeIds.INT, TypeIds.FLOAT, TypeIds.BOOLEAN, TypeIds.STRING)

    override fun read(ctx: Chamber.ReadCtx, declaredWireTypeId: String): RyoValue {
        val schema = ryo.requireSchema(declaredWireTypeId)
        require(schema.kind == GloryKind.CTOR) { "Schema $declaredWireTypeId is not CTOR" }

        val cases = effectiveCases(schema)
        val caseIndex = if (cases.size > 1) normalizeSignedByteIndex(ctx.reader.readSignedByte(), cases.size) else 0
        val selected = cases[caseIndex]

        val map = LinkedHashMap<String, RyoValue?>()
        selected.args.forEach { name ->
            val m = schema.member(name)
            val memberWire = m.wireTypeId
            map[name] = if (memberWire in directWireTypes) RyoScalarValue.of(m.typeRef, ryo.readDirectScalar(memberWire, ctx.reader)) else ctx.chamber.readChild(ctx)
        }

        val resolvedCtorCaseIndex = if (cases.size > 1) caseIndex else null
        return RyoHostedValue(ObjectTypeRef(schema.modelId), map, ctorCaseIndex = resolvedCtorCaseIndex)
    }

    override fun write(ctx: Chamber.WriteCtx, value: RyoValue, declaredWireTypeId: String) {
        val schema = ryo.requireSchema(declaredWireTypeId)
        require(schema.kind == GloryKind.CTOR) { "Schema $declaredWireTypeId is not CTOR" }
        val hosted = value as? RyoHostedValue ?: error("Expect RyoHostedValue for $declaredWireTypeId")

        val cases = effectiveCases(schema)
        val caseIndex = hosted.ctorCaseIndex ?: inferCase(hosted, cases)
        val selected = cases.getOrElse(caseIndex) { error("Invalid ctor case index $caseIndex for $declaredWireTypeId") }

        if (cases.size > 1) ctx.writer.writeByte(caseIndex.toByte())

        selected.args.forEach { name ->
            val m = schema.member(name)
            val mv = hosted.members[name]
            if (mv == null && !m.nullable) error("Ctor arg '$name' is not nullable in model ${schema.modelId}")
            val memberWire = m.wireTypeId
            if (memberWire in directWireTypes) {
                val value = mv?.let { (it as? RyoScalarValue ?: error("Missing direct ctor arg $name")).value }
                ryo.writeDirectScalar(memberWire, value, ctx.writer)
            } else ctx.chamber.writeChild(ctx, mv)
        }
    }

    private fun effectiveCases(schema: ModelSchema): List<CtorCase> = schema.ctorCases.ifEmpty { listOf(CtorCase("default", schema.members.map { it.name })) }

    private fun inferCase(hosted: RyoHostedValue, cases: List<CtorCase>): Int {
        val keys = hosted.members.keys
        val idx = cases.indexOfFirst { c -> c.args.all { it in keys } }
        return if (idx >= 0) idx else 0
    }

    private fun normalizeSignedByteIndex(v: Int, size: Int): Int {
        val x = if (v < 0) v + 256 else v
        require(x in 0 until size) { "Ctor case index out of bounds: $x size=$size" }
        return x
    }
}
