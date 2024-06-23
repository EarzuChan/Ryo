import {createApp, h} from "vue"
import type {DialogModel} from "@/models/Models"
import CommonDialog from "@/views/Dialogs/CommonDialog.vue"

const TAG = "UsefulUtils"

export function sleepFor(delay: number) {
    return new Promise(resolve => setTimeout(resolve, delay))
}

export async function copyTextToClipboard(text: string) {
    try {
        await navigator.clipboard.writeText(text)
        console.log(TAG, 'Text copied to clipboard')
    } catch (err) {
        console.error(TAG, 'Failed to copy text: ', err)
    }
}

export function getUpToNLines(str: string, n: number, appendDots: boolean = true) {
    // 使用换行符分割字符串为数组
    const lines = str.split('\n')

    // 如果数组长度超过n，只保留前n个元素
    if (lines.length > n) {
        const firstFiveLines = lines.slice(0, n).join('\n')
        return appendDots ? firstFiveLines + " ......" : firstFiveLines
    }

    // 如果不足n行，返回原字符串
    return str
}

export function isEqual(a: number[], b: number[]): boolean {
    return a.length === b.length && a.every((val, index) => val === b[index])
}

export function boolToText(value?: boolean, yes: string = "成功", no: string = "失败"): string {
    return value === true ? yes : no
}

export function arrayToText(arr: any[], empty: string = "数组为空"): string {
    if (arr.length === 0) return empty
    return arr.join(", ")
}

export function getSfcName(et: any): string {
    const fileName: string = et.__file! as string
    return fileName.substring(fileName.lastIndexOf("/") + 1, fileName.lastIndexOf("."))
}

export function delayExecution(delay: number, lambda: TimerHandler) {
    let timeoutId = setTimeout(lambda, delay)
    return {
        cancel: function () {
            clearTimeout(timeoutId)
        }
    }
}

export function isScrollbarVisible(element: HTMLElement): boolean {
    return element.scrollHeight > element.clientHeight
}

export function testArray(text: string, times: number): string[] {
    let result = []
    for (let i = 0; i < times; i++) result.push(`${text}-${i}`)
    return result
}