import {defineStore} from "pinia";
import {computed, markRaw, ref} from "vue";
import type {ItemModel, MassFile, TabModel} from "@/models/Models";
import ItemPage from "@/views/ContentPages/ItemPage.vue";
import WelcomePage from "@/views/ContentPages/WelcomePage.vue"
import {addWebEventListener, emitWebEvent, makeWebLetter} from "@/utils/WinWebAppUtils"
import {useDialogStateStore} from "@/stores/DialogState";

const TAG = "OpenedFilesState"

export const useOpenedFilesStateStore = defineStore('opened-files-state', () => {

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
                                {"name": "i", "type": "java.lang.Integer"}]
                        }, isArray: false, typeName: "sengine.graphics2d.FontSprites"
                    },
                    data: {iArr: [1, 2, 3], bArr: [[1, 2], [3, 4]], f: 1.9, i: 191810}
                } as ItemModel
            },
            {name: "欢迎页", page: markRaw(WelcomePage)},
            {name: "空页", nonResident: true},
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
        } catch (err) {
            console.error(TAG, "遇到问题，App初始化终止，将报错", err);
        } finally {
            console.log(TAG, "Init over")
        }
    })()

    return {
        openedFiles,
        anchorTab,
        clickTab,
        closeTab,
        openedTabs,
        activeTabIndex,
        activeTab,
    }
})