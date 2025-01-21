import {defineStore} from "pinia";
import {useAppStateStore} from "@/stores/AppState"
import {ref, watch} from "vue"
import type {RyoType} from "@/models/AppModels"

const TAG = "TestingState"

export const useTestingStateStore = defineStore("testing-state", () => {
    const appState = useAppStateStore()

    const testingType = ref<RyoType>()

    const wantedTestingType = ref("me.earzuchan.weakpipe.HugeOuter")

    const testingData = ref<any>(null)

    watch(wantedTestingType, (newValue) => {
        try {
            if (newValue.length === 0) throw new Error("类名为空")

            let ryoType = appState.getRyoTypeByName(newValue)

            testingType.value = ryoType

            reinitData()
        } catch (e: any) {
            console.log(TAG, "不能设置真测试用类型", e)
        }
    }, {immediate: true})

    function reinitData() {
        testingData.value = appState.getInitValue(testingType.value!)
    }

    return {
        testingType,
        wantedTestingType,
        testingData,
        reinitData,
    }
})