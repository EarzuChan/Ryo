import {defineStore} from "pinia"
import {ref} from "vue"
import type {KurisuHostCapabilities} from "@/models/KurisuModels"
import {KurisuWindowState} from "@/models/KurisuModels"
import {addWebEventListener, emitWebEvent, makeWebLetter, sendWebCallAndTakeItsReturnValues} from "@/utils/KurisuUtils"

const TAG = "KurisuState"

export const useKurisuStateStore = defineStore("kurisu-state", () => {
    const available = ref(false)

    const isAppWindowMaximized = ref<boolean>(false)
    const hostCapabilities = ref<KurisuHostCapabilities>({
        supportsWindowControls: false
    })

    function setAppWindowState(state: KurisuWindowState) {
        emitWebEvent(makeWebLetter("AppProperty:WindowState", state))
    }

    function stopApp() {
        emitWebEvent(makeWebLetter("AppCommand:StopApp"))
    }

    function argsToIsAppWindowMaximized(args: unknown[]): boolean {
        return Number(args[0]) === KurisuWindowState.Maximized
    }

    function returnValuesToHostCapabilities(returnValues: unknown[]): KurisuHostCapabilities {
        const first = returnValues[0]
        if (!first || typeof first !== "object") return {supportsWindowControls: false}

        const supportsWindowControls = (first as { supportsWindowControls?: unknown }).supportsWindowControls
        return {supportsWindowControls: supportsWindowControls === true}
    }

    ;(async () => {
        try {
            console.log(TAG, "Start init")

            addWebEventListener("AppEvent:AppWindowStateChanged", (args: unknown[]) => {
                const maximized = argsToIsAppWindowMaximized(args)
                console.log(TAG, "AppMaximizationChanged", maximized)
                isAppWindowMaximized.value = maximized
            })
            console.log(TAG, "AppWindowStateChanged监听器已创建")

            isAppWindowMaximized.value = argsToIsAppWindowMaximized(await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppProperty:WindowState")))
            console.log(TAG, "已拉取初始AppWindowState")

            hostCapabilities.value = returnValuesToHostCapabilities(
                await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppProperty:HostCapabilities"))
            )
            console.log(TAG, "已拉取HostCapabilities", hostCapabilities.value)

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
        hostCapabilities,
        setAppWindowState,
        stopApp,
    }
})
