import {defineStore} from "pinia"
import {computed, markRaw, ref} from "vue"
import ItemPage from "@/views/ContentPages/ItemPage.vue"
import WelcomePage from "@/views/ContentPages/WelcomePage.vue"
import {
    addWebEventListener,
    emitWebEvent,
    makeWebLetter,
    sendWebCall,
    sendWebCallAndTakeItsReturnValues
} from "@/utils/KurisuUtils"
import {useDialogStateStore} from "@/stores/DialogState"
import {type FileModel, type VolumeModel, TabType} from "@/models/AppModels"
import type {TabModel} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import {ensure, TODO} from "@/utils/UsefulUtils"

const TAG = "WorkspaceState"

export const useWorkspaceStateStore = defineStore('workspace-state', () => {
        const available = ref(false)

        const appState = useAppStateStore()

        const activeTabExposed = ref<any>(null)

        const openedTabs = ref<TabModel[]>([
            /*{
                name: "项目页1",
                page: markRaw(ItemPage),
                data: 0
            },
            {
                name: "项目页2",
                page: markRaw(ItemPage),
                data: 1
            },
            {name: "欢迎页", page: markRaw(WelcomePage), nonResident: true},*/
        ])
        const activeTabIndex = ref(-1)
        const activeTab = computed(() => openedTabs.value[activeTabIndex.value])
        const activeVolume = computed(() => activeItem.value?.fromFile)
        const activeItem = computed(() => openedItems.value[activeTab.value?.data])

        const openedVolumes = ref<VolumeModel[]>([/*{
        name: "假文件1",
        items: [{id: 1, name: "假项目1"}, {id: 2, name: "假项目2"}]
    }, {name: "假文件2", items: [{id: 1, name: "假项目1"}, {id: 2, name: "假项目2"}]}*/])
        const openedItems = ref<FileModel[]>([
            /*{
                id: 1919810, parseSuccess: true,
                type: appState.getRyoTypeByName("sengine.graphics2d.FontSprites[]"),
                data: [{
                    iArr: [1, 9, 1, 9], bArr: [[1, 2], [3, 4]], f: 1.9,
                    i: 810,
                }]
            },
            {
                id: 1919810, parseSuccess: true,
                type: {
                    baseType: {
                        type: "java.lang.String",
                    }, isArray: true, typeName: "java.lang.String"
                },
                data: ["man"]
            }*/])

        const dialogState = useDialogStateStore()

        function openVolume() {
            emitWebEvent(makeWebLetter('OpenVolume'))
        }

        function newVolume() {
            emitWebEvent(makeWebLetter('NewVolume'))
        }

        function clickTab(index: number) {
            activeTabIndex.value = index
            // 相应操作
        }

        function anchorTab(index: number) {
            const tab = openedTabs.value[index]
            if (tab.nonResident) {
                tab.nonResident = false
            }
        }

        function getIsTabUnsaved(index: number) {
            const man = openedTabs.value[index].data
            if (typeof man === 'number') {
                return openedItems.value[man].unsaved === true
            } else return false
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

        function internalCloseTab(index: number) {
            openedTabs.value.splice(index, 1)
            if (activeTabIndex.value === index) {
                activeTabIndex.value = openedTabs.value.length !== 0 ? 0 : -1
            } else if (activeTabIndex.value > index) {
                activeTabIndex.value--
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
                    internalOpenTab({name: "空白页", nonResident: true})
                    break
                case TabType.Item:
                    let name = openedItems.value[data]?.name
                    if (name === undefined) name = "无名项目"
                    internalOpenTab({name, page: markRaw(ItemPage), data, nonResident: true})
                    // TODO: 项目一旦unsaved，就常驻
                    break
                case TabType.Welcome:
                    internalOpenTab({name: "欢迎", page: markRaw(WelcomePage), nonResident: true})
            }
        }

        function internalOpenTab(tab: TabModel) {
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

        async function mentionItem(massName: string, itemId: number) {
            console.log(TAG, "提及项目", massName, itemId)

            let mamba = openedItems.value.findIndex(item =>
                item.id === itemId && item.fromFile === massName)

            if (mamba !== -1) {
                const tabIndex = openedTabs.value.findIndex(tab => tab.data === mamba)

                if (tabIndex !== -1) {
                    activeTabIndex.value = tabIndex
                    return
                }
            } else {
                const fileModel = await getFullFileModel(massName, itemId)

                if (ensure(fileModel)) {
                    mamba = openedItems.value.push(fileModel!) - 1
                }
            }

            openTab(TabType.Item, mamba)
        }

        async function getFullFileModel(massName: string, itemId: number) {
            console.log(TAG, "获取项目", massName, itemId)
            const fileModel = (await sendWebCallAndTakeItsReturnValues(makeWebLetter('GetFullFileModel', massName, itemId)))[0] as FileModel
            fileModel.ryoType = appState.getRyoTypeByName(fileModel.type!)
            console.log(TAG, "获取到项目", massName, itemId, fileModel)

            return fileModel
        }

        function saveVolume(massName: string) {
            emitWebEvent(makeWebLetter('SaveVolume', massName, false))
        }

        function saveVolumeAs(massName: string) {
            emitWebEvent(makeWebLetter('SaveVolume', massName, true))
        }

        async function saveItem(massName: string, itemName: string, data: any) {
            return (await sendWebCallAndTakeItsReturnValues(makeWebLetter('SaveItem', massName, itemName, data)))[0]
        }

        function closeVolume(massName: string) {
            emitWebEvent(makeWebLetter('CloseVolume', massName))
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
            closeVolume,
            getIsTabUnsaved,
            mentionItem,
            newVolume,
            openTab,
            openVolume,
            openedItems,
            openedTabs,
            openedVolumes,
            pageDiscard,
            pageRedo,
            pageReload,
            pageSave,
            pageUndo,
            saveVolume,
            saveVolumeAs,
            saveItem,
            setActiveTabExposed,
        }
    }
)