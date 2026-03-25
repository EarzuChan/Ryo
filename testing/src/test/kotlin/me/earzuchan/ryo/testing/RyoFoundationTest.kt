package me.earzuchan.ryo.testing

import me.earzuchan.ryo.foundation.Ryo
import me.earzuchan.ryo.foundation.UnknownTypePolicy
import me.earzuchan.ryo.foundation.io.RyoReader
import me.earzuchan.ryo.foundation.io.RyoWriter
import me.earzuchan.ryo.foundation.io.loadFrom
import me.earzuchan.ryo.foundation.path.RyoPath
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.special.FragmentalImage
import me.earzuchan.ryo.foundation.special.fragmentalImageOrNull
import me.earzuchan.ryo.foundation.schema.CtorPrefs
import me.earzuchan.ryo.foundation.schema.ModelSchema
import me.earzuchan.ryo.foundation.schema.ModelSchemaJson
import me.earzuchan.ryo.foundation.schema.modelSchema
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.io.writeTo
import me.earzuchan.ryo.foundation.special.specialValue
import me.earzuchan.ryo.foundation.value.RyoContainerValue
import me.earzuchan.ryo.foundation.value.RyoHostedValue
import me.earzuchan.ryo.foundation.value.RyoMeta
import me.earzuchan.ryo.foundation.value.RyoNullValue
import me.earzuchan.ryo.foundation.value.RyoScalarValue
import me.earzuchan.ryo.foundation.value.RyoUnknownFrame
import me.earzuchan.ryo.foundation.value.RyoUnknownGraph
import me.earzuchan.ryo.foundation.value.RyoUnknownValue
import me.earzuchan.ryo.foundation.value.asHostedOrNull
import me.earzuchan.ryo.foundation.value.asScalarOrNull
import okio.FileSystem
import okio.Path
import okio.Path.Companion.toPath
import org.junit.jupiter.api.Assumptions.assumeTrue
import org.junit.jupiter.api.BeforeAll
import org.junit.jupiter.api.TestInstance
import kotlin.test.Test
import kotlin.test.assertEquals
import kotlin.test.assertFalse
import kotlin.test.assertIs
import kotlin.test.assertNotNull
import kotlin.test.assertTrue
import kotlin.test.assertFailsWith

@TestInstance(TestInstance.Lifecycle.PER_CLASS)
class RyoFoundationTest {
    private lateinit var ryo: Ryo

    private val testEnvDir = "build/testEnv".toPath()
    private val neoFixturePath = testEnvDir / "neoFixture.fs"
    private val legacyFixturePath = testEnvDir / "legacyFixture.fs"
    private val complexNeoFixturePath = testEnvDir / "complexNeoFixture.fs"
    private val complexLegacyFixturePath = testEnvDir / "complexLegacyFixture.fs"
    private val fullFixturePath = testEnvDir / "fullFixture.fs"
    private val uncompactedFixturePath = testEnvDir / "uncompactedFixture.fs"
    private val compactedFixturePath = testEnvDir / "compactedFixture.fs"
    private val refFixturePath = testEnvDir / "refFixture.fs"
    private val initialFixturePath = testEnvDir / "initialFixture.fs"
    private val clonedFixturePath = testEnvDir / "clonedFixture.fs"
    private val afterGcFixturePath = testEnvDir / "afterGcFixture.fs"
    private val textureFixturePath = testEnvDir / "screenbg-right.png.texture"
    private val textureStructurePath = testEnvDir / "texture-structure.txt"
    private val textureFixturePath2 = testEnvDir / "12-paranoid.png.texture"
    private val textureStructurePath2 = testEnvDir / "texture-structure-12-paranoid.txt"
    private val textureBindingPath = testEnvDir / "texture-bindings.txt"

    private val dialogueDescriptorTypeId = "game31.DialogueTree\$DialogueTreeDescriptor"
    private val conversationTypeId = "game31.DialogueTree\$Conversation"
    private val userMessageTypeId = "game31.DialogueTree\$UserMessage"
    private val senderMessageTypeId = "game31.DialogueTree\$SenderMessage"
    private val stringArrayWireTypeId = TypeRefs.arrayOf(TypeRefs.STRING).wireTypeId

    private fun dialogueTreeSchemas(): List<ModelSchema> {
        val str = TypeIds.STRING
        val bool = TypeIds.BOOLEAN
        val float = TypeIds.FLOAT
        val strArr = TypeRefs.arrayOf(TypeRefs.STRING).wireTypeId
        val conversationArr = TypeRefs.arrayOf(TypeRefs.objectType(conversationTypeId)).wireTypeId
        val userMessageArr = TypeRefs.arrayOf(TypeRefs.objectType(userMessageTypeId)).wireTypeId
        val senderMessageArr = TypeRefs.arrayOf(TypeRefs.objectType(senderMessageTypeId)).wireTypeId

        return listOf(
            modelSchema(dialogueDescriptorTypeId, ModelSchema.GloryKind.CTOR) {
                member("dialogueNameSpace", str)
                member("conversationList", conversationArr)
                ctorCase("default", "dialogueNameSpace", "conversationList")
            },
            modelSchema(conversationTypeId, ModelSchema.GloryKind.CTOR) {
                member("tags", strArr)
                member("status", str)
                member("userMessages", userMessageArr)
                member("stateOfDiswatch", bool)
                member("senderMessagers", senderMessageArr)
                member("tagsToUnlock", strArr)
                member("tagsToLock", strArr)
                member("trigger", str)
                ctorCase("default", "tags", "status", "userMessages", "stateOfDiswatch", "senderMessagers", "tagsToUnlock", "tagsToLock", "trigger")
            },
            modelSchema(userMessageTypeId, ModelSchema.GloryKind.CTOR) {
                member("message", str)
                member("isHidden", bool)
                ctorCase("default", "message", "isHidden")
            },
            modelSchema(senderMessageTypeId, ModelSchema.GloryKind.CTOR) {
                member("message", str)
                member("origin", str)
                member("dateText", str)
                member("timeText", str)
                member("idleTime", float)
                member("typingTime", float)
                member("trigger", str)
                member("triggerTime", float)
                ctorCase("default", "message", "origin", "dateText", "timeText", "idleTime", "typingTime", "trigger", "triggerTime")
            }
        )
    }

    private fun expectedComplexDescriptor(): RyoHostedValue {
        val userMessage = ryo.hosted(userMessageTypeId) {
            setScalar("message", "尚勃的臭勒")
            setScalar("isHidden", true)
        }

        val senderMessage = ryo.hosted(senderMessageTypeId) {
            setScalar("message", "肛塞")
            setScalar("origin", "Vamos")
            setScalar("dateText", "雷霆")
            setScalar("timeText", "带派不")
            setScalar("idleTime", 114514f)
            setScalar("typingTime", 666f)
            setScalar("trigger", "老弟")
            setScalar("triggerTime", 1919810f)
        }

        val conversation = ryo.hosted(conversationTypeId) {
            set(
                "tags",
                ryo.container(
                    TypeRefs.arrayOf(TypeRefs.STRING),
                    listOf(ryo.scalar("荣叔叔"))
                )
            )
            setScalar("status", "Vamos")
            set(
                "userMessages",
                ryo.container(
                    TypeRefs.arrayOf(TypeRefs.objectType(userMessageTypeId)),
                    listOf(userMessage)
                )
            )
            setScalar("stateOfDiswatch", false)
            set(
                "senderMessagers",
                ryo.container(
                    TypeRefs.arrayOf(TypeRefs.objectType(senderMessageTypeId)),
                    listOf(senderMessage)
                )
            )
            set(
                "tagsToUnlock",
                ryo.container(
                    TypeRefs.arrayOf(TypeRefs.STRING),
                    listOf(ryo.scalar("秒锁"))
                )
            )
            set(
                "tagsToLock",
                ryo.container(
                    TypeRefs.arrayOf(TypeRefs.STRING),
                    listOf(ryo.scalar("隐世修了臭所"))
                )
            )
            setScalar("trigger", "隐世臭了修所")
        }

        return ryo.hosted(dialogueDescriptorTypeId) {
            setScalar("dialogueNameSpace", "猪头肉")
            set(
                "conversationList",
                ryo.container(
                    TypeRefs.arrayOf(TypeRefs.objectType(conversationTypeId)),
                    listOf(conversation)
                )
            )
        }
    }

    @BeforeAll
    fun setup() {
        FileSystem.SYSTEM.createDirectories(testEnvDir)
        ryo = Ryo { registerSchemas(dialogueTreeSchemas()) }
    }

    @Test
    fun testSimpleRead() {
        assumeTrue(FileSystem.SYSTEM.exists(legacyFixturePath), "Skip testSimpleRead because legacyFixture.fs is missing")

        val loaded = ryo.createVolumeChamber().apply { loadFrom(legacyFixturePath) }

        val state = assertIs<RyoScalarValue>(loaded.get("state"))
        assertEquals(TypeIds.BOOLEAN, state.wireTypeId)
        assertEquals(true, state.value)

        val name = assertIs<RyoScalarValue>(loaded.get("name"))
        assertEquals(TypeIds.STRING, name.wireTypeId)
        assertEquals("senpai", name.value)

        val corp = assertIs<RyoContainerValue>(loaded.get("corp"))
        assertEquals(stringArrayWireTypeId, corp.wireTypeId)
        val corpStrings = corp.elements.map { element ->
            val scalar = assertIs<RyoScalarValue>(element)
            assertEquals(TypeIds.STRING, scalar.wireTypeId)
            scalar.value as String
        }
        assertEquals(listOf("acceed", "coat"), corpStrings)
    }

    @Test
    fun testSimpleWrite() {
        FileSystem.SYSTEM.createDirectories(testEnvDir)

        val volume = ryo.createVolumeChamber().apply {
            set("state", ryo.scalar(true))
            set("name", ryo.scalar("senpai"))
            set(
                "corp", ryo.container(
                    TypeRefs.fromWireTypeId(stringArrayWireTypeId),
                    listOf(
                        ryo.scalar("acceed"),
                        ryo.scalar("coat")
                    )
                )
            )
        }
        volume.writeTo(neoFixturePath)

        assertTrue(FileSystem.SYSTEM.exists(neoFixturePath))
    }

    @Test
    fun testTypedGetAndSafeCastExtensions() {
        val chamber = ryo.createVolumeChamber().apply {
            set("state", ryo.scalar(true))
            set("user", ryo.hosted(userMessageTypeId) {
                setScalar("message", "hello")
                setScalar("isHidden", false)
            })
        }

        val state = chamber.requireAs<RyoScalarValue>("state")
        assertEquals(true, state.value)
        assertEquals(null, chamber.getAs<RyoHostedValue>("state"))
        assertEquals(null, chamber.get("state").asHostedOrNull())
        assertNotNull(chamber.get("state").asScalarOrNull())

        val ex = assertFailsWith<IllegalStateException> { chamber.requireAs<RyoHostedValue>("state") }
        assertEquals(ex.message?.contains("state"), true)
    }

    @Test
    fun testHostedAndContainerShouldValidateImmediatelyByDefault() {
        val hosted = ryo.hosted(userMessageTypeId) { setScalar("message", "auto-init") }
        assertEquals(false, (hosted.members["isHidden"] as RyoScalarValue).value)

        val containerError = assertFailsWith<IllegalArgumentException> { ryo.container(TypeRefs.STRING, listOf(ryo.scalar("a"))) }
        assertEquals(containerError.message?.contains("must be array"), true)
    }

    @Test
    fun testValidateOnCreateCanBeDisabled() {
        val relaxed = Ryo {
            validateOnCreate(false)
            registerSchemas(dialogueTreeSchemas())
        }
        val partial = relaxed.hosted(userMessageTypeId) { setScalar("message", "late validate") }
        relaxed.createVolumeChamber().set("x", partial)
    }

    @Test
    fun testCtorPrefsPersistentAndSessionOverride() {
        val childId = "demo.ChildMulti"
        val rootId = "demo.RootSingle"

        val noPrefs = Ryo {
            registerSchemas(
                listOf(
                    modelSchema(childId, ModelSchema.GloryKind.CTOR) {
                        member("a", TypeIds.INT)
                        member("b", TypeIds.INT)
                        ctorCase("first", "a")
                        ctorCase("second", "b")
                    },
                    modelSchema(rootId, ModelSchema.GloryKind.CTOR) {
                        member("child", childId)
                        ctorCase("default", "child")
                    }
                )
            )
        }
        assertFailsWith<IllegalStateException> { noPrefs.initRyoTypeFor(TypeRefs.objectType(childId)) }

        val persistent = Ryo {
            ctorPrefs { type(childId, "first") }
            registerSchemas(
                listOf(
                    modelSchema(childId, ModelSchema.GloryKind.CTOR) {
                        member("a", TypeIds.INT)
                        member("b", TypeIds.INT)
                        ctorCase("first", "a")
                        ctorCase("second", "b")
                    },
                    modelSchema(rootId, ModelSchema.GloryKind.CTOR) {
                        member("child", childId)
                        ctorCase("default", "child")
                    }
                )
            )
        }

        val childByPersistent = persistent.initRyoTypeFor(TypeRefs.objectType(childId)) as RyoHostedValue
        assertEquals(0, childByPersistent.ctorCaseIndex)

        val rootBySession = persistent.initRyoTypeFor(TypeRefs.objectType(rootId), CtorPrefs.build { path(RyoPath.ROOT.member("child"), "second") }) as RyoHostedValue
        val childBySession = rootBySession.members["child"] as RyoHostedValue
        assertEquals(1, childBySession.ctorCaseIndex)
    }

    @Test
    fun testHostedShouldUseInitAndCtorPrefs() {
        val typeId = "demo.HostedMultiCtor"
        val local = Ryo {
            registerSchema(
                modelSchema(typeId, ModelSchema.GloryKind.CTOR) {
                    member("left", TypeIds.INT)
                    member("right", TypeIds.INT)
                    ctorCase("leftCase", "left")
                    ctorCase("rightCase", "right")
                }
            )
        }

        val hosted = local.hosted(typeId, ctorPrefs = CtorPrefs.build { type(typeId, "rightCase") }) { setScalar("left", 7) }
        assertEquals(1, hosted.ctorCaseIndex)
        assertEquals(7, (hosted.members["left"] as RyoScalarValue).value)
        assertEquals(0, (hosted.members["right"] as RyoScalarValue).value)
    }

    @Test
    fun testSchemaJsonDslAndBuildNormalization() {
        val json = """
            [
              {
                "modelId":"demo.AutoCtor",
                "kind":"CTOR",
                "members":[
                  {"name":"a","wireTypeId":"java.lang.Integer"},
                  {"name":"b","wireTypeId":"java.lang.String","nullable":true}
                ]
              }
            ]
        """.trimIndent()

        val parsed = ModelSchemaJson.parseList(json)
        assertEquals(listOf("a", "b"), parsed.single().ctorCases.single().args)

        val dsl = modelSchema("demo.DslCtor", ModelSchema.GloryKind.CTOR) {
            member("x", TypeIds.INT)
            member("y", TypeIds.STRING, nullable = true)
        }
        assertEquals(listOf("x", "y"), dsl.ctorCases.single().args)

        val fromBuilder = Ryo {
            registerSchemasJson(json)
            registerSchema(dsl)
        }
        assertEquals(listOf("a", "b"), fromBuilder.requireSchema("demo.AutoCtor").ctorCases.single().args)
        assertEquals(listOf("x", "y"), fromBuilder.requireSchema("demo.DslCtor").ctorCases.single().args)
    }

    @Test
    fun testNullValueShouldRoundTripAsRyoNullValue() {
        val local = Ryo {
            registerSchema(
                modelSchema("demo.NullableHolder", ModelSchema.GloryKind.FIELD) {
                    member("name", TypeIds.STRING, nullable = true)
                }
            )
        }
        val value = local.hosted("demo.NullableHolder") { setNull("name", TypeIds.STRING) }
        val loaded = local.createVolumeChamber().apply { set("n", value) }.let { chamber -> local.createVolumeChamber().apply { loadFromBytes(chamber.saveToBytes()) }.requireAs<RyoHostedValue>("n") }
        assertIs<RyoNullValue>(loaded.members["name"])
    }

    @Test
    fun testUnknownGraphShouldMoveAcrossVolumesWithoutDecode() {
        val opaque = Ryo { unknownTypePolicy(UnknownTypePolicy.OPAQUE) }

        val unknown = RyoUnknownValue(
            typeRef = TypeRefs.objectType("demo.unknown.Root"),
            gloryId = "demo.UnknownGlory",
            opaquePayload = byteArrayOf(1, 2, 3),
            metas = listOf(RyoMeta.create(200, 3)),
            graph = RyoUnknownGraph(
                rootId = 100,
                frames = listOf(
                    RyoUnknownFrame(100, "demo.unknown.Root", "demo.UnknownGlory", byteArrayOf(1, 2, 3), listOf(RyoMeta.create(200, 3))),
                    RyoUnknownFrame(200, "demo.unknown.Child", "demo.UnknownGloryChild", byteArrayOf(9), emptyList())
                )
            )
        )

        val src = opaque.createVolumeChamber().apply { set("u", unknown) }
        val exported = src.requireAs<RyoUnknownValue>("u")

        val dst = opaque.createVolumeChamber().apply { set("u", exported) }
        val moved = dst.requireAs<RyoUnknownValue>("u")
        val graph = assertNotNull(moved.graph)

        assertEquals(2, graph.frames.size)
        val root = graph.frame(graph.rootId)
        val refMeta = root.metas.single()

        assertEquals(3, refMeta.type)
        assertTrue(graph.frames.any { it.nodeId == refMeta.payload })

        val remappedIds = graph.frames.map { it.nodeId }.sorted()
        assertEquals(listOf(0, 1), remappedIds, "Exported graph IDs must be perfectly normalized to 0-based indices!")
    }

    @Test
    fun testComplexRead() {
        assumeTrue(FileSystem.SYSTEM.exists(complexLegacyFixturePath), "Skip testComplexRead because complexLegacyFixture.fs is missing")
        val loaded = ryo.createVolumeChamber().apply { loadFrom(complexLegacyFixturePath) }
        val testValue = assertIs<RyoHostedValue>(loaded.get("test"))
        assertEquals(expectedComplexDescriptor(), testValue)
    }

    @Test
    fun testComplexWrite() {
        val volume = ryo.createVolumeChamber().apply { set("test", expectedComplexDescriptor()) }
        volume.writeTo(complexNeoFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(complexNeoFixturePath))
    }

    @Test
    fun testCtorInferShouldPreferCaseThatCoversProvidedMembers() {
        val typeId = "demo.CtorInfer"
        val local = Ryo {
            registerSchema(modelSchema(typeId, ModelSchema.GloryKind.CTOR) {
                member("a", TypeIds.INT)
                member("b", TypeIds.INT)
                ctorCase("onlyA", "a")
                ctorCase("aAndB", "a", "b")
            })
        }
        val value = local.hosted(typeId, ctorPrefs = CtorPrefs.build { type(typeId, "aAndB") }) {
            setScalar("a", 1)
            setScalar("b", 2)
        }
        val loaded = local.createVolumeChamber().apply { set("x", value) }.let { written ->
            local.createVolumeChamber().apply { loadFromBytes(written.saveToBytes()) }.get("x")
        }
        val hosted = assertIs<RyoHostedValue>(loaded)
        assertEquals(setOf("a", "b"), hosted.members.keys)
        assertEquals(1, assertIs<RyoScalarValue>(hosted.members["a"]).value)
        assertEquals(2, assertIs<RyoScalarValue>(hosted.members["b"]).value)
    }

    @Test
    fun testFixedStringShouldUseUtf8ByteLength() {
        val header = "荣🐉"
        val bytes = RyoWriter().writeFixedString(header).toByteArray()
        assertTrue(RyoReader(bytes).checkFixedString(header))
        assertFalse(RyoReader(bytes).checkFixedString("誉🐉"))
    }

    @Test
    fun testFragmentalImageRoundTrip() {
        val fi = FragmentalImage(
            clipSize = 4,
            levels = listOf(
                FragmentalImage.Level(
                    width = 5,
                    height = 3,
                    format = FragmentalImage.PixelFormat.RGB888,
                    fragments = listOf(
                        FragmentalImage.Fragment(4, 3, FragmentalImage.PixelFormat.RGB888, FragmentalImage.Encoding.RAW, ByteArray(4 * 3 * 3) { it.toByte() }),
                        FragmentalImage.Fragment(1, 3, FragmentalImage.PixelFormat.RGB888, FragmentalImage.Encoding.JPEG, byteArrayOf(0x01, 0x23, 0x45, 0x67))
                    )
                ),
                FragmentalImage.Level(
                    width = 2,
                    height = 2,
                    format = FragmentalImage.PixelFormat.RGBA8888,
                    fragments = listOf(
                        FragmentalImage.Fragment(2, 2, FragmentalImage.PixelFormat.RGBA8888, FragmentalImage.Encoding.RAW, ByteArray(2 * 2 * 4) { (it * 3).toByte() })
                    )
                )
            )
        )

        val bytes = ryo.createVolumeChamber().apply { set("fi", fi.specialValue()) }.saveToBytes()
        val loadedFi = ryo.createVolumeChamber().apply { loadFromBytes(bytes) }.get("fi").fragmentalImageOrNull()
        assertNotNull(loadedFi)
        assertFragmentalImageEquals(fi, loadedFi)
    }

    @Test
    fun testGc(){
        val chamber = ryo.createVolumeChamber().apply {
            set("first",ryo.scalar("这一块那一块"))
            set("middle",ryo.container(
                TypeRefs.arrayOf(TypeRefs.objectType(userMessageTypeId)),
                listOf(ryo.hosted(userMessageTypeId){
                    setScalar("message", "尚勃的臭勒")
                    setScalar("isHidden", true)
                })
            ))
            set("last",ryo.scalar(1919810))
        }
        chamber.writeTo(fullFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(fullFixturePath))

        chamber.delete("middle")
        chamber.writeTo(uncompactedFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(uncompactedFixturePath))

        chamber.collectGarbage()
        chamber.writeTo(compactedFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(compactedFixturePath))

        val refChamber = ryo.createVolumeChamber().apply {
            set("first",ryo.scalar("这一块那一块"))
            set("last",ryo.scalar(1919810))
        }
        refChamber.writeTo(refFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(refFixturePath))
    }

    @Test
    fun testClone() {
        val chamber = ryo.createVolumeChamber().apply {
            // 构造一个包含复杂层级和子节点的对象（RyoContainer 包裹 RyoHosted）
            set("original_target", ryo.container(
                TypeRefs.arrayOf(TypeRefs.objectType(userMessageTypeId)),
                listOf(ryo.hosted(userMessageTypeId) {
                    setScalar("message", "尚勃的臭勒")
                    setScalar("isHidden", true)
                })
            ))
            set("ambient_data", ryo.scalar(114514))
        }

        // 初始状态写入
        chamber.writeTo(initialFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(initialFixturePath))

        // 核心测试：执行 Chamber 的底层深拷贝
        chamber.clone("original_target", "cloned_target")

        // 验证拷贝是否成功（读取测试）
        val originalVal = chamber.get("original_target")
        val clonedVal = chamber.get("cloned_target")
        assertNotNull(clonedVal, "Cloned value should not be null")

        // 由于是纯底层拷贝，反序列化出来的结构应该完全一致
        assertEquals(originalVal, clonedVal, "Deep copied content must match the original") // 这个多少有点Hack，应该拆结构来对比

        chamber.writeTo(clonedFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(clonedFixturePath))

        // 极端测试：解耦与 GC 生存测试
        // 我们将原目标直接删除
        chamber.delete("original_target")

        // 此时，如果深拷贝的底层引用图（metaHeap 和 rootIds）没有正确独立，那么接下来的 GC 可能会把 cloned_target 的子节点也错误地当作垃圾回收掉！
        chamber.collectGarbage()

        // GC 后的存活性验证
        val survivingClone = chamber.get("cloned_target")
        assertNotNull(survivingClone, "Cloned value must survive after the original is deleted and GC is collected")

        // 再次确认数据没有在 GC 压缩期间发生损坏或错位
        assertEquals(clonedVal.toString(), survivingClone.toString(), "Data must remain fully intact after GC defragmentation")

        // 顺便确认无辜的旁观者数据还活着
        assertNotNull(chamber.get("ambient_data"))

        chamber.writeTo(afterGcFixturePath)
        assertTrue(FileSystem.SYSTEM.exists(afterGcFixturePath))
    }

    private fun assertFragmentalImageEquals(expected: FragmentalImage, actual: FragmentalImage) {
        assertEquals(expected.clipSize, actual.clipSize)
        assertEquals(expected.levels.size, actual.levels.size)
        expected.levels.zip(actual.levels).forEach { (el, al) ->
            assertEquals(el.width, al.width)
            assertEquals(el.height, al.height)
            assertEquals(el.format, al.format)
            assertEquals(el.fragments.size, al.fragments.size)
            el.fragments.zip(al.fragments).forEach { (ef, af) ->
                assertEquals(ef.width, af.width)
                assertEquals(ef.height, af.height)
                assertEquals(ef.format, af.format)
                assertEquals(ef.encoding, af.encoding)
                assertTrue(ef.bytes.contentEquals(af.bytes))
            }
        }
    }

    @Test
    fun inspectTextureFixtureStructure() {
        assumeTrue(FileSystem.SYSTEM.exists(textureFixturePath), "Skip inspectTextureFixtureStructure because texture fixture is missing")
        val texture = Ryo().createTextureChamber().apply { loadFrom(textureFixturePath) }
        val sb = StringBuilder().appendLine("groupCount=${texture.groupCount}")
        for (gi in 0 until texture.groupCount) {
            val items = texture.getGroup(gi)
            sb.appendLine("group[$gi].entryCount=${items.size}")
            items.forEachIndexed { ei, value ->
                val fi = value.fragmentalImageOrNull()
                if (fi == null) sb.appendLine("group[$gi].entry[$ei]=${value::class.simpleName} wire=${value.wireTypeId}")
                else {
                    sb.appendLine("group[$gi].entry[$ei]=FragmentalImage clipSize=${fi.clipSize} levels=${fi.levels.size}")
                    fi.levels.forEachIndexed { li, lvl -> sb.appendLine("group[$gi].entry[$ei].level[$li]=${lvl.width}x${lvl.height} format=${lvl.format} fragments=${lvl.fragments.size}") }
                }
            }
        }
        FileSystem.SYSTEM.write(textureStructurePath) { writeUtf8(sb.toString()) }
        assertTrue(sb.isNotEmpty())
    }

    @Test
    fun inspectTextureFixtureStructureParanoid() {
        assumeTrue(FileSystem.SYSTEM.exists(textureFixturePath2), "Skip inspectTextureFixtureStructureParanoid because texture fixture is missing")
        val texture = Ryo().createTextureChamber().apply { loadFrom(textureFixturePath2) }
        val sb = StringBuilder().appendLine("groupCount=${texture.groupCount}")
        for (gi in 0 until texture.groupCount) {
            val items = texture.getGroup(gi)
            sb.appendLine("group[$gi].entryCount=${items.size}")
            items.forEachIndexed { ei, value ->
                val fi = value.fragmentalImageOrNull()
                if (fi == null) sb.appendLine("group[$gi].entry[$ei]=${value::class.simpleName} wire=${value.wireTypeId}")
                else {
                    sb.appendLine("group[$gi].entry[$ei]=FragmentalImage clipSize=${fi.clipSize} levels=${fi.levels.size}")
                    fi.levels.forEachIndexed { li, lvl -> sb.appendLine("group[$gi].entry[$ei].level[$li]=${lvl.width}x${lvl.height} format=${lvl.format} fragments=${lvl.fragments.size}") }
                }
            }
        }
        FileSystem.SYSTEM.write(textureStructurePath2) { writeUtf8(sb.toString()) }
        assertTrue(sb.isNotEmpty())
    }

    @Test
    fun inspectTextureBindingWireTypes() {
        val sb = StringBuilder()
        dumpTextureBindings(textureFixturePath, "screenbg-right", sb)
        dumpTextureBindings(textureFixturePath2, "12-paranoid", sb)
        FileSystem.SYSTEM.write(textureBindingPath) { writeUtf8(sb.toString()) }
        assertTrue(sb.isNotEmpty())
    }

    private fun dumpTextureBindings(path: Path, label: String, sb: StringBuilder) {
        if (!FileSystem.SYSTEM.exists(path)) {
            sb.appendLine("$label:missing")
            return
        }
        val texture = Ryo().createTextureChamber().apply { loadFrom(path) }
        val frameClass = texture::class.java.superclass
        val bindingsField = frameClass.getDeclaredField("gloryBindings").apply { isAccessible = true }
        val bindings = bindingsField.get(texture) as List<*>
        sb.appendLine("$label.bindings=${bindings.size}")
        bindings.forEachIndexed { idx, any ->
            val clazz = any!!::class.java
            val wire = clazz.getDeclaredField("wireTypeId").apply { isAccessible = true }.get(any)
            val glory = clazz.getDeclaredField("gloryId").apply { isAccessible = true }.get(any)
            sb.appendLine("$label.binding[$idx].wire=$wire glory=$glory")
        }
    }
}
