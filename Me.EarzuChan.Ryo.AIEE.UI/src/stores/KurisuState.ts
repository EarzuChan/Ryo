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
        supportsWindowControls: false,
        supportsWindowStateRead: false,
        supportsWindowStateWrite: false,
        supportsOpenFileDialog: false,
        supportsSaveFileDialog: false,
    })

    function setAppWindowState(state: KurisuWindowState) {
        if (!hostCapabilities.value.supportsWindowStateWrite) return
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
        
        if (!first || typeof first !== "object") return {
            supportsWindowControls: false,
            supportsWindowStateRead: false,
            supportsWindowStateWrite: false,
            supportsOpenFileDialog: false,
            supportsSaveFileDialog: false,
        }

        const raw = first as {
            supportsWindowControls?: unknown
            supportsWindowStateRead?: unknown
            supportsWindowStateWrite?: unknown
            supportsOpenFileDialog?: unknown
            supportsSaveFileDialog?: unknown
        }
        
        return {
            supportsWindowControls: raw.supportsWindowControls === true,
            supportsWindowStateRead: raw.supportsWindowStateRead === true,
            supportsWindowStateWrite: raw.supportsWindowStateWrite === true,
            supportsOpenFileDialog: raw.supportsOpenFileDialog === true,
            supportsSaveFileDialog: raw.supportsSaveFileDialog === true,
        }
    }

    ;(async () => {
        try {
            console.log(TAG, "Start init")

            addWebEventListener("AppEvent:AppWindowStateChanged", (args: unknown[]) => {
                if (!hostCapabilities.value.supportsWindowStateRead) return
                const maximized = argsToIsAppWindowMaximized(args)
                console.log(TAG, "AppMaximizationChanged", maximized)
                isAppWindowMaximized.value = maximized
            })
            console.log(TAG, "AppWindowStateChanged监听器已创建")

            hostCapabilities.value = returnValuesToHostCapabilities(await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppProperty:HostCapabilities")))
            console.log(TAG, "已拉取HostCapabilities", hostCapabilities.value)

            if (hostCapabilities.value.supportsWindowStateRead) {
                isAppWindowMaximized.value = argsToIsAppWindowMaximized(await sendWebCallAndTakeItsReturnValues(makeWebLetter("AppProperty:WindowState")))
                console.log(TAG, "已拉取初始AppWindowState")
            } else {
                isAppWindowMaximized.value = true
                console.log(TAG, "当前宿主不支持窗口状态读取，默认视作Maximized")
            }

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
