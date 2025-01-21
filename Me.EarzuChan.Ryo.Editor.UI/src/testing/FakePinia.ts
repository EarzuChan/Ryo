import type {Pinia, PiniaPlugin} from "pinia"
import app from "@/App.vue";
import {markRaw, ref} from "vue"
import {createStub} from "@/testing/Stub"
import type {RyoType} from "@/models/AppModels";
import FieldEditor from "@/components/editors/FieldEditor.vue";
import TextEditor from "@/components/editors/TextEditor.vue";

const TAG = "FakePinia"

interface FakePinia extends Pinia {
    _s: any
}

export function createFakePinia(): FakePinia {
    const state = ref({})

    console.log(TAG, "创建FakePinia")

    return {
        state,
        _s: {
            has(id: string) {
                console.log(TAG, "检查是否有", id)

                return true
            },
            get(id: string) {
                console.log(TAG, "获取", id)

                switch (id) {
                    case "app-state":
                        console.log(TAG, "返回FakeAppStateStore")
                        return fakeAppStateStore

                    default:
                        console.log(TAG, "由于没有对应的FakeStore，返回一个Stub")
                        return createStub(id + "-fake")
                }

            },
        },
        use(plugin: PiniaPlugin): Pinia {
            console.log(TAG, "使用插件", plugin)

            return this;
        },
        install: (app) => {
            console.log(TAG, "安装到App", app)
        }
    }
}

const fakeAppStateStore = {
    available: ref(true), // 不可变
    ensureRyoType(type: RyoType, data: any) {
        console.log(TAG, "确保", type, data)

        return true // 不确保
    },
    getEditorsByRyoType(ryoType: RyoType) {
        console.log(TAG, "获取编辑器", ryoType)

        return [markRaw(TextEditor)]
    },
}