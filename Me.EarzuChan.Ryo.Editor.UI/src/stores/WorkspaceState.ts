import {defineStore} from "pinia"
import {computed, markRaw, ref} from "vue"
import ItemPage from "@/views/ContentPages/ItemPage.vue"
import WelcomePage from "@/views/ContentPages/WelcomePage.vue"
import {addWebEventListener, emitWebEvent, makeWebLetter} from "@/utils/KurisuUtils"
import {useDialogStateStore} from "@/stores/DialogState"
import type {ItemModel, MassFile} from "@/models/AppModels"
import type {TabModel} from "@/models/UIModels"

const TAG = "WorkspaceState"

export const useWorkspaceStateStore = defineStore('workspace-state', () => {
    const available = ref(false)

    const openedFiles = ref<MassFile[]>([{
        name: "假文件1",
        items: [{id: 1, name: "假项目1"}, {id: 2, name: "假项目2"}]
    }, {name: "假文件2", items: [{id: 1, name: "假项目1"}, {id: 2, name: "假项目2"}]}])
    const openedTabs = ref<TabModel[]>(
        [
            {
                name: "项目页",
                page: markRaw(ItemPage),
                unsaved: true,
                data: {
                    id: 1919810, parseSuccess: true,
                    type: {
                        baseType: {
                            type: "sengine.graphics2d.FontSprites",
                            members: [
                                {"name": "iArr", "type": "java.lang.Integer[]"},
                                {"name": "bArr", "type": "java.lang.Byte[][]"},
                                {"name": "f", "type": "java.lang.Float"},
                                {"name": "i", "type": "game23.model.DialogueTreeModel$UserMessageModel"}]
                        }, isArray: false, typeName: "sengine.graphics2d.FontSprites"
                    },
                    data: {
                        iArr: [1, 2, 3], bArr: [[1, 2], [3, 4]], f: 1.9,
                        i: {isHidden: false, message: "Hello, World!"},
                    }
                } as ItemModel
            },
            {name: "欢迎页", page: markRaw(WelcomePage), nonResident: true},
        ])
    const activeTabIndex = ref(0)
    const activeTab = computed(() => openedTabs.value[activeTabIndex.value])
    const dialogState = useDialogStateStore()

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

    function closeTab(index: number) {
        const tab = openedTabs.value[index]

        activeTabIndex.value = index

        if (tab.unsaved) {
            // 相应询问等操作
            let close = true

            dialogState.dialog({
                headline: "是否要保存对 " + tab.name + " 的更改？",
                description: "如果不保存，你的更改将丢失。",
                actions: [
                    {
                        text: "保存", onClick() {
                            console.log(TAG, "保存")
                        }
                    },
                    {
                        text: "不保存", onClick() {
                            console.log(TAG, "不保存")
                        }
                    },
                    {
                        text: "取消", onClick() {
                            console.log(TAG, "取消")
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

    // Async Init
    (async () => {
        try {
            console.log(TAG, "Start init")

            addWebEventListener("OpenedFilesChanged", (args: MassFile[]) => {
                console.log(TAG, "OpenedFilesChanged", args)
                openedFiles.value = args
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
        anchorTab,
        clickTab,
        closeTab,
        openedTabs,
        activeTabIndex,
        activeTab,
    }
})