package me.earzuchan.ryo.testing

import kotlinx.coroutines.*
import kotlinx.coroutines.flow.filterNotNull
import kotlinx.coroutines.flow.map
import kotlinx.coroutines.flow.take
import kotlinx.coroutines.flow.toList
import me.earzuchan.ryo.foundation.Ryo
import me.earzuchan.ryo.foundation.schema.ModelSchema
import me.earzuchan.ryo.foundation.type.TypeIds
import me.earzuchan.ryo.foundation.type.TypeRefs
import me.earzuchan.ryo.foundation.util.RgbImage
import me.earzuchan.ryo.foundation.value.RyoHostedValue
import me.earzuchan.ryo.foundation.value.asScalarOrNull
import me.earzuchan.ryo.modern.ModernizedRyo
import me.earzuchan.ryo.modern.modernize
import me.earzuchan.ryo.modern.session.ContainerCursor
import me.earzuchan.ryo.modern.session.HostedCursor
import me.earzuchan.ryo.modern.session.ScalarCursor
import me.earzuchan.ryo.modern.session.ValuePath
import me.earzuchan.ryo.modern.texture.createCanonicalTexture
import me.earzuchan.ryo.modern.texture.isCanonical
import me.earzuchan.ryo.modern.texture.requireCanonicalMainImage
import me.earzuchan.ryo.modern.texture.setCanonicalMainImage
import org.junit.jupiter.api.BeforeAll
import org.junit.jupiter.api.TestInstance
import kotlin.test.*

@TestInstance(TestInstance.Lifecycle.PER_CLASS)
class RyoModernTest {
    companion object {
        private const val PROFILE_TYPE_ID = "demo.Profile"
        private const val ROOT_TYPE_ID = "demo.Root"
    }

    private lateinit var ryo: Ryo
    private lateinit var modern: ModernizedRyo

    @BeforeAll
    fun setup() {
        ryo = Ryo {
            registerSchemas(
                listOf(
                    ModelSchema(
                        modelId = PROFILE_TYPE_ID,
                        kind = ModelSchema.GloryKind.CTOR,
                        members = listOf(
                            ModelSchema.ModelMember("title", TypeIds.STRING),
                            ModelSchema.ModelMember("active", TypeIds.BOOLEAN)
                        ),
                        ctorCases = listOf(ModelSchema.CtorCase("default", listOf("title", "active")))
                    ),
                    ModelSchema(
                        modelId = ROOT_TYPE_ID,
                        kind = ModelSchema.GloryKind.CTOR,
                        members = listOf(
                            ModelSchema.ModelMember("name", TypeIds.STRING),
                            ModelSchema.ModelMember("profile", PROFILE_TYPE_ID),
                            ModelSchema.ModelMember("tags", TypeRefs.arrayOf(TypeRefs.STRING).wireTypeId)
                        ),
                        ctorCases = listOf(ModelSchema.CtorCase("default", listOf("name", "profile", "tags")))
                    )
                )
            )
        }

        modern = ryo.modernize()
    }

    private fun seedRoot() = ryo.hosted(ROOT_TYPE_ID) {
        setScalar("name", "senpai")
        set(
            "profile",
            ryo.hosted(PROFILE_TYPE_ID) {
                setScalar("title", "yaju")
                setScalar("active", true)
            }
        )
        set(
            "tags",
            ryo.container(TypeRefs.arrayOf(TypeRefs.STRING), listOf(ryo.scalar("alpha"), ryo.scalar("beta")))
        )
    }

    @Test
    fun `Volume should perfectly isolate sandbox until explicit commit`() {
        val chamber = ryo.createVolumeChamber().apply { set("demo", seedRoot()) }
        val volume = modern.manage(chamber)

        // Checkout：开辟沙箱
        val session = requireNotNull(volume.checkout("demo"))
        val root = session.getRootCursorAs<HostedCursor>()

        // 沙箱内疯狂修改
        root["name"] = "neo"
        root["profile"].asHosted()["title"] = "matrix"

        // 验证隔离性：大盘（Volume）绝对静止，一丝波澜都没有
        val rawVolumeData = volume.requireSnapshotAs<RyoHostedValue>("demo")
        assertEquals("senpai", rawVolumeData.members["name"].asScalarOrNull()?.value, "Volume should not be polluted by uncommitted session!")
        assertEquals(0L, volume.entries.value["demo"], "Revision should not increase before commit!")

        // Commit：手动写回，一锤定音
        volume.commit("demo", session)

        // 验证写回成功
        assertEquals("neo", volume.requireSnapshotAs<RyoHostedValue>("demo").members["name"].asScalarOrNull()?.value)
        assertEquals(1L, volume.entries.value["demo"], "Revision must bump after commit!")
    }

    @Test
    fun `Precise path dispatcher should only wake up affected UI flows`() = runBlocking {
        val session = modern.newSession(seedRoot())
        val root = session.getRootCursorAs<HostedCursor>()
        val profile = root["profile"].asHosted()

        // 提取各种粒度的 Flow
        val titleFlow = profile["title"].stateFlow
        val activeFlow = profile["active"].stateFlow
        val nameFlow = root["name"].stateFlow

        // 收集发射事件
        val activeEvents = mutableListOf<Boolean>()
        val titleEvents = mutableListOf<String>()
        val nameEvents = mutableListOf<String>()

        val jobs = listOf(
            launch(start = CoroutineStart.UNDISPATCHED) { activeFlow.filterNotNull().map { (it.asScalarOrNull()?.value ?: error("active is not scalar")) as Boolean }.collect { activeEvents += it } },
            launch(start = CoroutineStart.UNDISPATCHED) { titleFlow.filterNotNull().map { (it.asScalarOrNull()?.value ?: error("title is not scalar")) as String }.collect { titleEvents += it } },
            launch(start = CoroutineStart.UNDISPATCHED) { nameFlow.filterNotNull().map { (it.asScalarOrNull()?.value ?: error("name is not scalar")) as String }.collect { nameEvents += it } }
        )
        yield()

        // 🎯 核心操作：仅修改 active 字段
        profile["active"] = false
        yield()

        jobs.forEach { it.cancelAndJoin() }

        // 验证结果：只有 active 触发了流，旁支的 title 和 name 纹丝不动！
        // (注意 StateFlow 会发射一次初始值)
        assertEquals(listOf(true, false), activeEvents, "Active flow should emit new value")
        assertEquals(listOf("yaju"), titleEvents, "Title flow MUST NOT wake up when sibling active is changed!")
        assertEquals(listOf("senpai"), nameEvents, "Name flow MUST NOT wake up when deep nested profile is changed!")
    }

    @Test
    fun `Transaction block should batch mutations into a single atomic history record`() {
        val session = modern.newSession(seedRoot())
        val root = session.getRootCursorAs<HostedCursor>()

        // 使用高墙闭包进行事务批处理
        root.edit {
            this["name"] = "batched"
            nested("profile")["title"] = "batched_title"
            nested("profile")["active"] = false
        }

        // 验证修改生效
        assertEquals("batched", root["name"].snapshot.asScalarOrNull()?.value)
        assertEquals(false, root["profile"].asHosted()["active"].snapshot.asScalarOrNull()?.value)

        // 🎯 核心验证：执行了 3 次修改，但只需要 Undo 一次！
        assertTrue(session.undo())

        assertEquals("senpai", root["name"].snapshot.asScalarOrNull()?.value)
        assertEquals("yaju", root["profile"].asHosted()["title"].snapshot.asScalarOrNull()?.value)
        assertEquals(true, root["profile"].asHosted()["active"].snapshot.asScalarOrNull()?.value)

        // 再 Redo 一次，验证历史完整性
        assertTrue(session.redo())
        assertEquals("batched", root["name"].snapshot.asScalarOrNull()?.value)
    }

    @Test
    fun `Too beautiful API should feel like operating native Kotlin objects`() {
        val session = modern.newSession(seedRoot())
        val root = session.getRootCursorAs<HostedCursor>()
        val profile = root["profile"].asHosted()

        // 没有任何 assertIs 强转，没有 setScalar 样板代码，直接赋值！
        root["name"] = "kotlin"
        profile["title"] = "multiplatform"
        profile["active"] = false

        // 验证自动装箱和寻址生效
        assertEquals("kotlin", session.resolve(ValuePath.ROOT.member("name")).asScalarOrNull()?.value)
        assertEquals("multiplatform", session.resolve(ValuePath.ROOT.member("profile").member("title")).asScalarOrNull()?.value)
        assertEquals(false, session.resolve(ValuePath.ROOT.member("profile").member("active")).asScalarOrNull()?.value)
    }

    @Test
    fun `ValuePath intersection logic must correctly identify relationships`() {
        val rootPath = ValuePath.ROOT
        val profilePath = rootPath.member("profile")
        val titlePath = profilePath.member("title")
        val activePath = profilePath.member("active")

        // 父亲改变，儿子必受影响（因为父亲换了新引用）
        assertTrue(titlePath.isAffectedBy(profilePath))

        // 儿子改变，父亲必受影响（因为触发了沿途的 Copy-On-Write）
        assertTrue(profilePath.isAffectedBy(titlePath))

        // 兄弟改变，互不影响（这是性能优化的核心！）
        assertFalse(titlePath.isAffectedBy(activePath))
        assertFalse(activePath.isAffectedBy(titlePath))

        // 旁支改变，互不影响
        assertFalse(titlePath.isAffectedBy(rootPath.member("name")))
    }

    @Test
    fun `Root cursor should match root value kind`() {
        val scalarSession = modern.newSession(ryo.scalar(114))
        assertIs<ScalarCursor>(scalarSession.getRootCursorAs<ScalarCursor>())
        assertFailsWith<IllegalStateException> { scalarSession.getRootCursorAs<HostedCursor>() }
        val containerSession = modern.newSession(ryo.container(TypeRefs.arrayOf(TypeRefs.INT), listOf(ryo.scalar(1))))
        assertIs<ContainerCursor>(containerSession.getRootCursorAs<ContainerCursor>())
    }

    @Test
    fun `Container transaction with member path mismatch should fail with clear error`() {
        val session = modern.newSession(ryo.container(TypeRefs.arrayOf(TypeRefs.INT), listOf(ryo.scalar(1))))
        val root = session.getRootCursorAs<ContainerCursor>()
        val ex = assertFailsWith<IllegalStateException> { root.edit { this["bad"] = 7 } }
        assertTrue(ex.message?.contains("Path mismatch") == true)
    }

    @Test
    fun `Canonical texture helpers should keep detached edits until commit`() {
        val base = rgbaImage(2, 2, 255, 0, 0, 255, 0, 255, 0, 255, 0, 0, 255, 255, 255, 255, 255, 255)
        val edited = rgbaImage(2, 2, 0, 0, 0, 255, 10, 20, 30, 255, 40, 50, 60, 255, 70, 80, 90, 255)
        val texture = modern.createCanonicalTexture(base)
        assertTrue(texture.isCanonical())
        assertRgbEquals(base, texture.requireCanonicalMainImage())

        val handle = texture.checkoutFragmentalImage(0)
        handle.setMainImage(edited)
        assertRgbEquals(base, texture.requireCanonicalMainImage())

        val rev0 = texture.groups.value[0]
        texture.commitFragmentalImage(0, handle)
        assertEquals(rev0 + 1, texture.groups.value[0])
        assertRgbEquals(edited, texture.requireCanonicalMainImage())
    }

    @Test
    fun `FragmentalImageSession reactive flows should emit deep updates and undo`() = runBlocking {
        val texture = modern.createCanonicalTexture(rgbaImage(2, 2, 1, 2, 3, 255, 4, 5, 6, 255, 7, 8, 9, 255, 10, 11, 12, 255))
        val handle = texture.checkoutFragmentalImage(0)
        val widthsDeferred = async(start = CoroutineStart.UNDISPATCHED) { handle.state.map { it.width }.take(3).toList() }
        handle.setMainImage(rgbaImage(1, 1, 9, 8, 7, 255))
        yield()
        handle.undo()
        yield()
        val widths = withTimeout(3_000) { widthsDeferred.await() }
        assertEquals(listOf(2, 1, 2), widths)
    }

    @Test
    fun `Canonical texture setter should bump revision and replace image`() {
        val texture = modern.createCanonicalTexture(rgbaImage(1, 1, 11, 22, 33, 255))
        val rev0 = texture.groups.value[0]
        texture.setCanonicalMainImage(rgbaImage(1, 1, 44, 55, 66, 255))
        assertEquals(rev0 + 1, texture.groups.value[0])
        assertRgbEquals(rgbaImage(1, 1, 44, 55, 66, 255), texture.requireCanonicalMainImage())
    }
    private fun rgbaImage(width: Int, height: Int, vararg rgba: Int) = RgbImage(width, height, 4, ByteArray(rgba.size) { rgba[it].toByte() })
    private fun assertRgbEquals(expected: RgbImage, actual: RgbImage) {
        assertEquals(expected.width, actual.width)
        assertEquals(expected.height, actual.height)
        assertTrue(toRgbaBytes(expected).contentEquals(toRgbaBytes(actual)))
    }
    private fun toRgbaBytes(image: RgbImage): ByteArray = when (image.channels) {
        4 -> image.bytes
        3 -> ByteArray(image.width * image.height * 4).also { out ->
            var si = 0
            var di = 0
            while (si < image.bytes.size) {
                out[di++] = image.bytes[si++]
                out[di++] = image.bytes[si++]
                out[di++] = image.bytes[si++]
                out[di++] = 0xFF.toByte()
            }
        }
        else -> error("Unsupported channels: ${image.channels}")
    }
}
