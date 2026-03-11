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
        const explorerLocateTarget = ref<{volumeId: string, nonce: number} | null>(null)

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

        function hasBoundVolumePath(volume?: VolumeModel): boolean {
            return !!(volume?.localPath && volume.localPath.trim().length > 0)
        }

        function isVolumePendingSave(volume?: VolumeModel): boolean {
            if (!volume) return false
            return volume.unsaved === true || !hasBoundVolumePath(volume)
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

        function openVolumeByPath(filePath: string, openAsCopy: boolean, volumeName?: string) {
            emitWebEvent(makeWebLetter("OpenVolumeByPath", filePath, openAsCopy, volumeName ?? null))
        }

        async function pickVolumeFilePath(): Promise<string | undefined> {
            if (!kurisuState.hostCapabilities.supportsOpenFileDialog) return undefined
            const result = await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppCommand:OpenFileDialog", "MassFile", "fs"))
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

        function locateVolumeInExplorer(volumeId: string) {
            appState.sidePanelExpanded = true
            explorerLocateTarget.value = {volumeId, nonce: Date.now()}
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
                    headline: t("saveItemChangesPromptTitle", {item: tab.name}),
                    description: t("saveItemChangesPromptDescription"),
                    actions: [
                        {
                            text: t("save"), onClick() {
                                closeAction = "save"
                            }
                        },
                        {
                            text: t("dontSave"), onClick() {
                                closeAction = "discard"
                            }
                        },
                        {
                            text: t("cancel"), onClick() {
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

        function deleteItem(volumeId: string, itemName: string) {
            emitWebEvent(makeWebLetter('DeleteItem', volumeId, itemName))
        }

        function renameItem(volumeId: string, oldItemName: string) {
            const resolveRenameConflict = (newItemName: string) => {
                const volume = getOpenedVolume(volumeId)
                const backendConflict = !!volume?.items?.some(item =>
                    item.name === newItemName && item.name !== oldItemName
                )
                const localConflict = openedItems.value.some(item =>
                    item.fromVolumeId === volumeId &&
                    item.id === -1 &&
                    item.name === newItemName &&
                    item.name !== oldItemName
                )
                const openedConflict = openedItems.value.some(item =>
                    item.fromVolumeId === volumeId &&
                    item.name === newItemName &&
                    item.name !== oldItemName &&
                    item.itemKey !== undefined &&
                    openedTabs.value.some(tab => tab.data === item.itemKey)
                )

                return {
                    conflict: backendConflict || localConflict,
                    openedConflict,
                }
            }

            dialogState.orderSpecial(NameEditDialog, {
                headline: t("renameItem"),
                nameLabel: t("itemName"),
                oldName: oldItemName,
                placeholder: oldItemName,
                showOverwrite: true,
                overwriteLabel: t("allowOverwriteSameName"),
                overwriteDisabled: (itemName: string) => resolveRenameConflict(itemName).openedConflict,
                overwriteDisabledReason: (itemName: string) =>
                    resolveRenameConflict(itemName).openedConflict ? t("itemOverwriteBlockedByOpenedTarget") : undefined,
                nameValidator: (itemName: string, allowOverwrite: boolean) => {
                    const conflict = resolveRenameConflict(itemName)
                    if (conflict.openedConflict) return t("itemOverwriteBlockedByOpenedTarget")
                    if (conflict.conflict && !allowOverwrite) return t("itemRenameConflictHint")
                    return undefined
                },
                confirm: (itemName: string, allowOverwrite?: boolean) => {
                    console.log(TAG, volumeId, "Rename", oldItemName, "To", itemName)
                    emitWebEvent(makeWebLetter('RenameItem', volumeId, oldItemName, itemName, !!allowOverwrite))
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

        async function mentionItem(volumeId: string, itemId: number) {
            console.log(TAG, "提及项目", volumeId, itemId)

            let itemKey = openedItems.value.find(item =>
                item.id === itemId && item.fromVolumeId === volumeId)
            ?.itemKey

            if (itemKey) {
                const tabIndex = openedTabs.value.findIndex(tab => tab.data === itemKey)

                if (tabIndex !== -1) {
                    activeTabIndex.value = tabIndex
                    return
                }
            } else {
                const fileModel = await getFullFileModel(volumeId, itemId)

                if (ensure(fileModel)) {
                    ensureItemKey(fileModel!)
                    openedItems.value.push(fileModel!)
                    itemKey = fileModel!.itemKey
                }
            }

            if (itemKey) openTab(TabType.Item, itemKey)
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
            const resolveAddConflict = (itemName: string) => {
                const volume = openedVolumes.value.find(v => v.id === volumeId)
                const backendExists = volume?.items?.find(item => item.name === itemName)
                const localDraftExists = openedItems.value.some(item =>
                    item.fromVolumeId === volumeId && item.id === -1 && item.name === itemName
                )
                const openedConflict = openedItems.value.some(item =>
                    item.fromVolumeId === volumeId &&
                    item.name === itemName &&
                    item.itemKey !== undefined &&
                    openedTabs.value.some(tab => tab.data === item.itemKey)
                )

                return {
                    volume,
                    backendExists,
                    conflict: !!backendExists || localDraftExists,
                    overwriteBlocked: localDraftExists || openedConflict,
                }
            }

            dialogState.orderSpecial(AddItemDialog, {
                showOverwrite: true,
                overwriteLabel: t("allowOverwriteSameName"),
                overwriteDisabled: (itemName: string) => resolveAddConflict(itemName).overwriteBlocked,
                overwriteDisabledReason: (itemName: string) =>
                    resolveAddConflict(itemName).overwriteBlocked ? t("itemOverwriteBlockedByOpenedTarget") : undefined,
                nameValidator: (itemName: string, allowOverwrite: boolean) => {
                    const conflict = resolveAddConflict(itemName)
                    if (conflict.overwriteBlocked) return t("itemOverwriteBlockedByOpenedTarget")
                    if (conflict.conflict && !allowOverwrite) return t("itemRenameConflictHint")
                    return undefined
                },
                confirm: (itemName: string, ryoType: RyoType, allowOverwrite?: boolean) => {
                    console.log(volumeId, itemName, ryoType)
                    const conflict = resolveAddConflict(itemName)
                    if (conflict.overwriteBlocked) return
                    if (conflict.conflict && !allowOverwrite) return

                    const fileModel: FileModel = {
                        data: appState.getInitValue(ryoType),
                        fromVolumeId: volumeId,
                        fromFile: conflict.volume?.name,
                        id: conflict.backendExists && allowOverwrite ? conflict.backendExists.id : -1,
                        name: itemName,
                        parseSuccess: true,
                        volumeRevision: getVolumeRevision(volumeId),
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
                        if (item.id === -1 || item.volumeRevision === undefined) item.volumeRevision = volume.revision
                    })

                    if (staleItemKeys.length > 0) {
                        console.warn(TAG, "移除失效或无法定位归属的Item数量", staleItemKeys.length)
                        removeOpenedItemsByKeys(staleItemKeys)
                    }
                })
                console.log(TAG, "OpenedVolumesChanged监听器已创建")

                addWebEventListener("VolumeItemRenamed", (args: string[]) => {
                    const [volumeId, oldName, newName] = args
                    console.log(TAG, volumeId, "接收重命名", oldName, "->", newName)

                    const item = openedItems.value.find(i => i.fromVolumeId === volumeId && i.name === oldName)
                    if (item) {
                        item.name = newName
                        const thePage = openedTabs.value.find(tab => tab.data === item.itemKey)
                        if (thePage) thePage.name = newName
                    }
                })
                console.log(TAG, "ItemRenamed监听器已创建")

                addWebEventListener("VolumeItemDeleted", (args: any[]) => {
                    const [volumeId, id] = args as [string, number]
                    console.log(TAG, volumeId, "接收删除", id)
                    const index = openedItems.value.findIndex(i => i.fromVolumeId === volumeId && i.id === id)
                    if (index !== -1) openedItems.value[index].id = -1 // 标记为未写入后端，对了，可能要重置unsaved、dirty啥的状态
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
            renameVolume,
            cloneVolume,
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
            explorerLocateTarget,
        }
    }
)
