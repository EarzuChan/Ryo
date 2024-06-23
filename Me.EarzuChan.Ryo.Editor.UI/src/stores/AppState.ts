import {ref} from 'vue'
import {defineStore} from 'pinia'
import {
    addWebEventListener,
    emitWebEvent,
    makeWebLetter,
    sendWebCallAndTakeItsReturnValues
} from "@/utils/KurisuUtils"
import {type MemberType, type RyoType, type TypeSchema} from "@/models/AppModels"
import {KurisuWindowState} from "@/models/KurisuModels"
import NumberEditor from "@/components/Editors/NumberEditor.vue"
import TextEditor from "@/components/Editors/TextEditor.vue"
import BooleanEditor from "@/components/Editors/BooleanEditor.vue"
import ArrayEditor from "@/components/Editors/ArrayEditor.vue"
import FieldEditor from "@/components/Editors/FieldEditor.vue"

const TAG = "AppState"

export const useAppStateStore = defineStore('app-state', () => {
        const available = ref(false)
        const dataTypeSchemas = ref<TypeSchema[]>([])

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

            const ryoType = {baseType, isArray, typeName}
            console.log(TAG, "已获取RyoType", typeName, ryoType)
            return ryoType
        }

        function typeSchemaToRyoType(baseType: TypeSchema, isArray: boolean = false): RyoType {
            return {baseType, isArray, typeName: baseType.type}
        }

        function getInitValue(type: RyoType) {
            if (type.isArray) {
                return []
            } else if (type.baseType) switch (type.baseType.type) {
                case "java.lang.String":
                case "java.lang.Character":
                    return ""
                case "java.lang.Integer":
                case "java.lang.Long":
                case "java.lang.Float":
                case "java.lang.Double":
                case "java.lang.Short":
                case "java.lang.Byte":
                    return 0
                case "java.lang.Void":
                    return null
                case "java.lang.Boolean":
                    return false
                default:
                    // TODO: 初始化各字段？
                    return {}
            }
        }

        function ensureRyoType(type: RyoType, data: any) {
            if (data === null || data === undefined) return true // 无需检查

            if (type.isArray && Array.isArray(data)) return true
            else if (type.baseType) switch (type.baseType.type) {
                case "java.lang.String":
                case "java.lang.Character":
                    return typeof data === "string"
                case "java.lang.Integer":
                case "java.lang.Long":
                case "java.lang.Float":
                case "java.lang.Double":
                case "java.lang.Short":
                case "java.lang.Byte":
                    return typeof data === "number"
                case "java.lang.Void":
                    throw new Error("不是，哥们？！你哪来的Void")
                case "java.lang.Boolean":
                    return typeof data === "boolean"
                default:
                    // TODO: 检测字段？
                    return typeof data === "object" && !Array.isArray(data)
            }
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
                available.value = true
            } catch (err) {
                console.error(TAG, "Init failed", err)
            } finally {
                console.log(TAG, "Init over")
            }
        })()

        return {
            ensureRyoType,
            available,
            dataTypeSchemas,
            fetchDataSchemas,
            getInitValue,
            getEditorsByRyoType,
            getRyoTypeByName,
            typeSchemaToRyoType,
        }
    }
)
