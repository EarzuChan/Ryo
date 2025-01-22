import {emitWebEvent, makeWebLetter} from "@/utils/KurisuUtils";

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
    // 貌似也是为了处理Proxy数组不等于原数组而被迫处理的
    return a.length === b.length && a.every((val, index) => val === b[index])
}

export function boolToText(value?: boolean, yes: string = 'YES', no: string = 'NO'): string {
    return value === true ? yes : no
}

export function arrayToText(arr: any[], empty: string = 'EMPTY ARRAY'): string {
    if (arr.length === 0) return empty
    return arr.join(", ")
}

export function getSfcName(et: any): string {
    if (ensure(et.__name)) return et.__name
    else {
        const fileName = et.__file
        return fileName.substring(fileName.lastIndexOf("/") + 1, fileName.lastIndexOf("."))
    }
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

export function makeTestArray(text: string, times: number): string[] {
    let result = []
    for (let i = 0; i < times; i++) result.push(`${text}-${i}`)
    return result
}

export function ensure(obj?: any): boolean {
    // console.log(TAG, "确保", obj)
    return obj !== null && obj !== undefined;
}

export function ensureObject(obj?: any): boolean {
    return ensure(obj) && obj instanceof Object
}

export function generateId(seed: number): number {
    seed++
    return Math.floor(Math.random() * seed * 1000 + seed)
}

export function deepCopy(obj: any): any {
    return JSON.parse(JSON.stringify(obj))
}

export function openLink(link: string) {
    emitWebEvent(makeWebLetter('OpenLink', link))
}

export function TODO(...arg: any[]) {
    console.warn("TODO", ...arg)

    // 把参数拼接成字符串
    return "TODO " + arg.slice(1).join(" ")
}