import {defineStore} from "pinia"
import {computed, markRaw, ref} from "vue"
import ItemPage from "@/views/pages/ItemPage.vue"
import WelcomePage from "@/views/pages/WelcomePage.vue"
import SettingsPage from "@/views/pages/SettingsPage.vue"
import {
    addWebEventListener,
    emitWebEvent,
    makeWebLetter,
    sendWebCallAndTakeItsReturnValues
} from "@/utils/KurisuUtils"
import {useDialogStateStore} from "@/stores/DialogState"
import {type EditorSession, type FileModel, type VolumeModel, TabType, type RyoType} from "@/models/AppModels"
import type {TabModel} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import {deepCopy, ensure, generateId} from "@/utils/UsefulUtils"
import AddItemDialog from "@/views/dialogs/AddItemDialog.vue"
import {useI18n} from "vue-i18n"
import RenameItemDialog from "@/views/dialogs/RenameItemDialog.vue"
import {applyOperations, buildSessionFrame, hasRedo, hasUndo, pushUndoFrame} from "@/utils/SessionUtils"
import {CommandErrorCode, type CommandError, type CommandResult} from "@/models/CommandModels"
import {fail, normalizeCommandError, ok} from "@/utils/CommandUtils"

const TAG = "WorkspaceState"

export const useWorkspaceStateStore = defineStore('workspace-state', () => {
        const available = ref(false)

        const appState = useAppStateStore()

        const {t} = useI18n()

        const activeTabExposed = ref<any>(null)

        const openedVolumes = ref<VolumeModel[]>([])
        const openedItems = ref<FileModel[]>([])
        const openedTabs = ref<TabModel[]>([])
        const activeTabIndex = ref(-1)
        const activeTab = computed(() => openedTabs.value[activeTabIndex.value])
        const activeItemKey = computed(() =>
            typeof activeTab.value?.data === "string" ? activeTab.value.data : undefined)
        const activeItem = computed(() => openedItems.value.find(i => i.itemKey === activeItemKey.value))
        const activeVolume = computed(() => {
            const candidate = activeItem.value?.fromFile
            if (!candidate) return undefined
            return openedVolumes.value.some(v => v.name === candidate) ? candidate : undefined
        })

        const dialogState = useDialogStateStore()
        const DEFAULT_MAX_UNDO = 200
        const closingVolumes = new Set<string>()

        function copyData<T>(value: T): T {
            if (value === undefined) return value
            return deepCopy(value)
        }

        function getVolumeRevision(volumeName?: string): number {
            if (!volumeName) return -1
            return openedVolumes.value.find(v => v.name === volumeName)?.revision ?? -1
        }

        function getOpenedVolume(volumeName?: string): VolumeModel | undefined {
            if (!volumeName) return undefined
            return openedVolumes.value.find(v => v.name === volumeName)
        }

        function makeItemKey(item: FileModel) {
            const volume = item.fromFile ?? "unknown-volume"
            const idPart = item.id >= 0 ? `id-${item.id}` : `draft-${generateId(Date.now())}`
            const name = item.name ?? "unnamed"
            return `${volume}:${idPart}:${name}:${generateId(Date.now())}`
        }

        function ensureItemKey(item: FileModel): string {
            if (!item.itemKey) item.itemKey = makeItemKey(item)
            return item.itemKey
        }

        function getItemIndexByKey(itemKey?: string): number {
            if (!itemKey) return -1
            return openedItems.value.findIndex(item => item.itemKey === itemKey)
        }

        function getItemByKey(itemKey?: string): FileModel | undefined {
            const index = getItemIndexByKey(itemKey)
            return index === -1 ? undefined : openedItems.value[index]
        }

        function makeSessionId(item: FileModel) {
            return `${item.fromFile ?? "unknown"}:${item.id}:${item.name ?? "unnamed"}:${generateId(Date.now())}`
        }

        function isItemUnsaved(item: FileModel): boolean {
            if (item.id === -1) return true
            if (!item.session) return item.unsaved === true
            return isSessionDirty(item, item.session)
        }

        function refreshItemTabState(itemIndex: number) {
            const item = openedItems.value[itemIndex]
            if (!item) return

            const itemKey = ensureItemKey(item)
            for (const tab of openedTabs.value) if (tab.data === itemKey) if (item.name) tab.name = item.name
        }

        function setItemUnsaved(itemIndex: number, unsaved: boolean) {
            const item = openedItems.value[itemIndex]
            if (!item) return

            item.unsaved = unsaved
            refreshItemTabState(itemIndex)
        }

        function markItemTabsResident(itemRef: number | string) {
            const itemIndex = resolveItemIndex(itemRef)
            const item = openedItems.value[itemIndex]
            if (!item) return

            const itemKey = ensureItemKey(item)
            for (const tab of openedTabs.value) if (tab.data === itemKey && tab.nonResident) tab.nonResident = false
        }

        function buildItemSession(item: FileModel): EditorSession {
            const baselineData = copyData(item.data)
            return {
                sessionId: makeSessionId(item),
                baselineData,
                currentData: copyData(baselineData),
                onceEditorOverrides: {},
                editorOverrideVersion: 0,
                undoStack: [],
                redoStack: [],
                applying: false,
                maxUndo: DEFAULT_MAX_UNDO,
            }
        }

        function isSessionDirty(item: FileModel, session: EditorSession) {
            return item.id === -1 || session.undoStack.length > 0
        }

        function resolveItemIndex(itemRef: number | string): number {
            if (typeof itemRef === "number") return itemRef
            return getItemIndexByKey(itemRef)
        }

        function ensureItemSession(itemRef: number | string): EditorSession | undefined {
            const itemIndex = resolveItemIndex(itemRef)
            const item = openedItems.value[itemIndex]
            if (!item) return

            ensureItemKey(item)
            if (!item.session) item.session = buildItemSession(item)

            if (item.tempData === undefined || item.session.applying) item.tempData = copyData(item.session.currentData)
            setItemUnsaved(itemIndex, isSessionDirty(item, item.session))
            return item.session
        }

        function syncSessionToItem(itemIndex: number, session: EditorSession) {
            const item = openedItems.value[itemIndex]
            if (!item) return

            item.tempData = copyData(session.currentData)
            setItemUnsaved(itemIndex, isSessionDirty(item, session))
        }

        function recordItemSessionChange(itemRef: number | string, newData: any) {
            const itemIndex = resolveItemIndex(itemRef)
            const session = ensureItemSession(itemIndex)
            if (!session || session.applying) return

            const frame = buildSessionFrame(session.currentData, newData)
            if (!frame) return

            pushUndoFrame(session.undoStack, frame, session.maxUndo)
            // VSCode风：一旦编辑就常驻
            markItemTabsResident(itemIndex)
            session.redoStack = []
            session.currentData = copyData(newData)
            syncSessionToItem(itemIndex, session)
        }

        function undoItemSession(itemRef: number | string): boolean {
            const itemIndex = resolveItemIndex(itemRef)
            const session = ensureItemSession(itemIndex)
            if (!session || !hasUndo(session.undoStack)) return false

            const frame = session.undoStack.pop()!

            session.applying = true
            session.currentData = applyOperations(session.currentData, frame.backward)
            session.redoStack.push(frame)
            syncSessionToItem(itemIndex, session)
            session.applying = false
            return true
        }

        function redoItemSession(itemRef: number | string): boolean {
            const itemIndex = resolveItemIndex(itemRef)
            const session = ensureItemSession(itemIndex)
            if (!session || !hasRedo(session.redoStack)) return false

            const frame = session.redoStack.pop()!

            session.applying = true
            session.currentData = applyOperations(session.currentData, frame.forward)
            session.undoStack.push(frame)
            syncSessionToItem(itemIndex, session)
            session.applying = false
            return true
        }

        function discardItemSessionChanges(itemRef: number | string) {
            const itemIndex = resolveItemIndex(itemRef)
            const session = ensureItemSession(itemIndex)
            if (!session) return

            session.applying = true
            session.currentData = copyData(session.baselineData)
            session.undoStack = []
            session.redoStack = []

            syncSessionToItem(itemIndex, session)
            session.applying = false
        }

        function commitItemSessionAsSaved(itemRef: number | string, savedData: any) {
            const itemIndex = resolveItemIndex(itemRef)
            const session = ensureItemSession(itemIndex)
            if (!session) return

            session.applying = true
            session.baselineData = copyData(savedData)
            session.currentData = copyData(savedData)
            session.undoStack = []
            session.redoStack = []

            syncSessionToItem(itemIndex, session)
            session.applying = false
        }

        function canUndoItemSession(itemRef: number | string) {
            const itemIndex = resolveItemIndex(itemRef)
            const session = ensureItemSession(itemIndex)
            return !!session && hasUndo(session.undoStack)
        }

        function canRedoItemSession(itemRef: number | string) {
            const itemIndex = resolveItemIndex(itemRef)
            const session = ensureItemSession(itemIndex)
            return !!session && hasRedo(session.redoStack)
        }

        function getItemSessionOnceEditorOverrides(itemRef: number | string): Record<string, string> {
            return ensureItemSession(itemRef)?.onceEditorOverrides ?? {}
        }

        function setItemSessionOnceEditorOverride(itemRef: number | string, path: string, editorId: string) {
            const session = ensureItemSession(itemRef)
            if (!session) return
            session.onceEditorOverrides[path] = editorId
            session.editorOverrideVersion++
        }

        function removeItemSessionOnceEditorOverride(itemRef: number | string, path: string) {
            const session = ensureItemSession(itemRef)
            if (!session) return
            if (!(path in session.onceEditorOverrides)) return
            delete session.onceEditorOverrides[path]
            session.editorOverrideVersion++
        }

        function getItemSessionEditorOverrideVersion(itemRef: number | string): number {
            return ensureItemSession(itemRef)?.editorOverrideVersion ?? 0
        }

        function openVolume() {
            emitWebEvent(makeWebLetter('OpenVolume'))
        }

        function newVolume() {
            emitWebEvent(makeWebLetter('NewVolume', t("newMassPrefix")))
        }

        function clickTab(index: number) {
            activeTabIndex.value = index
        }

        function anchorTab(index: number) {
            const tab = openedTabs.value[index]
            if (tab.nonResident) tab.nonResident = false
        }

        function getIsTabUnsaved(index: number) {
            const tab = openedTabs.value[index]
            if (!tab) return false
            const man = tab.data

            if (typeof man === 'string') {
                const item = getItemByKey(man)
                return !!item && isItemUnsaved(item)
            }
            else return false
        }

        function closeTab(index: number) {
            const tab = openedTabs.value[index]
            if (!tab) return

            activeTabIndex.value = index

            const closeCurrentTab = () => {
                if (typeof tab.data === "string" && getItemByKey(tab.data)) {
                    removeOpenedItemByKey(tab.data)
                } else {
                    internalCloseTab(tab, index)
                }
            }

            if (getIsTabUnsaved(index)) {
                let closeAction: "save" | "discard" | "cancel" = "cancel"

                dialogState.order({
                    headline: "是否要保存对 " + tab.name + " 的更改？",
                    description: "如果不保存，你的更改将丢失。",
                    actions: [
                        {
                            text: "保存", onClick() {
                                closeAction = "save"
                            }
                        },
                        {
                            text: "不保存", onClick() {
                                closeAction = "discard"
                            }
                        },
                        {
                            text: "取消", onClick() {
                                closeAction = "cancel"
                            }
                        }
                    ],
                    onClosed() {
                        ;(async () => {
                            switch (closeAction) {
                                case "save":
                                    if (typeof tab.data === "string") {
                                        const saved = await saveItemByKey(tab.data, true)
                                        if (saved) closeCurrentTab()
                                    } else {
                                        activeTabExposed.value?.save?.(() => closeCurrentTab())
                                    }
                                    break
                                case "discard":
                                    closeCurrentTab()
                                    break
                                case "cancel":
                                    break
                            }
                        })()
                    }
                })
            } else closeCurrentTab()
        }

        function internalCloseTab(tab: TabModel, indexHint?: number) {
            let index = -1
            if (tab.key) index = openedTabs.value.findIndex(t => t.key === tab.key)
            if (index === -1 && indexHint !== undefined && openedTabs.value[indexHint] === tab) index = indexHint
            if (index === -1) index = openedTabs.value.indexOf(tab)
            if (index === -1) return

            const activeTabKey = openedTabs.value[activeTabIndex.value]?.key
            openedTabs.value.splice(index, 1)

            if (openedTabs.value.length === 0) {
                activeTabIndex.value = -1
                return
            }

            if (activeTabKey) {
                const newActiveIndex = openedTabs.value.findIndex(t => t.key === activeTabKey)
                activeTabIndex.value = newActiveIndex === -1
                    ? Math.min(index, openedTabs.value.length - 1)
                    : newActiveIndex
            } else {
                if (activeTabIndex.value === index) activeTabIndex.value = Math.min(index, openedTabs.value.length - 1)
                else if (activeTabIndex.value > index) activeTabIndex.value--
            }
        }

        function setActiveTabExposed(page: any) {
            activeTabExposed.value = page
            console.debug(TAG, "已设置当前Tab", page)
        }

        function openTab(tabType: TabType, data?: any) {
            console.debug(TAG, "打开Tab", tabType, data)
            switch (tabType) {
                case TabType.Empty:
                    internalOpenTab({key: "tab-empty", name: t('emptyPage'), nonResident: true})
                    break

                case TabType.Item:
                    const fileModel = getItemByKey(data)
                    if (!fileModel) {
                        console.warn(TAG, "打开ItemTab失败，未找到itemKey", data)
                        return
                    }
                    const itemKey = ensureItemKey(fileModel)
                    ensureItemSession(itemKey)

                    let name = fileModel.name
                    if (name === undefined) name = t('noNameItem')

                    internalOpenTab({
                        key: `tab-item-${itemKey}`,
                        name,
                        page: markRaw(ItemPage),
                        data: itemKey,
                        // Existing persisted items open as preview-like tabs;
                        // brand-new drafts stay resident to avoid accidental replacement.
                        nonResident: fileModel.id !== -1
                    })
                    break

                case TabType.Welcome:
                    internalOpenTab({key: "tab-welcome", name: t('welcome'), page: markRaw(WelcomePage), nonResident: true})
                    break
                
                case TabType.Settings:
                    internalOpenTab({key: "tab-settings", name: t('preferences'), page: markRaw(SettingsPage), nonResident: false})
            }
        }

        function internalOpenTab(tab: TabModel) {
            if (tab.key) {
                const tabIndex = openedTabs.value.findIndex(t => t.key === tab.key)
                if (tabIndex !== -1) {
                    activeTabIndex.value = tabIndex
                    return
                }
            }

            // 如果Tab不是ItemPage，就看看有没有打开过，有就简单切换至就行了
            if (tab.page !== markRaw(ItemPage)) {
                const index = openedTabs.value.findIndex(t => t.page === tab.page)
                if (index !== -1) {
                    activeTabIndex.value = index
                    return
                }
            }

            // 遍历是否有非常驻，有就顶掉
            let nonResidentIndex = openedTabs.value.findIndex(tab => tab.nonResident)
            console.debug(TAG, "内部打开Tab", tab, nonResidentIndex)
            if (nonResidentIndex !== -1) {
                openedTabs.value[nonResidentIndex] = tab
                activeTabIndex.value = nonResidentIndex
            } else {
                openedTabs.value.push(tab)
                activeTabIndex.value = openedTabs.value.length - 1
            }
        }

        function pageDiscard() {
            activeTabExposed.value?.discard()
        }

        function pageReload(fromSystem: boolean = false) {
            activeTabExposed.value?.reload(fromSystem)
        }

        function pageSave() {
            activeTabExposed.value?.save()
        }

        function pageRedo() {
            activeTabExposed.value?.redo()
        }

        function pageUndo() {
            activeTabExposed.value?.undo()
        }

        function deleteItem(massName: string, itemName: string) {
            emitWebEvent(makeWebLetter('DeleteItem', massName, itemName))
        }

        function renameItem(massName: string, oldItemName: string) {
            dialogState.orderSpecial(RenameItemDialog, {
                oldName: oldItemName,
                confirm: (itemName: string) => {
                    console.log(TAG, massName, "Rename", oldItemName, "To", itemName)

                    emitWebEvent(makeWebLetter('RenameItem', massName, oldItemName, itemName))
                }
            })
        }

        async function mentionItem(massName: string, itemId: number) {
            console.log(TAG, "提及项目", massName, itemId)

            let itemKey = openedItems.value.find(item =>
                item.id === itemId && item.fromFile === massName)
            ?.itemKey

            if (itemKey) {
                const tabIndex = openedTabs.value.findIndex(tab => tab.data === itemKey)

                if (tabIndex !== -1) {
                    activeTabIndex.value = tabIndex
                    return
                }
            } else {
                const fileModel = await getFullFileModel(massName, itemId)

                if (ensure(fileModel)) {
                    ensureItemKey(fileModel!)
                    openedItems.value.push(fileModel!)
                    itemKey = fileModel!.itemKey
                }
            }

            if (itemKey) openTab(TabType.Item, itemKey)
        }

        async function getFullFileModel(massName: string, itemId: number) {
            console.log(TAG, "获取项目", massName, itemId)
            const fileModel = (await sendWebCallAndTakeItsReturnValues(makeWebLetter('GetFullFileModel', massName, itemId)))[0] as FileModel
            fileModel.ryoType = appState.getRyoTypeByDataTypeName(fileModel.dataTypeName!)
            fileModel.unsaved = false
            if (fileModel.volumeRevision === undefined) fileModel.volumeRevision = getVolumeRevision(massName)
            ensureItemKey(fileModel)
            console.log(TAG, "获取到项目", massName, itemId, fileModel)

            return fileModel
        }

        function saveVolume(massName: string) {
            emitWebEvent(makeWebLetter('SaveVolume', massName, false))
        }

        function saveVolumeAs(massName: string) {
            emitWebEvent(makeWebLetter('SaveVolume', massName, true))
        }

        async function saveItem(
            massName: string,
            itemId: number,
            itemName: string,
            data: any,
            tsRyoTypeName: string,
            expectedVolumeRevision: number
        ) {
            const ret = await sendWebCallAndTakeItsReturnValues(
                makeWebLetter('SaveItem', massName, itemId, itemName, data, tsRyoTypeName, expectedVolumeRevision)
            )
            return {
                itemId: ret[0] as number,
                volumeRevision: ret[1] as number,
            }
        }

        function buildValidationError(item: FileModel, payload: any): CommandError | null {
            if (!item.fromFile || !item.name || !item.dataTypeName) return {
                code: CommandErrorCode.Validation,
                message: t("saveErrorItemInfoIncomplete"),
                recoverHint: t("saveErrorCheckItemMetaHint"),
            }

            if (!item.ryoType) return {
                code: CommandErrorCode.Validation,
                message: t("saveErrorItemTypeMissing"),
                recoverHint: t("saveErrorReopenAndRetryHint"),
            }

            const validation = appState.validateDataByRyoType(item.ryoType, payload)
            if (!validation.valid) {
                const top = validation.issues.slice(0, 5)
                    .map(issue => `${issue.path}: ${issue.message}`)
                    .join("\n")
                const remain = validation.issues.length > 5
                    ? t("saveErrorValidationRemaining", {count: validation.issues.length - 5})
                    : ""
                return {
                    code: CommandErrorCode.Validation,
                    message: t("saveErrorValidationFailedWithDetails", {details: top, remain}),
                    recoverHint: t("saveErrorFixInvalidFieldsHint"),
                }
            }

            return null
        }

        async function executeSaveItemCommand(item: FileModel, payload: any): Promise<CommandResult<{
            itemId: number
            volumeRevision: number
        }>> {
            const validationError = buildValidationError(item, payload)
            if (validationError) return fail(validationError)

            try {
                const saved = await saveItem(
                    item.fromFile!,
                    item.id,
                    item.name!,
                    payload,
                    item.dataTypeName!,
                    item.volumeRevision ?? -1
                )
                return ok(saved)
            } catch (err) {
                return fail(normalizeCommandError(err))
            }
        }

        function formatErrorReason(reason: any): string {
            if (typeof reason === "object" && reason && "message" in reason) {
                const commandError = reason as CommandError
                const msg = `${commandError.message}`
                const recoverHint = commandError.recoverHint
                    ?? (commandError.code === CommandErrorCode.Conflict ? t("saveErrorConflictRecoverHint") : undefined)
                return recoverHint ? `${msg}\n${recoverHint}` : msg
            }
            if (reason instanceof Error) return reason.message
            if (typeof reason === "string") return reason
            try {
                return JSON.stringify(reason)
            } catch {
                return `${reason}`
            }
        }

        async function showSaveErrorDialog(itemName: string, reason: any): Promise<void> {
            return new Promise(resolve => {
                dialogState.order({
                    icon: "close",
                    headline: t("saveItemFailed", {item: itemName}),
                    description: formatErrorReason(reason),
                    actions: [{text: t('confirm')}],
                    onClosed: () => resolve(),
                })
            })
        }

        async function saveItemByKey(itemKey: string, showErrorDialog: boolean = true): Promise<boolean> {
            const itemIndex = getItemIndexByKey(itemKey)
            const item = itemIndex === -1 ? undefined : openedItems.value[itemIndex]
            if (!item) return true
            if (!isItemUnsaved(item)) {
                setItemUnsaved(itemIndex, false)
                return true
            }

            const payload = copyData(item.tempData)
            const result = await executeSaveItemCommand(item, payload)
            if (result.ok) {
                const saved = result.value
                item.id = saved.itemId
                item.volumeRevision = saved.volumeRevision
                item.data = payload
                commitItemSessionAsSaved(itemKey, payload)
                return true
            }

            console.error(TAG, "保存项目失败", item, result.error)
            if (showErrorDialog) await showSaveErrorDialog(item.name ?? t("unknown"), result.error)
            return false
        }

        async function askUnsavedItemAction(itemName: string): Promise<"save" | "discard" | "cancel"> {
            return new Promise(resolve => {
                let action: "save" | "discard" | "cancel" = "cancel"
                dialogState.order({
                    headline: t("saveItemChangesPromptTitle", {item: itemName}),
                    description: t("saveItemChangesPromptDescription"),
                    actions: [
                        {text: t("save"), onClick: () => { action = "save" }},
                        {text: t("dontSave"), onClick: () => { action = "discard" }},
                        {text: t("cancel"), onClick: () => { action = "cancel" }},
                    ],
                    onClosed: () => resolve(action),
                })
            })
        }

        async function askUnsavedVolumeAction(volumeName: string): Promise<"save" | "discard" | "cancel"> {
            return new Promise(resolve => {
                let action: "save" | "discard" | "cancel" = "cancel"
                dialogState.order({
                    headline: t("saveVolumeChangesPromptTitle", {volume: volumeName}),
                    description: t("saveVolumeChangesPromptDescription"),
                    actions: [
                        {text: t("save"), onClick: () => { action = "save" }},
                        {text: t("dontSave"), onClick: () => { action = "discard" }},
                        {text: t("cancel"), onClick: () => { action = "cancel" }},
                    ],
                    onClosed: () => resolve(action),
                })
            })
        }

        function wait(ms: number) {
            return new Promise(resolve => setTimeout(resolve, ms))
        }

        async function waitForVolumeClean(volumeName: string, timeoutMs: number = 8000): Promise<boolean> {
            const started = Date.now()
            while (Date.now() - started <= timeoutMs) {
                const volume = getOpenedVolume(volumeName)
                if (!volume) return true
                if (volume.unsaved !== true) return true
                await wait(120)
            }

            const latest = getOpenedVolume(volumeName)
            return latest?.unsaved !== true
        }

        async function showVolumeSaveIncompleteDialog(volumeName: string): Promise<void> {
            return new Promise(resolve => {
                dialogState.order({
                    icon: "error",
                    headline: t("volumeCloseBlockedByUnsavedTitle", {volume: volumeName}),
                    description: t("volumeCloseBlockedByUnsavedDescription"),
                    actions: [{text: t("confirm")}],
                    onClosed: () => resolve(),
                })
            })
        }

        function removeOpenedItemByKey(itemKey: string) {
            const itemIndex = getItemIndexByKey(itemKey)
            if (itemIndex === -1) return

            const activeTabKey = openedTabs.value[activeTabIndex.value]?.key
            openedItems.value.splice(itemIndex, 1)
            openedTabs.value = openedTabs.value.filter(tab => tab.data !== itemKey)
            if (openedTabs.value.length === 0) {
                activeTabIndex.value = -1
                return
            }

            if (activeTabKey) {
                const newActiveIndex = openedTabs.value.findIndex(t => t.key === activeTabKey)
                activeTabIndex.value = newActiveIndex === -1
                    ? Math.min(activeTabIndex.value, openedTabs.value.length - 1)
                    : newActiveIndex
            } else if (activeTabIndex.value >= openedTabs.value.length) {
                activeTabIndex.value = openedTabs.value.length - 1
            }
        }

        function removeOpenedItemsByVolume(massName: string) {
            const targets = openedItems.value
                .filter(item => item.fromFile === massName)
                .map(item => item.itemKey)
                .filter((k): k is string => !!k)

            targets.forEach(removeOpenedItemByKey)
        }

        async function closeVolume(massName: string) {
            if (closingVolumes.has(massName)) return
            closingVolumes.add(massName)

            try {
                let savedAnyItemInThisCloseFlow = false
                const targets = openedItems.value
                    .filter(item => item.fromFile === massName && item.itemKey)
                    .map(item => item.itemKey!)

                for (const itemKey of targets) {
                    const item = getItemByKey(itemKey)
                    if (!item || !isItemUnsaved(item)) continue

                    const action = await askUnsavedItemAction(item.name ?? t("noNameItem"))
                    if (action === "cancel") return
                    if (action === "save") {
                        const saved = await saveItemByKey(itemKey, true)
                        if (!saved) return
                        savedAnyItemInThisCloseFlow = true
                    }
                }

                const volume = getOpenedVolume(massName)
                if (volume?.unsaved || savedAnyItemInThisCloseFlow) {
                    const action = await askUnsavedVolumeAction(massName)
                    if (action === "cancel") return

                    if (action === "save") {
                        saveVolume(massName)
                        const clean = await waitForVolumeClean(massName)
                        if (!clean) {
                            await showVolumeSaveIncompleteDialog(massName)
                            return
                        }
                    }
                }

                removeOpenedItemsByVolume(massName)
                emitWebEvent(makeWebLetter('CloseVolume', massName))
            } finally {
                closingVolumes.delete(massName)
            }
        }

        function gcVolume(massName: string) {
            emitWebEvent(makeWebLetter('GcVolume', massName))
        }

        function addItemInVolume(massName: string) {
            dialogState.orderSpecial(AddItemDialog, {
                confirm: (itemName: string, ryoType: RyoType) => {
                    console.log(massName, itemName, ryoType)

                    const volume = openedVolumes.value.find(v => v.name === massName)
                    if (volume) {
                        const backendExists = volume.items?.find(item => item.name === itemName)

                        // 如果后端没找到，再找本地草稿
                        let localItem: FileModel | undefined
                        if (!backendExists) localItem = openedItems.value.find(item =>
                            item.fromFile === massName && item.name === itemName && item.id === -1
                        )

                        // 只要有任意一个存在
                        if (backendExists || localItem) {
                            dialogState.order({
                                headline: `已存在"${itemName}"`,
                                description: "不可重复创建同名项目，是否跳转到已有项目？",
                                actions: [
                                    {text: "取消"},
                                    {
                                        text: "跳转", onClick() {
                                            if (backendExists) mentionItem(massName, backendExists.id!)
                                            else {
                                                const localKey = localItem?.itemKey
                                                if (!localKey) return
                                                const tabIndex = openedTabs.value.findIndex(tab => tab.data === localKey)
                                                if (tabIndex !== -1) activeTabIndex.value = tabIndex
                                                else openTab(TabType.Item, localKey)
                                            }
                                        }
                                    }
                                ]
                            })
                            return // 终止
                        }
                    }

                    const fileModel: FileModel = {
                        data: appState.getInitValue(ryoType),
                        fromFile: massName,
                        id: -1, // 未保存，则未分配（-1）
                        name: itemName,
                        parseSuccess: true,
                        volumeRevision: getVolumeRevision(massName),
                        ryoType: ryoType,
                        dataTypeName: appState.getDataTypeNameByRyoType(ryoType),
                        unsaved: true
                    }
                    ensureItemKey(fileModel)

                    openedItems.value.push(fileModel)

                    openTab(TabType.Item, fileModel.itemKey)
                }
            })
        }

        // Async Init
        (async () => {
            try {
                console.log(TAG, "Start init")

                addWebEventListener("OpenedVolumesChanged", (args: VolumeModel[][]) => {
                    console.log(TAG, "接收到Opened Volumes", args[0])
                    openedVolumes.value = args[0]

                    const openedNames = new Set(openedVolumes.value.map(v => v.name))
                    const staleVolumes = new Set(
                        openedItems.value
                            .map(item => item.fromFile)
                            .filter((name): name is string => !!name && !openedNames.has(name))
                    )
                    staleVolumes.forEach(removeOpenedItemsByVolume)
                })
                console.log(TAG, "OpenedVolumesChanged监听器已创建")

                addWebEventListener("VolumeItemRenamed", (args: string[]) => {
                    const [massName, oldName, newName] = args
                    console.log(TAG, massName, "接收重命名", oldName, "->", newName)

                    const item = openedItems.value.find(i => i.fromFile === massName && i.name === oldName)
                    if (item) {
                        item.name = newName
                        const thePage = openedTabs.value.find(tab => tab.data === item.itemKey)
                        if (thePage) thePage.name = newName
                    }
                })
                console.log(TAG, "ItemRenamed监听器已创建")

                addWebEventListener("VolumeItemDeleted", (args: any[]) => {
                    const [massName, id] = args as [string, number]
                    console.log(TAG, massName, "接收删除", id)
                    const index = openedItems.value.findIndex(i => i.fromFile === massName && i.id === id)
                    if (index !== -1) openedItems.value[index].id = -1 // 标记为未写入后端，对了，可能要重置unsaved、dirty啥的状态
                })
                console.log(TAG, "ItemDeleted监听器已创建")

                addWebEventListener("VolumeIdsRemapped", (args: any[]) => {
                    const [massName, intArr] = args as [string, number[]]
                    console.log(TAG, massName, "已打开项目的ID重新同步", intArr) // 主要是处理openedItems里面的ID
                    openedItems.value.forEach(item => {
                        if (item.fromFile === massName && item.id !== -1) {
                            const newId = intArr[item.id]
                            if (newId !== -1) item.id = newId
                        }
                    })
                })
                console.log(TAG, "VolumeIdsRemapped监听器已创建")

                emitWebEvent(makeWebLetter('NotifyOpenedVolumes'))
                console.log(TAG, "已提醒发送OpenedVolumes")

                available.value = true
            } catch (err) {
                console.error(TAG, "Init error", err)
            } finally {
                console.log(TAG, "Init over")
            }
        })()

        return {
            activeItem,
            activeItemKey,
            activeTab,
            activeTabIndex,
            activeTabExposed,
            activeVolume,
            anchorTab,
            available,
            clickTab,
            closeTab,
            deleteItem,
            closeVolume,
            ensureItemSession,
            getIsTabUnsaved,
            mentionItem,
            newVolume,
            gcVolume,
            openTab,
            openVolume,
            openedItems,
            openedTabs,
            openedVolumes,
            pageDiscard,
            pageRedo,
            pageReload,
            pageSave,
            recordItemSessionChange,
            renameItem,
            pageUndo,
            undoItemSession,
            redoItemSession,
            canUndoItemSession,
            canRedoItemSession,
            getItemSessionEditorOverrideVersion,
            getItemSessionOnceEditorOverrides,
            setItemSessionOnceEditorOverride,
            removeItemSessionOnceEditorOverride,
            discardItemSessionChanges,
            commitItemSessionAsSaved,
            saveVolume,
            saveVolumeAs,
            saveItem,
            saveItemByKey,
            setActiveTabExposed,
            addItemInVolume,
        }
    }
)
