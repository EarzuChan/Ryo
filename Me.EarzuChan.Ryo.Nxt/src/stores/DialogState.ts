import {defineStore} from "pinia"
import {createApp, h, ref} from "vue";
import type {DialogModel} from "@/models/Models";
import CommonDialog from "@/views/Dialogs/CommonDialog.vue";

const TAG = "DialogState"
export const useDialogStateStore = defineStore("dialog-state", () => {
    // 创建一个队列来存储对话框配置
    const dialogQueue = ref<DialogModel[]>([])
    let isShowingDialog = ref(false) // 标记是否有对话框正在显示

    // 修改原始的 dialog 函数
    function dialog(config: DialogModel) {
        if (isShowingDialog.value) {
            // 如果有对话框正在显示，将新对话框配置添加到队列的开头
            dialogQueue.value.unshift(config)
        } else {
            // 如果没有对话框正在显示，立即显示新对话框，并标记为打开状态
            isShowingDialog.value = true
            showDialog(config)
        }
    }

// 定义一个函数，用于显示下一个对话框
    function showNextDialog() {
        if (dialogQueue.value.length > 0) {
            const config = dialogQueue.value.shift()! // 从队列中取出最后一个对话框配置
            showDialog(config)
        } else {
            isShowingDialog.value = false // 没有更多对话框时，标记为关闭状态
        }
    }

// 定义一个函数，用于显示对话框
    function showDialog(config: DialogModel) {
        const div = document.createElement('div')
        document.body.appendChild(div)

        const app = createApp({
            render() {
                return h(CommonDialog, {
                    ...config,
                    onClose: () => {
                        if (config.onClose) {
                            config.onClose()
                        }
                    },
                    onClosed: () => {
                        app.unmount()
                        document.body.removeChild(div)

                        showNextDialog()

                        if (config.onClosed) {
                            config.onClosed()
                        }
                    }
                })
            }
        })

        app.mount(div)
    }

    return {dialog, isShowingDialog};
});