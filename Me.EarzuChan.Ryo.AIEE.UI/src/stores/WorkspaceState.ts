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
import NameEditDialog from "@/views/dialogs/NameEditDialog.vue"
import {applyOperations, buildSessionFrame, hasRedo, hasUndo, pushUndoFrame} from "@/utils/SessionUtils"
import {CommandErrorCode, type CommandError, type CommandResult} from "@/models/CommandModels"
import {fail, normalizeCommandError, ok} from "@/utils/CommandUtils"
import {useKurisuStateStore} from "@/stores/KurisuState"
import ImportItemDialog from "@/views/dialogs/ImportItemDialog.vue"
import {
    buildItemExchangeEnvelope,
    decodeImportedItemData,
    parseItemExchangeDocument
} from "@/utils/ItemExchangeUtils"

const TAG = "WorkspaceState"

type ExplorerLocateTarget = {
    path: number[]
    nonce: number
}

type ItemNameConflict = {
    volume?: VolumeModel
    backendExists?: { id: number, name: string }
    localDraftExists: boolean
    openedConflict: boolean
    conflict: boolean
    overwriteBlocked: boolean
}

type ImportDialogSubmitResult = {
    close?: boolean
    error?: string
}

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
        const activeVolumeId = computed(() => {
            const candidate = activeItem.value?.fromVolumeId
            if (!candidate) return undefined
            return openedVolumes.value.some(v => v.id === candidate) ? candidate : undefined
        })
        const activeVolume = computed(() => {
            const volumeId = activeVolumeId.value
            if (!volumeId) return undefined
            return openedVolumes.value.find(v => v.id === volumeId)
        })

        const dialogState = useDialogStateStore()
        const kurisuState = useKurisuStateStore()
        const DEFAULT_MAX_UNDO = 200
        const closingVolumes = new Set<string>()
        const pendingLocalVolumeRevisionSync = new Set<string>()
        const explorerLocateTarget = ref<ExplorerLocateTarget | null>(null)
        let exitFlowRunning = false

        function copyData<T>(value: T): T {
            if (value === undefined) return value
            return deepCopy(value)
        }

        function normalizeLocalPath(path: string): string {
            return path.replace(/\//g, "\\").toLowerCase()
        }

        function getVolumeRevision(volumeId?: string): number {
            if (!volumeId) return -1
            return openedVolumes.value.find(v => v.id === volumeId)?.revision ?? -1
        }

        function getOpenedVolume(volumeId?: string): VolumeModel | undefined {
            if (!volumeId) return undefined
            return openedVolumes.value.find(v => v.id === volumeId)
        }

        function syncOpenedItemVolumeRevision(volumeId: string, revision?: number) {
            if (revision === undefined) return
            openedItems.value.forEach(item => {
                if (item.fromVolumeId === volumeId) item.volumeRevision = revision
            })
        }

        function hasBoundVolumePath(volume?: VolumeModel): boolean {
            return !!(volume?.localPath && volume.localPath.trim().length > 0)
        }

        function isVolumePendingSave(volume?: VolumeModel): boolean {
            if (!volume) return false
            return volume.unsaved === true || !hasBoundVolumePath(volume)
        }

        function getFileNameWithoutExtension(filePath: string): string {
            const normalized = filePath.replace(/\\/g, "/")
            const fileName = normalized.slice(normalized.lastIndexOf("/") + 1)
            const extIndex = fileName.lastIndexOf(".")
            return extIndex > 0 ? fileName.slice(0, extIndex) : fileName
        }

        function buildSuggestedJsonFileName(itemName: string): string {
            const safeName = (itemName || "item")
                .replace(/[\\/:*?"<>|]/g, "_")
                .trim()
            return `${safeName || "item"}.json`
        }

        function buildImportValidationDetails(issues: { path: string, message: string }[]): string {
            const top = issues.slice(0, 5)
                .map(issue => `${issue.path}: ${issue.message}`)
                .join("\n")
            const remain = issues.length > 5
                ? t("itemImportValidationRemaining", {count: issues.length - 5})
                : ""
            return `${top}${remain}`
        }

        function makeVolumeNamePlaceholder(baseName: string): string {
            const taken = new Set(
                openedVolumes.value
                    .map(v => v.name.trim().toLowerCase())
                    .filter(name => !!name)
            )
            let index = 1
            while (taken.has(`${baseName} (${index})`.toLowerCase())) index++
            return `${baseName} (${index})`
        }

        function makeItemKey(item: FileModel) {
            const volume = item.fromVolumeId ?? item.fromFile ?? "unknown-volume"
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

        function getTabItem(index: number): FileModel | undefined {
            const tab = openedTabs.value[index]
            return typeof tab?.data === "string" ? getItemByKey(tab.data) : undefined
        }

        function getOpenedVolumeItem(volumeId: string, itemId: number) {
            return openedVolumes.value.find(volume => volume.id === volumeId)?.items?.find(item => item.id === itemId)
        }

        function getOpenedItemByVolumeEntry(volumeId: string, itemId: number): FileModel | undefined {
            return openedItems.value.find(item => item.fromVolumeId === volumeId && item.id === itemId)
        }

        function makeSessionId(item: FileModel) {
            const source = item.fromVolumeId ?? item.fromFile ?? "unknown"
            return `${source}:${item.id}:${item.name ?? "unnamed"}:${generateId(Date.now())}`
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
            return item.unsaved === true || item.id === -1 || session.undoStack.length > 0
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
            const item = openedItems.value[itemIndex]
            if (!item) return

            session.applying = true
            item.unsaved = false
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

        function openVolumeByPath(filePath: string, openAsCopy: boolean, volumeName?: string) {
            emitWebEvent(makeWebLetter("OpenVolumeByPath", filePath, openAsCopy, volumeName ?? null))
        }

        async function pickVolumeFilePath(): Promise<string | undefined> {
            if (!kurisuState.hostCapabilities.supportsOpenFileDialog) return undefined
            const result = await sendWebCallAndTakeItsReturnValues(
                makeWebLetter("AppCommand:OpenFileDialog", "MassFile", "fs"),
                null
            )
            const path = result[0] as string | null | undefined
            return path ?? undefined
        }

        async function askDuplicateOpenAction(volumeName: string): Promise<"copy" | "locate" | "cancel"> {
            return new Promise(resolve => {
                let action: "copy" | "locate" | "cancel" = "cancel"
                dialogState.order({
                    headline: t("openVolumeConflictTitle", {volume: volumeName}),
                    description: t("openVolumeConflictDescription"),
                    actions: [
                        {text: t("openVolumeConflictOpenAsCopy"), onClick: () => { action = "copy" }},
                        {text: t("openVolumeConflictLocate"), onClick: () => { action = "locate" }},
                        {text: t("cancel"), onClick: () => { action = "cancel" }},
                    ],
                    onClosed: () => resolve(action),
                })
            })
        }

        function locateExplorerPath(path: number[]) {
            appState.sidePanelExpanded = true
            explorerLocateTarget.value = {path: [...path], nonce: Date.now()}
        }

        function locateVolumeInExplorer(volumeId: string): boolean {
            const volumeIndex = openedVolumes.value.findIndex(vol => vol.id === volumeId)
            if (volumeIndex === -1) return false
            locateExplorerPath([volumeIndex])
            return true
        }

        function locateItemInExplorer(volumeId: string, itemId: number): boolean {
            const volumeIndex = openedVolumes.value.findIndex(vol => vol.id === volumeId)
            if (volumeIndex === -1) return false

            const itemIndex = openedVolumes.value[volumeIndex]?.items?.findIndex(item => item.id === itemId) ?? -1
            if (itemIndex === -1) return false

            locateExplorerPath([volumeIndex, itemIndex])
            return true
        }

        function canLocateItemInExplorer(item?: FileModel): boolean {
            if (!item?.fromVolumeId || item.id < 0) return false
            const volumeIndex = openedVolumes.value.findIndex(vol => vol.id === item.fromVolumeId)
            if (volumeIndex === -1) return false
            return (openedVolumes.value[volumeIndex]?.items?.findIndex(entry => entry.id === item.id) ?? -1) !== -1
        }

        function resolveItemNameConflict(
            volumeId: string,
            itemName: string,
            options?: {
                excludeItemKey?: string
                excludeName?: string
            }
        ): ItemNameConflict {
            const normalizedName = itemName.trim()
            const excludeItemKey = options?.excludeItemKey
            const excludeName = options?.excludeName
            const volume = getOpenedVolume(volumeId)
            const backendExists = volume?.items?.find(item =>
                item.name === normalizedName && item.name !== excludeName
            )
            const localDraftExists = openedItems.value.some(item =>
                item.fromVolumeId === volumeId
                && item.id === -1
                && item.name === normalizedName
                && item.itemKey !== excludeItemKey
                && item.name !== excludeName
            )
            const openedConflict = openedItems.value.some(item =>
                item.fromVolumeId === volumeId
                && item.name === normalizedName
                && item.itemKey !== excludeItemKey
                && item.name !== excludeName
                && item.itemKey !== undefined
                && openedTabs.value.some(tab => tab.data === item.itemKey)
            )

            return {
                volume,
                backendExists: backendExists ? {id: backendExists.id, name: backendExists.name} : undefined,
                localDraftExists,
                openedConflict,
                conflict: !!backendExists || localDraftExists,
                overwriteBlocked: localDraftExists || openedConflict,
            }
        }

        function getItemConflictValidator(
            volumeId: string,
            options?: {
                excludeItemKey?: string
                excludeName?: string
            }
        ) {
            const resolveConflict = (itemName: string) => resolveItemNameConflict(volumeId, itemName, options)
            return {
                resolveConflict,
                overwriteDisabled: (itemName: string) => resolveConflict(itemName).overwriteBlocked,
                overwriteDisabledReason: (itemName: string) =>
                    resolveConflict(itemName).overwriteBlocked ? t("itemOverwriteBlockedByOpenedTarget") : undefined,
                nameValidator: (itemName: string, allowOverwrite: boolean) => {
                    const conflict = resolveConflict(itemName)
                    if (conflict.openedConflict) return t("itemOverwriteBlockedByOpenedTarget")
                    if (conflict.conflict && !allowOverwrite) return t("itemRenameConflictHint")
                    return undefined
                }
            }
        }

        function createDraftItem(
            volumeId: string,
            itemName: string,
            ryoType: RyoType,
            data: any,
            allowOverwrite: boolean = false
        ): FileModel {
            const conflict = resolveItemNameConflict(volumeId, itemName)
            if (conflict.overwriteBlocked) throw new Error(t("itemOverwriteBlockedByOpenedTarget"))
            if (conflict.conflict && !allowOverwrite) throw new Error(t("itemRenameConflictHint"))

            const fileModel: FileModel = {
                data: copyData(data),
                tempData: copyData(data),
                fromVolumeId: volumeId,
                fromFile: conflict.volume?.name,
                id: conflict.backendExists && allowOverwrite ? conflict.backendExists.id : -1,
                name: itemName,
                parseSuccess: true,
                volumeRevision: getVolumeRevision(volumeId),
                ryoType,
                dataTypeName: appState.getDataTypeNameByRyoType(ryoType),
                unsaved: true
            }

            ensureItemKey(fileModel)
            openedItems.value.push(fileModel)
            ensureItemSession(fileModel.itemKey!)
            return fileModel
        }

        function buildImportPayload(
            rawData: any,
            ryoType: RyoType,
            dataTypeName: string,
            sourceTypeName?: string
        ) {
            if (sourceTypeName && sourceTypeName !== dataTypeName) {
                throw new Error(t("itemImportTypeMismatch", {expected: dataTypeName, actual: sourceTypeName}))
            }

            const payload = decodeImportedItemData(rawData, ryoType, typeName => appState.getRyoTypeByDataTypeName(typeName))
            const validation = appState.validateDataByRyoType(ryoType, payload)
            if (!validation.valid) {
                throw new Error(t("itemImportValidationFailed", {
                    details: buildImportValidationDetails(validation.issues)
                }))
            }

            return payload
        }

        async function pickJsonFileToOpen(): Promise<string | undefined> {
            if (!kurisuState.hostCapabilities.supportsOpenFileDialog) return undefined
            const result = await sendWebCallAndTakeItsReturnValues(
                makeWebLetter("AppCommand:OpenFileDialog", t("jsonFileDescription"), "json"),
                null
            )
            const path = result[0] as string | null | undefined
            return path ?? undefined
        }

        async function pickJsonFileToSave(suggestedFileName: string): Promise<string | undefined> {
            if (!kurisuState.hostCapabilities.supportsSaveFileDialog) return undefined
            const result = await sendWebCallAndTakeItsReturnValues(
                makeWebLetter("AppCommand:SaveFileDialog", t("jsonFileDescription"), "json", suggestedFileName),
                null
            )
            const path = result[0] as string | null | undefined
            return path ?? undefined
        }

        async function readTextFile(filePath: string): Promise<string> {
            const result = await sendWebCallAndTakeItsReturnValues(makeWebLetter("ReadTextFile", filePath))
            return result[0] as string
        }

        async function writeTextFile(filePath: string, content: string): Promise<void> {
            await sendWebCallAndTakeItsReturnValues(makeWebLetter("WriteTextFile", filePath, content))
        }

        function newVolume() {
            const base = t("newVolumeNamePlaceholderBase")
            const placeholder = makeVolumeNamePlaceholder(base)
            dialogState.orderSpecial(NameEditDialog, {
                headline: t("createVolume"),
                description: t("createVolumeDesc"),
                nameLabel: t("volumeName"),
                placeholder,
                allowPlaceholderSubmit: true,
                confirm: (value: string) => {
                    emitWebEvent(makeWebLetter("CreateVolume", value))
                }
            })
        }

        async function openVolume() {
            const filePath = await pickVolumeFilePath()
            if (!filePath) return

            const normalized = normalizeLocalPath(filePath)
            const existed = openedVolumes.value.find(v => v.localPath && normalizeLocalPath(v.localPath) === normalized)

            if (!existed) {
                openVolumeByPath(filePath, false)
                return
            }

            const action = await askDuplicateOpenAction(existed.name)
            if (action === "cancel") return
            if (action === "locate") {
                locateVolumeInExplorer(existed.id)
                return
            }

            const base = existed.name || t("copySuffixBase")
            const placeholder = makeVolumeNamePlaceholder(base)
            dialogState.orderSpecial(NameEditDialog, {
                headline: t("openVolumeAsCopy"),
                description: t("openVolumeAsCopyDesc"),
                nameLabel: t("volumeName"),
                placeholder,
                confirm: (value: string) => {
                    openVolumeByPath(filePath, true, value)
                }
            })
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

        async function closeTab(index: number): Promise<boolean> {
            const tab = openedTabs.value[index]
            if (!tab) return true

            activeTabIndex.value = index

            const closeCurrentTab = () => {
                if (typeof tab.data === "string" && getItemByKey(tab.data)) removeOpenedItemByKey(tab.data)
                else internalCloseTab(tab, index)
            }

            if (!getIsTabUnsaved(index)) {
                closeCurrentTab()
                return true
            }

            const action = await askUnsavedItemAction(tab.name)
            if (action === "cancel") return false
            if (action === "save" && typeof tab.data === "string") {
                const saved = await saveItemByKey(tab.data, true)
                if (!saved) return false
            }

            closeCurrentTab()
            return true
        }

        async function closeTabsByKeys(tabKeys: string[]): Promise<boolean> {
            const uniqueKeys = [...new Set(tabKeys)]
            for (const tabKey of uniqueKeys) {
                const index = openedTabs.value.findIndex(tab => tab.key === tabKey)
                if (index === -1) continue
                const closed = await closeTab(index)
                if (!closed) return false
            }
            return true
        }

        async function closeAllTabs(): Promise<boolean> {
            return closeTabsByKeys(
                openedTabs.value
                    .map(tab => tab.key)
                    .filter((key): key is string => !!key)
            )
        }

        async function closeOtherTabs(index: number): Promise<boolean> {
            const keepKey = openedTabs.value[index]?.key
            if (!keepKey) return true

            return closeTabsByKeys(
                openedTabs.value
                    .filter(tab => tab.key && tab.key !== keepKey)
                    .map(tab => tab.key!)
            )
        }

        async function closeSavedTabs(): Promise<boolean> {
            return closeTabsByKeys(
                openedTabs.value
                    .filter((_, index) => !getIsTabUnsaved(index))
                    .map(tab => tab.key)
                    .filter((key): key is string => !!key)
            )
        }

        async function closeTabsToLeft(index: number): Promise<boolean> {
            return closeTabsByKeys(
                openedTabs.value
                    .slice(0, index)
                    .map(tab => tab.key)
                    .filter((key): key is string => !!key)
            )
        }

        async function closeTabsToRight(index: number): Promise<boolean> {
            return closeTabsByKeys(
                openedTabs.value
                    .slice(index + 1)
                    .map(tab => tab.key)
                    .filter((key): key is string => !!key)
            )
        }

        async function confirmAppExit(): Promise<boolean> {
            return new Promise(resolve => {
                let shouldExit = false
                dialogState.order({
                    icon: "ryo",
                    headline: t("exitRyo"),
                    description: t("areYouSureToExit"),
                    actions: [
                        {text: t("cancel"), onClick: () => { shouldExit = false }},
                        {text: t("exit"), onClick: () => { shouldExit = true }},
                    ],
                    onClosed: () => resolve(shouldExit),
                })
            })
        }

        async function ensureTabsSavedBeforeAppExit(): Promise<boolean> {
            const handledItemKeys = new Set<string>()
            const tabKeys = openedTabs.value
                .map(tab => tab.key)
                .filter((key): key is string => !!key)

            for (const tabKey of tabKeys) {
                const index = openedTabs.value.findIndex(tab => tab.key === tabKey)
                if (index === -1) continue

                const tab = openedTabs.value[index]
                if (!getIsTabUnsaved(index)) continue

                if (typeof tab.data === "string") {
                    if (handledItemKeys.has(tab.data)) continue
                    handledItemKeys.add(tab.data)
                }

                const action = await askUnsavedItemAction(tab.name)
                if (action === "cancel") return false
                if (action === "save" && typeof tab.data === "string") {
                    const saved = await saveItemByKey(tab.data, true)
                    if (!saved) return false
                }
            }

            return true
        }

        async function ensureVolumesSavedBeforeAppExit(): Promise<boolean> {
            const volumeIds = openedVolumes.value.map(volume => volume.id)
            for (const volumeId of volumeIds) {
                const volume = getOpenedVolume(volumeId)
                if (!volume || !isVolumePendingSave(volume)) continue

                const volumeName = volume.name ?? t("unknown")
                const action = await askUnsavedVolumeAction(volumeName)
                if (action === "cancel") return false

                if (action === "save") {
                    saveVolume(volumeId)
                    const clean = await waitForVolumeClean(volumeId)
                    if (!clean) {
                        await showVolumeSaveIncompleteDialog(volumeName)
                        return false
                    }
                }
            }

            return true
        }

        async function tryExitApp(): Promise<boolean> {
            if (exitFlowRunning) return false
            exitFlowRunning = true

            try {
                if (!await ensureTabsSavedBeforeAppExit()) return false
                if (!await ensureVolumesSavedBeforeAppExit()) return false
                if (!await confirmAppExit()) return false

                kurisuState.stopApp()
                return true
            } finally {
                exitFlowRunning = false
            }
        }

        function canRenameTabItem(index: number): boolean {
            return !!getTabItem(index)
        }

        function canCopyTabItem(index: number): boolean {
            const item = getTabItem(index)
            return !!item?.fromVolumeId && !!item.name && item.id !== -1
        }

        function canExportTabItem(index: number): boolean {
            const item = getTabItem(index)
            return !!item?.itemKey && !!item.ryoType && !!item.dataTypeName && kurisuState.hostCapabilities.supportsSaveFileDialog
        }

        function canImportTabItem(index: number): boolean {
            const item = getTabItem(index)
            return !!item?.itemKey && !!item.ryoType && !!item.dataTypeName && kurisuState.hostCapabilities.supportsOpenFileDialog
        }

        function canLocateTabInExplorer(index: number): boolean {
            const item = getTabItem(index)
            if (!item?.fromVolumeId || item.id < 0) return false
            const volumeIndex = openedVolumes.value.findIndex(vol => vol.id === item.fromVolumeId)
            if (volumeIndex === -1) return false
            return (openedVolumes.value[volumeIndex]?.items?.findIndex(entry => entry.id === item.id) ?? -1) !== -1
        }

        function locateTabInExplorer(index: number) {
            const item = getTabItem(index)
            if (!item?.fromVolumeId || item.id < 0) return
            locateItemInExplorer(item.fromVolumeId, item.id)
        }

        function reorderTabs(nextTabs: TabModel[]) {
            const activeTabKey = openedTabs.value[activeTabIndex.value]?.key
            openedTabs.value = [...nextTabs]

            if (openedTabs.value.length === 0) {
                activeTabIndex.value = -1
                return
            }

            if (activeTabKey) {
                const nextActiveIndex = openedTabs.value.findIndex(tab => tab.key === activeTabKey)
                activeTabIndex.value = nextActiveIndex === -1 ? 0 : nextActiveIndex
                return
            }

            activeTabIndex.value = Math.min(activeTabIndex.value, openedTabs.value.length - 1)
        }

        function renameTabItem(index: number) {
            const item = getTabItem(index)
            if (!item?.itemKey) return
            renameItemByKey(item.itemKey)
        }

        function copyTabItem(index: number) {
            const item = getTabItem(index)
            if (!item?.fromVolumeId || !item.name || item.id === -1) return
            copyItem(item.fromVolumeId, item.name)
        }

        async function exportTabItem(index: number) {
            const item = getTabItem(index)
            if (!item?.itemKey) return
            await exportItemByKey(item.itemKey)
        }

        async function importTabItem(index: number) {
            const item = getTabItem(index)
            if (!item?.itemKey) return
            await importItemByKey(item.itemKey)
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

        async function deleteItem(volumeId: string, itemName: string) {
            const openedItem = openedItems.value.find(item =>
                item.fromVolumeId === volumeId && item.name === itemName && item.id !== -1
            )
            const shouldDelete = await askDeleteOpenedItemAction(
                itemName,
                !!openedItem,
                !!openedItem && isItemUnsaved(openedItem)
            )
            if (!shouldDelete) return

            try {
                pendingLocalVolumeRevisionSync.add(volumeId)
                await sendWebCallAndTakeItsReturnValues(makeWebLetter("DeleteItem", volumeId, itemName))
                if (openedItem?.itemKey) removeOpenedItemByKey(openedItem.itemKey)
            } catch (err) {
                pendingLocalVolumeRevisionSync.delete(volumeId)
                console.error(TAG, "删除项目失败", volumeId, itemName, err)
                await showOperationErrorDialog(t("deleteItemFailed", {item: itemName}), err)
            }
        }

        function renameItem(volumeId: string, oldItemName: string, options?: { anchorItemKey?: string }) {
            const validator = getItemConflictValidator(volumeId, {excludeName: oldItemName})
            dialogState.orderSpecial(NameEditDialog, {
                headline: t("renameItem"),
                nameLabel: t("itemName"),
                oldName: oldItemName,
                placeholder: oldItemName,
                showOverwrite: true,
                overwriteLabel: t("allowOverwriteSameName"),
                overwriteDisabled: validator.overwriteDisabled,
                overwriteDisabledReason: validator.overwriteDisabledReason,
                nameValidator: validator.nameValidator,
                confirm: (itemName: string, allowOverwrite?: boolean) => {
                    if (options?.anchorItemKey) markItemTabsResident(options.anchorItemKey)
                    pendingLocalVolumeRevisionSync.add(volumeId)
                    emitWebEvent(makeWebLetter("RenameItem", volumeId, oldItemName, itemName, !!allowOverwrite))
                }
            })
        }

        function renameItemByKey(itemKey: string) {
            const item = getItemByKey(itemKey)
            if (!item?.fromVolumeId || !item.name) return

            if (item.id !== -1) {
                renameItem(item.fromVolumeId, item.name, {anchorItemKey: itemKey})
                return
            }

            const validator = getItemConflictValidator(item.fromVolumeId, {
                excludeItemKey: item.itemKey,
                excludeName: item.name,
            })

            dialogState.orderSpecial(NameEditDialog, {
                headline: t("renameItem"),
                nameLabel: t("itemName"),
                oldName: item.name,
                placeholder: item.name,
                showOverwrite: true,
                overwriteLabel: t("allowOverwriteSameName"),
                overwriteDisabled: validator.overwriteDisabled,
                overwriteDisabledReason: validator.overwriteDisabledReason,
                nameValidator: validator.nameValidator,
                confirm: (nextName: string, allowOverwrite?: boolean) => {
                    const conflict = resolveItemNameConflict(item.fromVolumeId!, nextName, {
                        excludeItemKey: item.itemKey,
                        excludeName: item.name,
                    })
                    item.name = nextName
                    item.fromFile = conflict.volume?.name ?? item.fromFile
                    item.volumeRevision = getVolumeRevision(item.fromVolumeId)
                    item.id = conflict.backendExists && allowOverwrite ? conflict.backendExists.id : -1
                    item.unsaved = true
                    const itemIndex = getItemIndexByKey(itemKey)
                    if (itemIndex !== -1) refreshItemTabState(itemIndex)
                    markItemTabsResident(itemKey)
                    ensureItemSession(itemKey)
                }
            })
        }

        async function copyItem(volumeId: string, sourceItemName: string) {
            const openedSource = openedItems.value.find(item =>
                item.fromVolumeId === volumeId && item.name === sourceItemName && item.id !== -1
            )
            if (openedSource?.itemKey && isItemUnsaved(openedSource)) {
                const action = await askSaveBeforeCopyItemAction(sourceItemName)
                if (action !== "save") return

                const saved = await saveItemByKey(openedSource.itemKey, true)
                if (!saved) return
            }

            const base = sourceItemName || t("copySuffixBase")
            const placeholder = `${base} ${t("copySuffixBase")}`.trim()
            const validator = getItemConflictValidator(volumeId)

            dialogState.orderSpecial(NameEditDialog, {
                headline: t("makeItemCopy"),
                description: t("makeItemCopyDesc", {item: sourceItemName}),
                nameLabel: t("itemName"),
                placeholder,
                allowPlaceholderSubmit: true,
                showOverwrite: true,
                overwriteLabel: t("allowOverwriteSameName"),
                overwriteDisabled: validator.overwriteDisabled,
                overwriteDisabledReason: validator.overwriteDisabledReason,
                nameValidator: validator.nameValidator,
                confirm: async (itemName: string, allowOverwrite?: boolean) => {
                    pendingLocalVolumeRevisionSync.add(volumeId)
                    try {
                        await sendWebCallAndTakeItsReturnValues(
                            makeWebLetter("CopyItem", volumeId, sourceItemName, itemName, !!allowOverwrite)
                        )
                    } catch (err) {
                        pendingLocalVolumeRevisionSync.delete(volumeId)
                        console.error(TAG, "创建项目副本失败", volumeId, sourceItemName, itemName, err)
                        await showOperationErrorDialog(t("copyItemFailed", {item: sourceItemName}), err)
                        return false
                    }
                }
            })
        }

        function renameVolume(volumeId: string, oldVolumeName: string) {
            const placeholder = oldVolumeName
            dialogState.orderSpecial(NameEditDialog, {
                headline: t("renameVolume"),
                nameLabel: t("volumeName"),
                oldName: oldVolumeName,
                placeholder,
                confirm: (newName: string) => {
                    emitWebEvent(makeWebLetter("RenameVolume", volumeId, newName))
                }
            })
        }

        function cloneVolume(volumeId: string) {
            const target = getOpenedVolume(volumeId)
            if (!target) return
            const placeholder = makeVolumeNamePlaceholder(target.name || t("copySuffixBase"))
            dialogState.orderSpecial(NameEditDialog, {
                headline: t("cloneVolume"),
                description: t("cloneVolumeDesc"),
                nameLabel: t("volumeName"),
                placeholder,
                allowPlaceholderSubmit: true,
                confirm: (newName: string) => {
                    emitWebEvent(makeWebLetter("CloneVolume", volumeId, newName))
                }
            })
        }

        async function exportItemData(itemName: string, ryoType: RyoType, dataTypeName: string, payload: any) {
            const filePath = await pickJsonFileToSave(buildSuggestedJsonFileName(itemName))
            if (!filePath) return

            const envelope = buildItemExchangeEnvelope(
                copyData(payload),
                ryoType,
                dataTypeName,
                typeName => appState.getRyoTypeByDataTypeName(typeName)
            )
            await writeTextFile(filePath, `${JSON.stringify(envelope, null, 2)}\n`)
        }

        async function buildImportPayloadFromFile(filePath: string, ryoType: RyoType, dataTypeName: string) {
            const parsed = parseItemExchangeDocument(await readTextFile(filePath))
            return buildImportPayload(
                parsed.data,
                ryoType,
                dataTypeName,
                parsed.envelope?.dataTypeName
            )
        }

        async function requestImportPayloadForItem(item: Pick<FileModel, "ryoType" | "dataTypeName">) {
            if (!item.ryoType || !item.dataTypeName) return undefined
            const filePath = await pickJsonFileToOpen()
            if (!filePath) return undefined
            return buildImportPayloadFromFile(filePath, item.ryoType, item.dataTypeName)
        }

        async function askImportReplaceItemAction(itemName: string, dirty: boolean): Promise<boolean> {
            return new Promise(resolve => {
                let shouldImport = false
                dialogState.order({
                    headline: t("importReplaceConfirmTitle", {item: itemName}),
                    description: dirty
                        ? t("importReplaceDirtyDescription")
                        : t("importReplaceConfirmDescription"),
                    actions: [
                        {text: t("cancel"), onClick: () => { shouldImport = false }},
                        {text: t("importReplaceItem"), onClick: () => { shouldImport = true }},
                    ],
                    onClosed: () => resolve(shouldImport),
                })
            })
        }

        async function ensureOpenedItemByVolumeEntry(volumeId: string, itemId: number, activateTab: boolean): Promise<FileModel | undefined> {
            let item = getOpenedItemByVolumeEntry(volumeId, itemId)
            if (!item) {
                const fileModel = await getFullFileModel(volumeId, itemId)
                if (!ensure(fileModel)) return undefined
                ensureItemKey(fileModel)
                openedItems.value.push(fileModel)
                item = fileModel
            }

            if (activateTab && item.itemKey) openTab(TabType.Item, item.itemKey)
            return item
        }

        async function exportItemByKey(itemKey: string) {
            const item = getItemByKey(itemKey)
            if (!item?.ryoType || !item.dataTypeName) return

            try {
                await exportItemData(
                    item.name ?? "item",
                    item.ryoType,
                    item.dataTypeName,
                    item.tempData ?? item.data
                )
            } catch (err) {
                console.error(TAG, "导出项目失败", item.name, err)
                await showOperationErrorDialog(t("exportCurrentItemFailed", {item: item.name ?? t("unknown")}), err)
            }
        }

        async function exportVolumeItem(volumeId: string, itemId: number) {
            const openedItem = getOpenedItemByVolumeEntry(volumeId, itemId)
            if (openedItem?.itemKey) {
                await exportItemByKey(openedItem.itemKey)
                return
            }

            try {
                const snapshot = await getFullFileModel(volumeId, itemId)
                if (!snapshot?.ryoType || !snapshot.dataTypeName) return
                await exportItemData(
                    snapshot.name ?? "item",
                    snapshot.ryoType,
                    snapshot.dataTypeName,
                    snapshot.data
                )
            } catch (err) {
                const itemName = getOpenedVolumeItem(volumeId, itemId)?.name ?? t("unknown")
                console.error(TAG, "导出资源树项目失败", volumeId, itemId, err)
                await showOperationErrorDialog(t("exportCurrentItemFailed", {item: itemName}), err)
            }
        }

        async function importItemByKey(itemKey: string) {
            const item = getItemByKey(itemKey)
            if (!item?.itemKey || !item.ryoType || !item.dataTypeName) return

            try {
                const payload = await requestImportPayloadForItem(item)
                if (payload === undefined) return
                const shouldImport = await askImportReplaceItemAction(
                    item.name ?? t("unknown"),
                    isItemUnsaved(item)
                )
                if (!shouldImport) return

                recordItemSessionChange(item.itemKey, payload)
            } catch (err) {
                console.error(TAG, "导入当前项目失败", item.name, err)
                await showOperationErrorDialog(t("importCurrentItemFailed", {item: item.name ?? t("unknown")}), err)
            }
        }

        async function importVolumeItem(volumeId: string, itemId: number) {
            const openedItem = getOpenedItemByVolumeEntry(volumeId, itemId)
            if (openedItem?.itemKey) {
                await importItemByKey(openedItem.itemKey)
                return
            }

            try {
                const snapshot = await getFullFileModel(volumeId, itemId)
                if (!snapshot?.ryoType || !snapshot.dataTypeName) return

                const payload = await requestImportPayloadForItem(snapshot)
                if (payload === undefined) return

                const shouldImport = await askImportReplaceItemAction(snapshot.name ?? t("unknown"), false)
                if (!shouldImport) return

                const targetItem = getOpenedItemByVolumeEntry(volumeId, itemId) ?? snapshot
                if (targetItem === snapshot) {
                    ensureItemKey(snapshot)
                    openedItems.value.push(snapshot)
                }

                if (!targetItem.itemKey) return
                openTab(TabType.Item, targetItem.itemKey)
                recordItemSessionChange(targetItem.itemKey, payload)
            } catch (err) {
                const itemName = getOpenedVolumeItem(volumeId, itemId)?.name ?? t("unknown")
                console.error(TAG, "导入资源树项目失败", volumeId, itemId, err)
                await showOperationErrorDialog(t("importCurrentItemFailed", {item: itemName}), err)
            }
        }

        async function exportCurrentItem() {
            if (!activeItem.value?.itemKey) return
            await exportItemByKey(activeItem.value.itemKey)
        }

        async function importCurrentItem() {
            if (!activeItem.value?.itemKey) return
            await importItemByKey(activeItem.value.itemKey)
        }

        async function requestImportItemFile() {
            try {
                const filePath = await pickJsonFileToOpen()
                if (!filePath) return undefined

                const parsed = parseItemExchangeDocument(await readTextFile(filePath))
                return {
                    filePath,
                    suggestedItemName: getFileNameWithoutExtension(filePath),
                    suggestedDataTypeName: parsed.suggestedDataTypeName,
                }
            } catch (err) {
                return {
                    error: formatErrorReason(err),
                }
            }
        }

        function importItemIntoVolume(volumeId: string) {
            const validator = getItemConflictValidator(volumeId)

            dialogState.orderSpecial(ImportItemDialog, {
                showOverwrite: true,
                overwriteLabel: t("allowOverwriteSameName"),
                overwriteDisabled: validator.overwriteDisabled,
                overwriteDisabledReason: validator.overwriteDisabledReason,
                nameValidator: validator.nameValidator,
                requestFile: requestImportItemFile,
                confirm: async (filePath: string, itemName: string, ryoType: RyoType, allowOverwrite?: boolean) => {
                    try {
                        const dataTypeName = appState.getDataTypeNameByRyoType(ryoType)
                        const payload = await buildImportPayloadFromFile(filePath, ryoType, dataTypeName)
                        const draft = createDraftItem(volumeId, itemName, ryoType, payload, !!allowOverwrite)
                        openTab(TabType.Item, draft.itemKey)
                        return {close: true} satisfies ImportDialogSubmitResult
                    } catch (err) {
                        console.error(TAG, "导入为新项目失败", volumeId, itemName, filePath, err)
                        return {
                            close: false,
                            error: formatErrorReason(err),
                        } satisfies ImportDialogSubmitResult
                    }
                }
            })
        }

        async function mentionItem(volumeId: string, itemId: number) {
            console.log(TAG, "提及项目", volumeId, itemId)
            await ensureOpenedItemByVolumeEntry(volumeId, itemId, true)
        }

        async function getFullFileModel(volumeId: string, itemId: number) {
            console.log(TAG, "获取项目", volumeId, itemId)
            const fileModel = (await sendWebCallAndTakeItsReturnValues(makeWebLetter('GetFullFileModel', volumeId, itemId)))[0] as FileModel
            if (!fileModel.fromVolumeId) fileModel.fromVolumeId = volumeId
            if (!fileModel.fromFile) fileModel.fromFile = getOpenedVolume(volumeId)?.name
            fileModel.ryoType = appState.getRyoTypeByDataTypeName(fileModel.dataTypeName!)
            fileModel.unsaved = false
            if (fileModel.volumeRevision === undefined) fileModel.volumeRevision = getVolumeRevision(volumeId)
            ensureItemKey(fileModel)
            console.log(TAG, "获取到项目", volumeId, itemId, fileModel)

            return fileModel
        }

        function saveVolume(volumeId: string) {
            emitWebEvent(makeWebLetter('SaveVolume', volumeId, false))
        }

        function saveVolumeAs(volumeId: string) {
            emitWebEvent(makeWebLetter('SaveVolume', volumeId, true))
        }

        async function saveItem(
            volumeId: string,
            itemId: number,
            itemName: string,
            data: any,
            tsRyoTypeName: string,
            expectedVolumeRevision: number
        ) {
            const ret = await sendWebCallAndTakeItsReturnValues(
                makeWebLetter('SaveItem', volumeId, itemId, itemName, data, tsRyoTypeName, expectedVolumeRevision)
            )
            return {
                itemId: ret[0] as number,
                volumeRevision: ret[1] as number,
            }
        }

        function buildValidationError(item: FileModel, payload: any): CommandError | null {
            if (!item.fromVolumeId || !item.name || !item.dataTypeName) return {
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
                    item.fromVolumeId!,
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
                    icon: "error",
                    headline: t("saveItemFailed", {item: itemName}),
                    description: formatErrorReason(reason),
                    actions: [{text: t('confirm')}],
                    onClosed: () => resolve(),
                })
            })
        }

        async function showOperationErrorDialog(headline: string, reason: any): Promise<void> {
            return new Promise(resolve => {
                dialogState.order({
                    icon: "error",
                    headline,
                    description: formatErrorReason(reason),
                    actions: [{text: t("confirm")}],
                    onClosed: () => resolve(),
                })
            })
        }

        async function askSaveBeforeCopyItemAction(itemName: string): Promise<"save" | "cancel"> {
            return new Promise(resolve => {
                let action: "save" | "cancel" = "cancel"
                dialogState.order({
                    headline: t("copyItemRequiresSavedSourceTitle", {item: itemName}),
                    description: t("copyItemRequiresSavedSourceDescription"),
                    actions: [
                        {text: t("cancel"), onClick: () => { action = "cancel" }},
                        {text: t("saveThenCopy"), onClick: () => { action = "save" }},
                    ],
                    onClosed: () => resolve(action),
                })
            })
        }

        async function askDeleteOpenedItemAction(itemName: string, opened: boolean, dirty: boolean): Promise<boolean> {
            return new Promise(resolve => {
                let shouldDelete = false
                dialogState.order({
                    icon: "discard",
                    headline: t("deleteItemConfirmTitle", {item: itemName}),
                    description: !opened
                        ? t("deleteItemConfirmDescription")
                        : dirty
                            ? t("deleteItemOpenedDirtyDescription")
                            : t("deleteItemOpenedDescription"),
                    actions: [
                        {text: t("cancel"), onClick: () => { shouldDelete = false }},
                        {text: t("delete"), onClick: () => { shouldDelete = true }},
                    ],
                    onClosed: () => resolve(shouldDelete),
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
                syncOpenedItemVolumeRevision(item.fromVolumeId!, saved.volumeRevision)
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

        async function waitForVolumeClean(volumeId: string, timeoutMs: number = 8000): Promise<boolean> {
            const started = Date.now()
            while (Date.now() - started <= timeoutMs) {
                const volume = getOpenedVolume(volumeId)
                if (!volume) return true
                if (!isVolumePendingSave(volume)) return true
                await wait(120)
            }

            const latest = getOpenedVolume(volumeId)
            return !isVolumePendingSave(latest)
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

        function removeOpenedItemsByVolume(volumeId: string) {
            const targets = openedItems.value
                .filter(item => item.fromVolumeId === volumeId)
                .map(item => item.itemKey)
                .filter((k): k is string => !!k)

            targets.forEach(removeOpenedItemByKey)
        }

        function removeOpenedItemsByKeys(itemKeys: string[]) {
            const uniqueKeys = [...new Set(itemKeys)]
            uniqueKeys.forEach(removeOpenedItemByKey)
        }

        async function closeVolume(volumeId: string) {
            if (closingVolumes.has(volumeId)) return
            closingVolumes.add(volumeId)

            try {
                const volumeName = getOpenedVolume(volumeId)?.name ?? t("unknown")
                let savedAnyItemInThisCloseFlow = false
                const targets = openedItems.value
                    .filter(item => item.fromVolumeId === volumeId && item.itemKey)
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

                const volume = getOpenedVolume(volumeId)
                if (isVolumePendingSave(volume) || savedAnyItemInThisCloseFlow) {
                    const action = await askUnsavedVolumeAction(volumeName)
                    if (action === "cancel") return

                    if (action === "save") {
                        saveVolume(volumeId)
                        const clean = await waitForVolumeClean(volumeId)
                        if (!clean) {
                            await showVolumeSaveIncompleteDialog(volumeName)
                            return
                        }
                    }
                }

                removeOpenedItemsByVolume(volumeId)
                emitWebEvent(makeWebLetter('CloseVolume', volumeId))
            } finally {
                closingVolumes.delete(volumeId)
            }
        }

        function gcVolume(volumeId: string) {
            emitWebEvent(makeWebLetter('GcVolume', volumeId))
        }

        function addItemInVolume(volumeId: string) {
            const validator = getItemConflictValidator(volumeId)

            dialogState.orderSpecial(AddItemDialog, {
                showOverwrite: true,
                overwriteLabel: t("allowOverwriteSameName"),
                overwriteDisabled: validator.overwriteDisabled,
                overwriteDisabledReason: validator.overwriteDisabledReason,
                nameValidator: validator.nameValidator,
                confirm: (itemName: string, ryoType: RyoType, allowOverwrite?: boolean) => {
                    try {
                        const fileModel = createDraftItem(
                            volumeId,
                            itemName,
                            ryoType,
                            appState.getInitValue(ryoType),
                            !!allowOverwrite
                        )
                        openTab(TabType.Item, fileModel.itemKey)
                    } catch (err) {
                        console.error(TAG, "新建项目草稿失败", volumeId, itemName, err)
                    }
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

                    const openedById = new Map(openedVolumes.value.map(v => [v.id, v]))
                    const openedByName = new Map<string, VolumeModel[]>()
                    const staleItemKeys: string[] = []

                    openedVolumes.value.forEach(volume => {
                        const group = openedByName.get(volume.name) ?? []
                        group.push(volume)
                        openedByName.set(volume.name, group)
                    })

                    openedItems.value.forEach(item => {
                        if (!item.fromVolumeId && item.fromFile) {
                            const candidates = openedByName.get(item.fromFile) ?? []
                            if (candidates.length === 1) item.fromVolumeId = candidates[0].id
                            else {
                                console.warn(TAG, "无法通过卷名唯一定位Item归属，将移除本地项", item.fromFile, item.name, candidates.length)
                            }
                        }

                        const volumeId = item.fromVolumeId
                        if (!volumeId || !openedById.has(volumeId)) {
                            staleItemKeys.push(ensureItemKey(item))
                            return
                        }

                        const volume = openedById.get(volumeId)!
                        item.fromFile = volume.name
                        if (pendingLocalVolumeRevisionSync.has(volumeId) || item.id === -1 || item.volumeRevision === undefined)
                            item.volumeRevision = volume.revision
                    })

                    openedVolumes.value.forEach(volume => pendingLocalVolumeRevisionSync.delete(volume.id))

                    if (staleItemKeys.length > 0) {
                        console.warn(TAG, "移除失效或无法定位归属的Item数量", staleItemKeys.length)
                        removeOpenedItemsByKeys(staleItemKeys)
                    }
                })
                console.log(TAG, "OpenedVolumesChanged监听器已创建")

                addWebEventListener("VolumeItemRenamed", (args: string[]) => {
                    const [volumeId, oldName, newName] = args
                    console.log(TAG, volumeId, "接收重命名", oldName, "->", newName)

                    const itemIndex = openedItems.value.findIndex(i => i.fromVolumeId === volumeId && i.name === oldName)
                    const item = itemIndex === -1 ? undefined : openedItems.value[itemIndex]
                    if (item) {
                        item.name = newName
                        refreshItemTabState(itemIndex)
                    }
                })
                console.log(TAG, "ItemRenamed监听器已创建")

                addWebEventListener("VolumeItemDeleted", (args: any[]) => {
                    const [volumeId, id] = args as [string, number]
                    console.log(TAG, volumeId, "接收删除", id)
                    const staleKeys = openedItems.value
                        .filter(item => item.fromVolumeId === volumeId && item.id === id && item.itemKey)
                        .map(item => item.itemKey!)
                    if (staleKeys.length > 0) removeOpenedItemsByKeys(staleKeys)
                })
                console.log(TAG, "ItemDeleted监听器已创建")

                addWebEventListener("VolumeIdsRemapped", (args: any[]) => {
                    const [volumeId, intArr] = args as [string, number[]]
                    console.log(TAG, volumeId, "已打开项目的ID重新同步", intArr) // 主要是处理openedItems里面的ID
                    openedItems.value.forEach(item => {
                        if (item.fromVolumeId === volumeId && item.id !== -1) {
                            const newId = intArr[item.id]
                            if (newId !== -1) item.id = newId
                        }
                    })
                })
                console.log(TAG, "VolumeIdsRemapped监听器已创建")

                addWebEventListener("HostWindow:CloseRequested", () => {
                    console.log(TAG, "宿主请求关闭窗口")
                    void tryExitApp()
                })
                console.log(TAG, "HostWindow:CloseRequested监听器已创建")

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
            activeVolumeId,
            activeVolume,
            anchorTab,
            available,
            clickTab,
            closeTab,
            closeAllTabs,
            closeOtherTabs,
            closeSavedTabs,
            closeTabsToLeft,
            closeTabsToRight,
            reorderTabs,
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
            renameItemByKey,
            renameVolume,
            cloneVolume,
            copyItem,
            exportCurrentItem,
            importCurrentItem,
            importItemIntoVolume,
            pageUndo,
            undoItemSession,
            redoItemSession,
            canUndoItemSession,
            canRedoItemSession,
            canRenameTabItem,
            canCopyTabItem,
            canExportTabItem,
            canImportTabItem,
            canLocateTabInExplorer,
            copyTabItem,
            exportTabItem,
            importTabItem,
            exportVolumeItem,
            importVolumeItem,
            locateTabInExplorer,
            locateItemInExplorer,
            renameTabItem,
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
            explorerLocateTarget,
            tryExitApp,
        }
    }
)
