import type {WebLetter, WebResponse} from "@/models/Models"
import {WebResponseState} from "@/models/Models";

declare const chrome: any
const TAG = "WinWebAppUtils"
const webEventListeners = new Map<string, Function>()

export function emitWebEvent(webEvent: WebLetter) {
    chrome.webview.postMessage(webEvent)
}

export function addWebEventListener(name: string, lambda: Function) {
    console.log(TAG, `添加${name}WebEvent监听器`)
    if (webEventListeners.has(name)) throw new Error(`已存在${name}WebEvent监听器`)
    webEventListeners.set(name, lambda)
}

export function removeWebEventListener(name: string) {
    console.log(TAG, `移除${name}WebEvent监听器`)
    webEventListeners.delete(name)
}

export function emitWebEventByApi(event: WebLetter) {
    chrome.webview.hostObjects.webApis["EmitWebEvent"](JSON.stringify(event))
}

export async function sendWebCall(call: WebLetter) {
    return JSON.parse(await chrome.webview.hostObjects.webApis["SendWebCall"](JSON.stringify(call))) as WebResponse
}

export async function sendWebCallAndTakeItsReturnValues(call: WebLetter) {
    const response = await sendWebCall(call)
    if (response.state == WebResponseState.Success) return response.returnValues
    throw new Error(response.returnValues[0])
}

export async function emitWebEventAndWaitForItsCallBack(webEvent: WebLetter, timeout = 5000) {
    const callBackName = `CallBack${webEvent.name}`
    return new Promise((resolve, reject) => {
        let fulfilled = false

        const lambda = (e: any[]) => {
            if (fulfilled) return
            fulfilled = true

            try {
                // console.log(TAG, `解决${callBackName}`)
                resolve(e)
            } catch (err) {
                // console.log(TAG, `解决${callBackName}承诺异常`, err)
                reject(err)
            } finally {
                // console.log(TAG, `正常移除${callBackName}监听器`)
                removeWebEventListener(callBackName)
            }
        }
        addWebEventListener(callBackName, lambda)

        setTimeout(() => {
            if (!fulfilled) {
                fulfilled = true // 设置 Promise 为已完成
                // console.log(TAG, `超时移除${callBackName}监听器`)
                chrome.webview.removeEventListener(callBackName, lambda)
                reject(new Error("等待返回事件时超时"))
            }
        }, timeout)

        emitWebEvent(webEvent)
    })
}

export function makeWebLetter(name: string, ...args: any[]): WebLetter {
    return {name, args}
}

// Init
try {
    console.log(TAG, "Start init")
    if (chrome === undefined || chrome.webview === undefined) throw new Error("Non Webview2 Environment")
    chrome.webview.addEventListener('message', (arg: any) => {
        try {
            const event = arg.data as WebLetter
            if (event !== null) {
                console.log(TAG, `事件名称：${event.name} 参数数：${event.args.length}`)
                webEventListeners.forEach((v, n) => {
                    if (n === event.name) {
                        console.log(TAG, `${event.name}真是监监又听听`)
                        v.call(null, event.args)
                    }
                })
            } else throw new Error(`返回的事件为Null`)
        } catch (err) {
            console.log(TAG, "处理WebEvent异常", err)
        }
    })
} catch (err) {
    console.error(TAG, "遇到问题，App初始化终止，将报错", err);
} finally {
    console.log(TAG, "Init over")
}