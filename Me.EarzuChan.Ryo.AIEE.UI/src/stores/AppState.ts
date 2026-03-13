import {computed, ref} from 'vue'
import {defineStore} from 'pinia'
import {emitWebEvent, makeWebLetter, sendWebCallAndTakeItsReturnValues} from "@/utils/KurisuUtils"
import {type EditorContext, type EditorDescriptor, type EditorOverrideRule, type OverrideRuleSet, type ResolvedEditorSelection, type RyoType, type TypeSchema} from "@/models/AppModels"
import NumberEditor from "@/components/editors/NumberEditor.vue"
import TextEditor from "@/components/editors/TextEditor.vue"
import BooleanEditor from "@/components/editors/BooleanEditor.vue"
import ArrayEditor from "@/components/editors/ArrayEditor.vue"
import FieldEditor from "@/components/editors/FieldEditor.vue"
import StringListEditor from "@/components/editors/StringListEditor.vue"
import {setLanguage, sysLang} from "@/misc/I18n"
import {i18n} from "@/misc/I18n"
import {dataTypeNameFromRyoType, ruleMatchesContext} from "@/utils/EditorOverrideUtils"
import {generateId} from "@/utils/UsefulUtils"

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
        const editorOverrideRules = ref<OverrideRuleSet>({pathRules: [], typeRules: []})
        const editorOverrideVersion = ref(0)
        const reffedAppLanguage = ref(sysLang)

        function applyLanguage(value: string, persist: boolean) {
            if (reffedAppLanguage.value === value && i18n.global.locale.value === value) return
            if (setLanguage(value)) {
                reffedAppLanguage.value = value as "zh" | "en" | "ru"
                if (persist) emitWebEvent(makeWebLetter("Preference:Language", value))
            }
        }

        const appLanguage = computed({
            get: () => reffedAppLanguage.value,
            set(value: string) {
                console.log(TAG, "设置语言", value)
                applyLanguage(value, true)
            }
        })

        // 为提高性能的缓存
        const ryoTypeCache = new Map<string, RyoType>()
        const editorsCache = new Map<string, EditorDescriptor[]>()
        const editorRegistry: EditorDescriptor[] = [
            {
                id: "generic.array",
                titleKey: "editorGenericArray",
                surface: "inline",
                component: ArrayEditor,
                priority: 10,
                supports: context => context.ryoType.isArray
            },
            {
                id: "special.string-list",
                titleKey: "editorSpecialStringList",
                surface: "inline",
                component: StringListEditor,
                priority: 5,
                supports: context => context.ryoType.isArray && context.ryoType.typeName === "java.lang.String"
            },
            {
                id: "generic.text",
                titleKey: "editorGenericText",
                surface: "inline",
                component: TextEditor,
                priority: 10,
                supports: context => !context.ryoType.isArray && ["java.lang.String", "java.lang.Character"].includes(context.ryoType.typeName)
            },
            {
                id: "generic.number",
                titleKey: "editorGenericNumber",
                surface: "inline",
                component: NumberEditor,
                priority: 10,
                supports: context => !context.ryoType.isArray && [
                    "java.lang.Integer", "java.lang.Long", "java.lang.Float",
                    "java.lang.Double", "java.lang.Short", "java.lang.Byte"
                ].includes(context.ryoType.typeName)
            },
            {
                id: "generic.boolean",
                titleKey: "editorGenericBoolean",
                surface: "inline",
                component: BooleanEditor,
                priority: 10,
                supports: context => !context.ryoType.isArray && context.ryoType.typeName === "java.lang.Boolean"
            },
            {
                id: "generic.field",
                titleKey: "editorGenericField",
                surface: "inline",
                component: FieldEditor,
                priority: 10,
                supports: context => !context.ryoType.isArray && !!context.ryoType.baseType && ![
                    "java.lang.String", "java.lang.Character", "java.lang.Integer", "java.lang.Long",
                    "java.lang.Float", "java.lang.Double", "java.lang.Short", "java.lang.Byte",
                    "java.lang.Boolean", "java.lang.Void"
                ].includes(context.ryoType.typeName)
            }
        ]

        function createEditorContext(ryoType: RyoType, options?: Partial<EditorContext>): EditorContext {
            return {
                itemKey: options?.itemKey,
                dataTypeName: options?.dataTypeName ?? dataTypeNameFromRyoType(ryoType),
                ryoType,
                path: options?.path ?? "$",
                isRoot: options?.isRoot ?? true,
            }
        }

        function getEditorsByContext(context: EditorContext): EditorDescriptor[] {
            const cacheKey = `${context.dataTypeName}|${context.isRoot}|${context.path}`

            if (editorsCache.has(cacheKey)) {
                console.log(TAG, "[Editors 缓存命中]", cacheKey)
                return editorsCache.get(cacheKey)!
            }

            const editors = editorRegistry.filter((editor: EditorDescriptor) => editor.supports ? editor.supports(context) : true)
                .sort((a, b) => (b.priority ?? 0) - (a.priority ?? 0))

            editorsCache.set(cacheKey, editors)
            return editors
        }

        function getEditorsByRyoType(ryoType: RyoType) {
            return getEditorsByContext(createEditorContext(ryoType))
        }

        function getEditorDescriptorById(editorId?: string): EditorDescriptor | undefined {
            if (!editorId) return undefined
            return editorRegistry.find((editor: EditorDescriptor) => editor.id === editorId)
        }

        function getEditorsForOverrideRule(scope: "path" | "type", pattern: string, typeConstraint?: string): EditorDescriptor[] {
            const targetTypeName = (scope === "path" ? typeConstraint : undefined)
                ?? (pattern.includes("*") ? typeConstraint : pattern)

            if (!targetTypeName) return []

            return getEditorsByContext(createEditorContext(getRyoTypeByDataTypeName(targetTypeName), {
                dataTypeName: targetTypeName,
                path: scope === "path" ? pattern || "$" : "$",
                isRoot: false,
            }))
        }

        function getOverrideRules(scope: "path" | "type") {
            return scope === "path" ? editorOverrideRules.value.pathRules : editorOverrideRules.value.typeRules
        }

        function isSameOverrideTarget(a: Pick<EditorOverrideRule, "scope" | "pattern" | "typeConstraint">, b: Pick<EditorOverrideRule, "scope" | "pattern" | "typeConstraint">) {
            return a.scope === b.scope
                && a.pattern === b.pattern
                && (a.typeConstraint ?? "") === (b.typeConstraint ?? "")
        }

        function setEditorOverrideRules(ruleSet: OverrideRuleSet, persist: boolean) {
            editorOverrideRules.value = {
                pathRules: [...(ruleSet?.pathRules ?? [])],
                typeRules: [...(ruleSet?.typeRules ?? [])],
            }
            editorOverrideVersion.value++

            if (persist) emitWebEvent(makeWebLetter("Preference:EditorOverrides", editorOverrideRules.value))
        }

        function upsertEditorOverrideRule(rule: Omit<EditorOverrideRule, "id" | "updatedAt"> & { id?: string }) {
            const draftRule: EditorOverrideRule = {
                ...rule,
                id: rule.id ?? generateId(Date.now()).toString(),
                updatedAt: Date.now(),
            }

            const currentRules = getOverrideRules(rule.scope)
            const byIdIndex = draftRule.id
                ? currentRules.findIndex((it: EditorOverrideRule) => it.id === draftRule.id)
                : -1
            const duplicateIndex = currentRules.findIndex((it: EditorOverrideRule, index: number) =>
                index !== byIdIndex && isSameOverrideTarget(it, draftRule)
            )

            let replaced = [...currentRules]

            if (byIdIndex !== -1) {
                replaced[byIdIndex] = draftRule
            } else if (duplicateIndex !== -1) {
                const duplicateRule = replaced[duplicateIndex]
                replaced[duplicateIndex] = {
                    ...draftRule,
                    id: duplicateRule.id,
                }
            } else {
                replaced.push(draftRule)
            }

            // Keep a single rule for each target (scope + pattern + typeConstraint).
            if (byIdIndex !== -1 && duplicateIndex !== -1) {
                replaced = replaced.filter((_, index: number) => index !== duplicateIndex)
            }

            setEditorOverrideRules({
                pathRules: rule.scope === "path" ? replaced : editorOverrideRules.value.pathRules,
                typeRules: rule.scope === "type" ? replaced : editorOverrideRules.value.typeRules,
            }, true)
        }

        function findDuplicateEditorOverrideRule(
            scope: "path" | "type",
            pattern: string,
            typeConstraint?: string,
            excludeId?: string
        ): EditorOverrideRule | undefined {
            return getOverrideRules(scope).find((it: EditorOverrideRule) =>
                it.id !== excludeId
                && it.scope === scope
                && it.pattern === pattern
                && (it.typeConstraint ?? "") === (typeConstraint ?? "")
            )
        }

        function removeEditorOverrideRule(scope: "path" | "type", ruleId: string) {
            const currentRules = getOverrideRules(scope)
            const filtered = currentRules.filter((it: EditorOverrideRule) => it.id !== ruleId)

            setEditorOverrideRules({
                pathRules: scope === "path" ? filtered : editorOverrideRules.value.pathRules,
                typeRules: scope === "type" ? filtered : editorOverrideRules.value.typeRules,
            }, true)
        }

        function resolveEditorForContext(context: EditorContext, onceOverrides?: Record<string, string>, requestedEditorId?: string): ResolvedEditorSelection {
            const availableEditors = getEditorsByContext(context)
            if (availableEditors.length === 0) return {source: "none"}

            const findEditor = (editorId?: string): EditorDescriptor | undefined =>
                editorId ? availableEditors.find((editor: EditorDescriptor) => editor.id === editorId) : undefined

            const requested = findEditor(requestedEditorId)
            if (requested) return {editor: requested, source: "requested"}

            const onceEditor = findEditor(onceOverrides?.[context.path])
            if (onceEditor) return {editor: onceEditor, source: "once"}

            const pathMatch = editorOverrideRules.value.pathRules
                .map(rule => ({rule, match: ruleMatchesContext(rule, context)}))
                .filter(candidate => candidate.match.matched && !!findEditor(candidate.rule.editorId))
                .sort((a, b) => {
                    if (b.match.score !== a.match.score) return b.match.score - a.match.score
                    return b.rule.updatedAt - a.rule.updatedAt
                })[0]

            if (pathMatch) {
                const matchedEditor = findEditor(pathMatch.rule.editorId)
                if (matchedEditor) return {
                    editor: matchedEditor,
                    source: "path",
                    matchedRule: pathMatch.rule,
                }
            }

            const typeMatch = editorOverrideRules.value.typeRules
                .map(rule => ({rule, match: ruleMatchesContext(rule, context)}))
                .filter(candidate => candidate.match.matched && !!findEditor(candidate.rule.editorId))
                .sort((a, b) => {
                    if (b.match.score !== a.match.score) return b.match.score - a.match.score
                    return b.rule.updatedAt - a.rule.updatedAt
                })[0]

            if (typeMatch) {
                const matchedEditor = findEditor(typeMatch.rule.editorId)
                if (matchedEditor) return {
                    editor: matchedEditor,
                    source: "type",
                    matchedRule: typeMatch.rule,
                }
            }

            return {
                editor: availableEditors[0],
                source: "default",
            }
        }

        function getMatchedEditorOverrideRulesForContext(context: EditorContext): EditorOverrideRule[] {
            const availableEditors = getEditorsByContext(context)
            if (availableEditors.length === 0) return []

            const canUseEditor = (editorId?: string) =>
                !!editorId && availableEditors.some((editor: EditorDescriptor) => editor.id === editorId)

            return [...editorOverrideRules.value.pathRules, ...editorOverrideRules.value.typeRules]
                .map(rule => ({rule, match: ruleMatchesContext(rule, context)}))
                .filter(candidate => candidate.match.matched && canUseEditor(candidate.rule.editorId))
                .sort((a, b) => {
                    if (a.rule.scope !== b.rule.scope) return a.rule.scope === "path" ? -1 : 1
                    if (b.match.score !== a.match.score) return b.match.score - a.match.score
                    return b.rule.updatedAt - a.rule.updatedAt
                })
                .map(candidate => candidate.rule)
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
            const tr = i18n.global.t

            const walk = (ryoType: RyoType, val: any, path: string) => {
                if (val === null || val === undefined) {
                    const baseType = ryoType.baseType
                    if (baseType?.type !== "java.lang.Void")
                        issues.push({path, message: tr("validationValueMissing") as string})
                    return
                }

                if (ryoType.isArray) {
                    if (!Array.isArray(val)) {
                        issues.push({path, message: tr("validationExpectedArrayActualType", {actual: typeof val}) as string})
                        return
                    }

                    const sub = getRyoTypeByDataTypeName(ryoType.typeName)
                    val.forEach((it: any, index: number) => walk(sub, it, `${path}[${index}]`))
                    return
                }

                const baseType = ryoType.baseType
                if (!baseType) return

                switch (baseType.type) {
                    case "java.lang.String":
                    case "java.lang.Character":
                        if (typeof val !== "string") issues.push({path, message: tr("validationExpectedStringActualType", {actual: typeof val}) as string})
                        return
                    case "java.lang.Integer":
                    case "java.lang.Long":
                    case "java.lang.Float":
                    case "java.lang.Double":
                    case "java.lang.Short":
                    case "java.lang.Byte":
                        if (typeof val !== "number" || Number.isNaN(val)) issues.push({path, message: tr("validationExpectedNumberActualType", {actual: typeof val}) as string})
                        return
                    case "java.lang.Boolean":
                        if (typeof val !== "boolean") issues.push({path, message: tr("validationExpectedBooleanActualType", {actual: typeof val}) as string})
                        return
                    case "java.lang.Void":
                        return
                    default:
                        if (typeof val !== "object" || Array.isArray(val)) {
                            issues.push({path, message: tr("validationExpectedObjectActualType", {actual: typeof val}) as string})
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
                applyLanguage(fetchedLanguage, false)

                const fetchedTesting = (await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppProperty:IsDebug")))[0]
                console.log(TAG, "Testing fetched", fetchedTesting)
                preferTesting.value = fetchedTesting

                const fetchedEditorOverrides = (await sendWebCallAndTakeItsReturnValues(makeWebLetter("Preference:EditorOverrides", {pathRules: [], typeRules: []})))[0] as OverrideRuleSet
                const realEditorOverrides = fetchedEditorOverrides ?? {pathRules: [], typeRules: []}
                console.log(TAG, "Editor Overrides fetched", realEditorOverrides)
                setEditorOverrideRules(realEditorOverrides, false)

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
            createEditorContext,
            getEditorsByContext,
            getEditorsByRyoType,
            getEditorDescriptorById,
            getEditorsForOverrideRule,
            getMatchedEditorOverrideRulesForContext,
            findDuplicateEditorOverrideRule,
            getDataTypeNameByRyoType,
            getRyoTypeByDataTypeName,
            editorOverrideRules,
            editorOverrideVersion,
            upsertEditorOverrideRule,
            removeEditorOverrideRule,
            resolveEditorForContext,
            typeSchemaToRyoType,
            validateDataByRyoType,
            sidePanelExpanded
        }
    }
)
