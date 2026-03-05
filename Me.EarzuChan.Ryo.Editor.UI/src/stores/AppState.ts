import {computed, ref} from 'vue'
import {defineStore} from 'pinia'
import {makeWebLetter, sendWebCallAndTakeItsReturnValues} from "@/utils/KurisuUtils"
import {type EditorDescriptor, type RyoType, type TypeSchema} from "@/models/AppModels"
import NumberEditor from "@/components/editors/NumberEditor.vue"
import TextEditor from "@/components/editors/TextEditor.vue"
import BooleanEditor from "@/components/editors/BooleanEditor.vue"
import ArrayEditor from "@/components/editors/ArrayEditor.vue"
import FieldEditor from "@/components/editors/FieldEditor.vue"
import {setLanguage, sysLang} from "@/misc/I18n"

const TAG = "AppState"

export interface ValidationIssue {
    path: string
    message: string
}

export interface ValidationResult {
    valid: boolean
    issues: ValidationIssue[]
}

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
        const editorsCache = new Map<string, EditorDescriptor[]>()

        function createEditorsByRyoType(ryoType: RyoType): EditorDescriptor[] {
            const editors: EditorDescriptor[] = []

            if (ryoType.isArray) {
                editors.push({id: "generic.array", title: "数组编辑器", component: ArrayEditor, priority: 10})
                return editors
            }

            if (!ryoType.baseType) return editors

            switch (ryoType.baseType.type) {
                case "java.lang.String":
                case "java.lang.Character":
                    editors.push({id: "generic.text", title: "文本编辑器", component: TextEditor, priority: 10})
                    break
                case "java.lang.Integer":
                case "java.lang.Long":
                case "java.lang.Float":
                case "java.lang.Double":
                case "java.lang.Short":
                case "java.lang.Byte":
                    editors.push({id: "generic.number", title: "数字编辑器", component: NumberEditor, priority: 10})
                    break
                case "java.lang.Void":
                    break
                case "java.lang.Boolean":
                    editors.push({id: "generic.boolean", title: "布尔编辑器", component: BooleanEditor, priority: 10})
                    break
                default:
                    editors.push({id: "generic.field", title: "对象编辑器", component: FieldEditor, priority: 10})
                    break
            }

            editors.sort((a, b) => (b.priority ?? 0) - (a.priority ?? 0))
            return editors
        }

        function getEditorsByRyoType(ryoType: RyoType) {
            // 生成唯一键：类型名 + 是否数组
            const cacheKey = `${ryoType.typeName}|${ryoType.isArray}`

            if (editorsCache.has(cacheKey)) {
                console.log(TAG, "[Editors 缓存命中]", cacheKey)
                return editorsCache.get(cacheKey)!
            }

            const editors = createEditorsByRyoType(ryoType)

            // 写入缓存
            editorsCache.set(cacheKey, editors)
            return editors
        }

        function getRyoTypeByDataTypeName(dataTypeName: string): RyoType {
            // 缓存命中检查
            if (ryoTypeCache.has(dataTypeName)) {
                console.log(TAG, "[RyoType 缓存命中]", dataTypeName)
                return ryoTypeCache.get(dataTypeName)!
            }

            // 原有逻辑（处理数组类型）
            let isArray = false
            let processedTypeName = dataTypeName
            if (dataTypeName.endsWith("[]")) {
                isArray = true
                processedTypeName = dataTypeName.slice(0, -2)
            }

            // 查找类型定义
            const baseType = dataTypeSchemas.value.find(schema => schema.type === processedTypeName)
            const ryoType = {baseType, isArray, typeName: processedTypeName}

            // 写入缓存
            ryoTypeCache.set(dataTypeName, ryoType)
            console.log(TAG, "RyoType计算并缓存", dataTypeName)
            return ryoType
        }

        function getDataTypeNameByRyoType(ryoType: RyoType): string {
            // 先尝试从缓存中查找
            for (const [cachedName, cachedType] of ryoTypeCache.entries()) {
                if (cachedType === ryoType) {
                    console.log(TAG, "[RyoType转Name 缓存命中]", cachedName)
                    return cachedName
                }
            }

            // 缓存未命中，计算类型名
            const typeName = ryoType.isArray ? `${ryoType.typeName}[]` : ryoType.typeName

            // 写入缓存
            ryoTypeCache.set(typeName, ryoType)
            console.log(TAG, "RyoType转DataTypeName计算并缓存", typeName)

            return typeName
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
                    // CHECK：初始化各字段，是否始终可靠？
                    const obj: { [key: string]: any } = {}
                    type.baseType.members?.forEach(field => {
                        obj[field.name] = getInitValue(getRyoTypeByDataTypeName(field.type))
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
                    // CHECK：是否需要检测字段
                    return typeof data === "object" && !Array.isArray(data)
            }
        }

        function validateDataByRyoType(type: RyoType, data: any): ValidationResult {
            const issues: ValidationIssue[] = []

            const walk = (t: RyoType, val: any, path: string) => {
                if (val === null || val === undefined) return

                if (t.isArray) {
                    if (!Array.isArray(val)) {
                        issues.push({path, message: `期望数组，实际为 ${typeof val}`})
                        return
                    }

                    const sub = getRyoTypeByDataTypeName(t.typeName)
                    val.forEach((it: any, index: number) => walk(sub, it, `${path}[${index}]`))
                    return
                }

                const baseType = t.baseType
                if (!baseType) return

                switch (baseType.type) {
                    case "java.lang.String":
                    case "java.lang.Character":
                        if (typeof val !== "string") issues.push({path, message: `期望字符串，实际为 ${typeof val}`})
                        return
                    case "java.lang.Integer":
                    case "java.lang.Long":
                    case "java.lang.Float":
                    case "java.lang.Double":
                    case "java.lang.Short":
                    case "java.lang.Byte":
                        if (typeof val !== "number" || Number.isNaN(val)) issues.push({path, message: `期望数字，实际为 ${typeof val}`})
                        return
                    case "java.lang.Boolean":
                        if (typeof val !== "boolean") issues.push({path, message: `期望布尔值，实际为 ${typeof val}`})
                        return
                    case "java.lang.Void":
                        return
                    default:
                        if (typeof val !== "object" || Array.isArray(val)) {
                            issues.push({path, message: `期望对象，实际为 ${typeof val}`})
                            return
                        }

                        baseType.members?.forEach(member => {
                            const nextType = getRyoTypeByDataTypeName(member.type)
                            walk(nextType, val[member.name], `${path}.${member.name}`)
                        })
                        return
                }
            }

            walk(type, data, "$")

            return {
                valid: issues.length === 0,
                issues,
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
            getDataTypeNameByRyoType,
            getRyoTypeByDataTypeName,
            typeSchemaToRyoType,
            validateDataByRyoType,
            sidePanelExpanded
        }
    }
)
