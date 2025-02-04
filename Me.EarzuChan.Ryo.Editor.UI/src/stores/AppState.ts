import {computed, ref} from 'vue'
import {defineStore} from 'pinia'
import {makeWebLetter, sendWebCallAndTakeItsReturnValues} from "@/utils/KurisuUtils"
import {type RyoType, type TypeSchema} from "@/models/AppModels"
import NumberEditor from "@/components/editors/NumberEditor.vue"
import TextEditor from "@/components/editors/TextEditor.vue"
import BooleanEditor from "@/components/editors/BooleanEditor.vue"
import ArrayEditor from "@/components/editors/ArrayEditor.vue"
import FieldEditor from "@/components/editors/FieldEditor.vue"
import {setLanguage, sysLang} from "@/misc/I18n"

const TAG = "AppState"

export const useAppStateStore = defineStore('app-state', () => {
        const available = ref(false)

        const preferTesting = ref(false)

        const dataTypeSchemas = ref<TypeSchema[]>([])
        const sidePanelExpanded = ref(true)
        const reffedAppLanguage = ref(sysLang)
        const appLanguage = computed({
            get: () => reffedAppLanguage.value,
            set(value: string) {
                console.log(TAG, "设置语言", value)
                if (setLanguage(value)) reffedAppLanguage.value = value
            }
        })

        // 为提高性能的缓存
        const ryoTypeCache = new Map<string, RyoType>()
        const editorsCache = new Map<string, any[]>()

        function getEditorsByRyoType(ryoType: RyoType) {
            // 生成唯一键：类型名 + 是否数组
            const cacheKey = `${ryoType.typeName}|${ryoType.isArray}`

            if (editorsCache.has(cacheKey)) {
                console.log(TAG, "[Editors 缓存命中]", cacheKey)
                return editorsCache.get(cacheKey)!
            }

            const editors = []

            if (ryoType.isArray) {
                editors.push(ArrayEditor)
            } else if (ryoType.baseType) switch (ryoType.baseType.type) {
                case "java.lang.String":
                    editors.push(TextEditor)
                    break
                case "java.lang.Character":
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

            // 写入缓存
            editorsCache.set(cacheKey, editors)
            return editors
        }

        function getRyoTypeByName(typeName: string): RyoType {
            // 缓存命中检查
            if (ryoTypeCache.has(typeName)) {
                console.log(TAG, "[RyoType 缓存命中]", typeName)
                return ryoTypeCache.get(typeName)!
            }

            // 原有逻辑（处理数组类型）
            let isArray = false
            let processedTypeName = typeName
            if (typeName.endsWith("[]")) {
                isArray = true
                processedTypeName = typeName.slice(0, -2)
            }

            // 查找类型定义
            const baseType = dataTypeSchemas.value.find(schema => schema.type === processedTypeName)
            const ryoType = {baseType, isArray, typeName: processedTypeName}

            // 写入缓存
            ryoTypeCache.set(typeName, ryoType)
            console.log(TAG, "RyoType计算并缓存", typeName)
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
                    return ""
                case "java.lang.Character":
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
                    // TODO：初始化各字段，是否始终可靠？
                    const obj: { [key: string]: any } = {}
                    type.baseType.members?.forEach(field => {
                        obj[field.name] = getInitValue(getRyoTypeByName(field.type))
                    })
                    return obj
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
                    // TODO：是否需要检测字段
                    return typeof data === "object" && !Array.isArray(data)
            }
        }

        async function fetchDataSchemas() {
            dataTypeSchemas.value = await sendWebCallAndTakeItsReturnValues(makeWebLetter('GetAllDataTypes')) as TypeSchema[]

            // 清空缓存
            ryoTypeCache.clear()
            editorsCache.clear()
            console.log(TAG, "因更新数据类型，缓存已清除")

            console.log(TAG, "DataTypeSchemas已拉取", dataTypeSchemas.value)
        }

        // Async Init
        (async () => {
            try {
                console.log(TAG, "Start init")

                await fetchDataSchemas()

                const fetchedLanguage = (await sendWebCallAndTakeItsReturnValues(makeWebLetter("Preference:Language", reffedAppLanguage.value)))[0]
                console.log(TAG, "Language fetched", fetchedLanguage)
                appLanguage.value = fetchedLanguage
                
                const fetchedTesting = (await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppProperty:5")))[0]
                console.log(TAG, "Testing fetched", fetchedTesting)
                preferTesting.value = fetchedTesting

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
            preferTesting,
            dataTypeSchemas,
            fetchDataSchemas,
            appLanguage,
            getInitValue,
            getEditorsByRyoType,
            getRyoTypeByName,
            typeSchemaToRyoType,
            sidePanelExpanded
        }
    }
)
