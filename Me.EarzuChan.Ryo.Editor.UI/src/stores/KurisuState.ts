import {defineStore} from "pinia";
import {ref} from "vue";
import {KurisuWindowState} from "@/models/KurisuModels";
import {addWebEventListener, emitWebEvent, makeWebLetter} from "@/utils/KurisuUtils"

const TAG = "KurisuState"

export const useKurisuStateStore = defineStore('kurisu-state', () => {
    const available = ref(false)

    const isAppWindowMaximized = ref<boolean>(false)

    function setAppWindowState(state: KurisuWindowState) {
        emitWebEvent(makeWebLetter("SetAppWindowState", state))
    }

    function stopApp() {
        emitWebEvent(makeWebLetter("StopApp"))
    }

    (async () => {
        try {
            console.log(TAG, "Start init")

            addWebEventListener("AppWindowStateChanged", (args: number[]) => {
                const state = args[0] as KurisuWindowState
                console.log(TAG, "AppMaximizationChanged", state, state === KurisuWindowState.Maximized)
                isAppWindowMaximized.value = state === KurisuWindowState.Maximized
            })
            console.log(TAG, "AppWindowStateChanged监听器已创建")
            emitWebEvent(makeWebLetter('NotifyAppWindowState'))
            console.log(TAG, "已提醒发送AppWindowState")

            available.value = true
        } catch (err) {
            console.error(TAG, "Init failed", err)
        } finally {
            console.log(TAG, "Init over")
        }
    })()

    return {
        available,
        isAppWindowMaximized,
        setAppWindowState,
        stopApp,
    }
})