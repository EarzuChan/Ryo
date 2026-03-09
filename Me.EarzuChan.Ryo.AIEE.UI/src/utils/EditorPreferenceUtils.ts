import type {EditorContext, EditorDescriptor, EditorOverrideRule, EditorOverrideScope} from "@/models/AppModels"

export type EditorApplyStrategy = "once" | "path" | "type" | "cancel"

interface AskStrategyDependencies {
    dialogState: {
        order: (config: any) => void
    }
    t: (key: string, named?: Record<string, unknown>) => string
}

interface ApplyEditorSelectionDependencies extends AskStrategyDependencies {
    appState: {
        upsertEditorOverrideRule: (rule: any) => void
        findDuplicateEditorOverrideRule?: (
            scope: EditorOverrideScope,
            pattern: string,
            typeConstraint?: string,
            excludeId?: string
        ) => EditorOverrideRule | undefined
        getEditorDescriptorById?: (editorId?: string) => EditorDescriptor | undefined
    }
    workspaceState: {
        setItemSessionOnceEditorOverride: (itemKey: string, path: string, editorId: string) => void
    }
    itemKey: string
    context: EditorContext
    editor: EditorDescriptor
}

function askReplaceExistingRule({
    dialogState,
    t,
    scope,
    pattern,
    currentEditorTitle,
    nextEditorTitle,
}: {
    dialogState: { order: (config: any) => void }
    t: (key: string, named?: Record<string, unknown>) => string
    scope: EditorOverrideScope
    pattern: string
    currentEditorTitle: string
    nextEditorTitle: string
}): Promise<boolean> {
    return new Promise(resolve => {
        let confirmed = false
        dialogState.order({
            headline: t("editorRulesReplaceConfirmTitle"),
            description: t("editorRulesReplaceConfirmDesc", {
                scope: scope === "path" ? t("editorRulesPathRules") : t("editorRulesTypeRules"),
                pattern,
                currentEditor: currentEditorTitle,
                nextEditor: nextEditorTitle,
            }),
            actions: [
                {text: t("cancel"), onClick() { confirmed = false }},
                {text: t("confirm"), onClick() { confirmed = true }},
            ],
            onClosed: () => resolve(confirmed),
        })
    })
}

export function askEditorApplyStrategy({dialogState, t}: AskStrategyDependencies): Promise<EditorApplyStrategy> {
    return new Promise(resolve => {
        let action: EditorApplyStrategy = "cancel"
        dialogState.order({
            headline: t("editorApplyStrategyHeadline"),
            description: t("editorApplyStrategyDesc"),
            actions: [
                {text: t("editorApplyOnce"), onClick() { action = "once" }},
                {text: t("editorApplyPathAlways"), onClick() { action = "path" }},
                {text: t("editorApplyTypeAlways"), onClick() { action = "type" }},
                {text: t("cancel"), onClick() { action = "cancel" }},
            ],
            onClosed: () => resolve(action),
        })
    })
}

export async function applyEditorSelectionWithStrategy(deps: ApplyEditorSelectionDependencies): Promise<EditorApplyStrategy> {
    const {appState, workspaceState, itemKey, context, editor} = deps
    const action = await askEditorApplyStrategy(deps)

    const tryApplyPersistentRule = async (scope: EditorOverrideScope, pattern: string, typeConstraint?: string) => {
        const duplicate = appState.findDuplicateEditorOverrideRule?.(scope, pattern, typeConstraint)
        if (duplicate && duplicate.editorId !== editor.id) {
            const currentEditorTitle = appState.getEditorDescriptorById?.(duplicate.editorId)?.titleKey
            const confirmed = await askReplaceExistingRule({
                dialogState: deps.dialogState,
                t: deps.t,
                scope,
                pattern,
                currentEditorTitle: currentEditorTitle ? deps.t(currentEditorTitle) : duplicate.editorId,
                nextEditorTitle: deps.t(editor.titleKey),
            })
            if (!confirmed) return false
        }

        appState.upsertEditorOverrideRule({
            id: duplicate?.id,
            scope,
            pattern,
            editorId: editor.id,
            enabled: true,
            typeConstraint,
        })
        return true
    }

    switch (action) {
        case "once":
            workspaceState.setItemSessionOnceEditorOverride(itemKey, context.path, editor.id)
            break
        case "path":
            if (!await tryApplyPersistentRule("path", context.path, context.dataTypeName)) return "cancel"
            break
        case "type":
            if (!await tryApplyPersistentRule("type", context.dataTypeName)) return "cancel"
            break
        case "cancel":
            break
    }

    return action
}
