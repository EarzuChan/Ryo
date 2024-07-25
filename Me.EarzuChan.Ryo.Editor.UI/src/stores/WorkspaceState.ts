import {defineStore} from "pinia"
import {computed, markRaw, ref} from "vue"
import ItemPage from "@/views/ContentPages/ItemPage.vue"
import WelcomePage from "@/views/ContentPages/WelcomePage.vue"
import {addWebEventListener, emitWebEvent, makeWebLetter} from "@/utils/KurisuUtils"
import {useDialogStateStore} from "@/stores/DialogState"
import {type ItemModel, type MassFile, TabType} from "@/models/AppModels"
import type {TabModel} from "@/models/AppModels"
import {useAppStateStore} from "@/stores/AppState"
import {TODO} from "@/utils/UsefulUtils"

const TAG = "WorkspaceState"

export const useWorkspaceStateStore = defineStore('workspace-state', () => {
    const available = ref(false)

    const appState = useAppStateStore()

    const activeTabPage = ref<any>(null)

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
    const activeTabIndex = ref(0)
    const activeTab = computed(() => openedTabs.value[activeTabIndex.value])

    const openedFiles = ref<MassFile[]>([/*{
        name: "假文件1",
        items: [{id: 1, name: "假项目1"}, {id: 2, name: "假项目2"}]
    }, {name: "假文件2", items: [{id: 1, name: "假项目1"}, {id: 2, name: "假项目2"}]}*/])
    const openedItems = ref<ItemModel[]>([
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

    function openFile() {
        emitWebEvent(makeWebLetter('OpenFile'))
    }

    function newFile() {
        emitWebEvent(makeWebLetter('NewFile'))
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

    function setActiveTabPage(page: any) {
        activeTabPage.value = page
        console.debug(TAG, "已设置当前Tab", page)
    }

    function openTab(tabType: TabType, index: number = -1) {
        console.debug(TAG, "打开Tab", tabType, index)
        switch (tabType) {
            case TabType.Empty:
                internalOpenTab({name: "空白页", nonResident: true})
                break
            case TabType.Item:
                let name = openedItems.value[index]?.name
                if (name === undefined) name = "无名项目"
                internalOpenTab({name, page: markRaw(ItemPage), data: index, nonResident: true})
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
        activeTabPage.value?.discard()
    }

    function pageReload(fromSystem: boolean = false) {
        activeTabPage.value?.reload(fromSystem)
    }

    function pageSave() {
        activeTabPage.value?.save()
    }

    function pageRedo() {
        activeTabPage.value?.redo()
    }

    function pageUndo() {
        activeTabPage.value?.undo()
    }

// Async Init
    (async () => {
        try {
            console.log(TAG, "Start init")

            addWebEventListener("OpenedFilesChanged", (args: MassFile[][]) => {
                console.log(TAG, "OpenedFilesChanged", args[0])
                openedFiles.value = args[0]
            })
            console.log(TAG, "OpenedFilesChanged监听器已创建")
            emitWebEvent(makeWebLetter('NotifyOpenedFiles'))
            console.log(TAG, "已提醒发送OpenedFiles")

            available.value = true
        } catch (err) {
            console.error(TAG, "Init error", err)
        } finally {
            console.log(TAG, "Init over")
        }
    })()

    return {
        available,
        openedFiles,
        openedItems,
        anchorTab,
        clickTab,
        closeTab,
        openedTabs,
        activeTabIndex,
        activeTab,
        activeTabPage,
        setActiveTabPage,
        pageDiscard,
        pageReload,
        pageRedo,
        pageUndo,
        pageSave,
        getIsTabUnsaved,
        openTab,
        openFile,
        newFile
    }
})