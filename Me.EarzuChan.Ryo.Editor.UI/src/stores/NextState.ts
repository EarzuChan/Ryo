import {defineStore} from "pinia"
import {sleepFor} from "@/utils/UsefulUtils"
import {ref} from "vue"

const TAG = "NextState"

export const useNextStateStore = defineStore('next-state', async () => {
    console.log(TAG, "Start init")
    await sleepFor(2000)
    const manba = ref("out")
    console.log(TAG, "Init over")
    return {
        manba
    }
})