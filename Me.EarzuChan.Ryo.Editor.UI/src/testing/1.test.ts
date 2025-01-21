import {test, expect, describe, beforeEach} from 'vitest'
import {setActivePinia} from "pinia"
import {useAppStateStore} from "@/stores/AppState"
import {createFakePinia} from "@/testing/FakePinia"
import {createStub} from "@/testing/Stub"
import EditorHolder from "@/components/EditorHolder.vue"
import {render} from "vitest-browser-vue"
import Test from "@/testing/Test.vue"
import '../styles/style-default.scss'
import '../styles/color-default.scss'
import {markRaw, ref} from "vue"

describe('第一套测试', () => {
    beforeEach(() => {
        setActivePinia(createFakePinia()) // createTestingPinia())
    })

    test('测试存根', () => {
        // 使用示例
        const stub = createStub();

        let _ = stub.field
        stub.method()
    })

    test('测试模拟StateStore', () => {
        const appState = useAppStateStore();

        let _ = appState.available
    })

    test("测试编辑器", () => {
        const wrapper = render(Test, {
            props: {
                component: markRaw(EditorHolder),
                bindProps: {
                    type: "恩情"
                },
                modelValue: ref("纯真"),
            },
        })
    })
})