import {ref} from 'vue'
import {defineStore} from 'pinia'
import {
    addWebEventListener,
    emitWebEvent,
    makeWebLetter, sendWebCallAndTakeItsReturnValues
} from "@/utils/WinWebAppUtils"
import {type MemberType, type RyoType, type TypeSchema, WinWebAppWindowState} from "@/models/Models"
import NumberEditor from "@/components/Editors/NumberEditor.vue"
import TextEditor from "@/components/Editors/TextEditor.vue"
import BooleanEditor from "@/components/Editors/BooleanEditor.vue"
import EditorHolder from "@/components/EditorHolder.vue"
import ArrayEditor from "@/components/Editors/ArrayEditor.vue"
import FieldEditor from "@/components/Editors/FieldEditor.vue";

const TAG = "AppState"

export const useAppStateStore = defineStore('app-state', () => {
    const dataTypeSchemas = ref<TypeSchema[]>([])
    const isAppWindowMaximized = ref<boolean>(false)

    function getEditorsByRyoType(ryoType: RyoType) {
        const editors = []

        if (ryoType.isArray) {
            editors.push(ArrayEditor)
        } else if (ryoType.baseType) switch (ryoType.baseType.type) {
            case "java.lang.String":
            case "java.lang.Character":
                editors.push(TextEditor)
                break
            case "java.lang.Integer":
            case "java.lang.Long":
            case "java.lang.Float":
            case "java.lang.Double":
            case "java.lang.Short":
            case "java.lang.Byte":
                editors.push(NumberEditor)
                break
            case "java.lang.Void":
                break
            case "java.lang.Boolean":
                editors.push(BooleanEditor)
                break
            default:
                editors.push(FieldEditor)
        }

        return editors
    }

    function getRyoTypeByName(typeName: string): RyoType {
        let isArray = false
        if (typeName.endsWith("[]")) {
            isArray = true
            typeName = typeName.substring(0, typeName.length - 2)
        }

        const baseType = dataTypeSchemas.value.find(schema => schema.type === typeName)

        return {baseType, isArray, typeName}
    }

    function typeSchemaToRyoType(baseType: TypeSchema, isArray: boolean = false): RyoType {
        return {baseType, isArray, typeName: baseType.type}
    }

    function setAppWindowState(state: WinWebAppWindowState) {
        emitWebEvent(makeWebLetter("SetAppWindowState", state))
    }

    function stopApp() {
        emitWebEvent(makeWebLetter("StopApp"))
    }

    async function fetchDataSchemas() {
        dataTypeSchemas.value = await sendWebCallAndTakeItsReturnValues(makeWebLetter('GetAllDataTypes')) as TypeSchema[]
        console.log(TAG, "DataTypeSchemas已拉取", dataTypeSchemas.value)
    }

    // Async Init
    (async () => {
        try {
            console.log(TAG, "Start init")

            await fetchDataSchemas()

            addWebEventListener("AppWindowStateChanged", (args: number[]) => {
                const state = args[0] as WinWebAppWindowState
                console.log(TAG, "AppMaximizationChanged", state, state === WinWebAppWindowState.Maximized)
                isAppWindowMaximized.value = state === WinWebAppWindowState.Maximized
            })
            console.log(TAG, "AppWindowStateChanged监听器已创建")
            emitWebEvent(makeWebLetter('NotifyAppWindowState'))
            console.log(TAG, "已提醒发送AppWindowState")
        } catch (err) {
            console.error(TAG, "遇到问题，App初始化终止，将报错", err);
        } finally {
            console.log(TAG, "Init over")
        }
    })()

    return {
        dataTypeSchemas,
        isAppWindowMaximized,
        fetchDataSchemas,
        getEditorsByRyoType,
        getRyoTypeByName,
        typeSchemaToRyoType,
        setAppWindowState,
        stopApp,
    }
})
