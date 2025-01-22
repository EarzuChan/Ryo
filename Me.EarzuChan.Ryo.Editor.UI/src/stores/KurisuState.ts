import {defineStore} from "pinia"
import {ref} from "vue"
import {KurisuWindowState} from "@/models/KurisuModels"
import {addWebEventListener, emitWebEvent, makeWebLetter, sendWebCallAndTakeItsReturnValues} from "@/utils/KurisuUtils"

const TAG = "KurisuState"

export const useKurisuStateStore = defineStore('kurisu-state', () => {
    const available = ref(false)

    const isAppWindowMaximized = ref<boolean>(false)

    function setAppWindowState(state: KurisuWindowState) {
        emitWebEvent(makeWebLetter("AppProperty:WindowState", state))
    }

    function stopApp() {
        emitWebEvent(makeWebLetter("AppCommand:StopApp"))
    }

    function argsToIsAppWindowMaximized(args: number[]): boolean {
        return args[0] as KurisuWindowState === KurisuWindowState.Maximized
    }

    (async () => {
        try {
            console.log(TAG, "Start init")

            addWebEventListener("AppEvent:AppWindowStateChanged", (args: number[]) => {
                const maximized = argsToIsAppWindowMaximized(args)
                console.log(TAG, "AppMaximizationChanged", maximized)
                isAppWindowMaximized.value = maximized
            })
            console.log(TAG, "AppWindowStateChanged监听器已创建")

            isAppWindowMaximized.value = argsToIsAppWindowMaximized(await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppProperty:WindowState")))
            console.log(TAG, "已拉取初始AppWindowState")

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