import {defineStore} from "pinia"
import {computed, markRaw, ref} from "vue"
import ItemPage from "@/views/pages/ItemPage.vue"
import WelcomePage from "@/views/pages/WelcomePage.vue"
import {
    addWebEventListener,
    emitWebEvent,
    makeWebLetter,
    sendWebCallAndTakeItsReturnValues
} from "@/utils/KurisuUtils"
import {useDialogStateStore} from "@/stores/DialogState"
import {type FileModel, type VolumeModel, TabType, type RyoType} from "@/models/AppModels"
import type {TabModel} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import {ensure, TODO} from "@/utils/UsefulUtils"
import AddItemDialog from "@/views/dialogs/AddItemDialog.vue"
import {useI18n} from "vue-i18n"
import RenameItemDialog from "@/views/dialogs/RenameItemDialog.vue";

const TAG = "WorkspaceState"

export const useWorkspaceStateStore = defineStore('workspace-state', () => {
        const available = ref(false)

        const appState = useAppStateStore()

        const {t} = useI18n()

        const activeTabExposed = ref<any>(null)

        const openedTabs = ref<TabModel[]>([])
        const activeTabIndex = ref(-1)
        const activeTab = computed(() => openedTabs.value[activeTabIndex.value])
        const activeVolume = computed(() => activeItem.value?.fromFile)
        const activeItem = computed(() => openedItems.value[activeTab.value?.data])

        const openedVolumes = ref<VolumeModel[]>([])

        const openedItems = ref<FileModel[]>([])

        const dialogState = useDialogStateStore()

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
            const man = openedTabs.value[index].data

            if (typeof man === 'number') return openedItems.value[man].unsaved === true
            else return false
        }

        function closeTab(index: number) {
            const tab = openedTabs.value[index]

            activeTabIndex.value = index

            if (getIsTabUnsaved(index)) {
                // 相应询问等操作
                let close = true

                dialogState.order({
                    headline: "是否要保存对 " + tab.name + " 的更改？",
                    description: "如果不保存，你的更改将丢失。",
                    actions: [
                        {
                            text: "保存", onClick() {
                                TODO(TAG, "保存")
                            }
                        },
                        {
                            text: "不保存", onClick() {
                                TODO(TAG, "不保存")
                            }
                        },
                        {
                            text: "取消", onClick() {
                                TODO(TAG, "取消")
                                close = false
                            }
                        }
                    ],
                    onClose() {
                        if (!close) return

                        internalCloseTab(index)
                    }
                })
            } else internalCloseTab(index)
        }

        // 可能要加入关闭ItemPage就关掉对应openedItem的逻辑
        function internalCloseTab(index: number) {
            openedTabs.value.splice(index, 1)

            if (activeTabIndex.value === index) activeTabIndex.value = openedTabs.value.length !== 0 ? 0 : -1
            else if (activeTabIndex.value > index) activeTabIndex.value--
        }

        function setActiveTabExposed(page: any) {
            activeTabExposed.value = page
            console.debug(TAG, "已设置当前Tab", page)
        }

        function openTab(tabType: TabType, data?: any) {
            console.debug(TAG, "打开Tab", tabType, data)
            switch (tabType) {
                case TabType.Empty:
                    internalOpenTab({name: t('emptyPage'), nonResident: true})
                    break

                case TabType.Item:
                    const fileModel = openedItems.value[data]

                    let name = fileModel.name
                    if (name === undefined) name = t('noNameItem')

                    internalOpenTab({name, page: markRaw(ItemPage), data, nonResident: !fileModel.unsaved})
                    break

                case TabType.Welcome:
                    internalOpenTab({name: t('welcome'), page: markRaw(WelcomePage), nonResident: true})
            }
        }

        function internalOpenTab(tab: TabModel) {
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

            let itsId = openedItems.value.findIndex(item =>
                item.id === itemId && item.fromFile === massName)

            if (itsId !== -1) {
                const tabIndex = openedTabs.value.findIndex(tab => tab.data === itsId)

                if (tabIndex !== -1) {
                    activeTabIndex.value = tabIndex
                    return
                }
            } else {
                const fileModel = await getFullFileModel(massName, itemId)

                if (ensure(fileModel)) itsId = openedItems.value.push(fileModel!) - 1
            }

            openTab(TabType.Item, itsId)
        }

        async function getFullFileModel(massName: string, itemId: number) {
            console.log(TAG, "获取项目", massName, itemId)
            const fileModel = (await sendWebCallAndTakeItsReturnValues(makeWebLetter('GetFullFileModel', massName, itemId)))[0] as FileModel
            fileModel.ryoType = appState.getRyoTypeByDataTypeName(fileModel.dataTypeName!)
            console.log(TAG, "获取到项目", massName, itemId, fileModel)

            return fileModel
        }

        function saveVolume(massName: string) {
            emitWebEvent(makeWebLetter('SaveVolume', massName, false))
        }

        function saveVolumeAs(massName: string) {
            emitWebEvent(makeWebLetter('SaveVolume', massName, true))
        }

        async function saveItem(massName: string, itemName: string, data: any, tsRyoTypeName: string) {
            return (await sendWebCallAndTakeItsReturnValues(makeWebLetter('SaveItem', massName, itemName, data, tsRyoTypeName)))[0]
        }

        function closeVolume(massName: string) {
            emitWebEvent(makeWebLetter('CloseVolume', massName))
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

                        // 如果后端没找到，再找本地。利用短路特性。
                        // 注意：这里必须显式用变量存 index，不要和 ItemModel 混用
                        let localIndex = -1
                        if (!backendExists) localIndex = openedItems.value.findIndex(item =>
                            item.fromFile === massName && item.name === itemName && item.id === -1
                        )

                        // 只要有任意一个存在
                        if (backendExists || localIndex !== -1) {
                            dialogState.order({
                                headline: `已存在"${itemName}"`,
                                description: "不可重复创建同名项目，是否跳转到已有项目？",
                                actions: [
                                    {text: "取消"},
                                    {
                                        text: "跳转", onClick() {
                                            if (backendExists) mentionItem(massName, backendExists.id!)
                                            else {
                                                // 肯定是 localIndex !== -1
                                                const tabIndex = openedTabs.value.findIndex(tab => tab.data === localIndex)
                                                if (tabIndex !== -1) activeTabIndex.value = tabIndex
                                                else openTab(TabType.Item, localIndex)
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
                        ryoType: ryoType,
                        dataTypeName: appState.getDataTypeNameByRyoType(ryoType),
                        unsaved: true
                    }

                    const itsId = openedItems.value.push(fileModel) - 1

                    openTab(TabType.Item, itsId)
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
                })
                console.log(TAG, "OpenedVolumesChanged监听器已创建")

                addWebEventListener("VolumeItemRenamed", (args: string[]) => {
                    const [massName, oldName, newName] = args
                    console.log(TAG, massName, "接收重命名", oldName, "->", newName)

                    const targetIdx = openedItems.value.findIndex(i => i.fromFile === massName && i.name === oldName)
                    if (targetIdx !== -1) openedItems.value[targetIdx].name = newName

                    const thePage = openedTabs.value.find(tab => tab.data === targetIdx)
                    if (thePage) thePage.name = newName
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
            renameItem,
            pageUndo,
            saveVolume,
            saveVolumeAs,
            saveItem,
            setActiveTabExposed,
            addItemInVolume,
        }
    }
)