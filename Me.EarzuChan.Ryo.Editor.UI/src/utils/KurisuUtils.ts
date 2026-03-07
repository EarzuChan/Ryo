import type {KurisuBridgeMessage, WebLetter, WebResponse} from "@/models/KurisuModels"
import {WebResponseState} from "@/models/KurisuModels"

const TAG = "KurisuUtils"
const DEFAULT_CALL_TIMEOUT = 10000
const BRIDGE_WS_ROUTE = "/__kurisu_bridge"

type WebEventListener = (args: any[]) => void
type PendingWebCall = {
    resolve: (response: WebResponse) => void
    reject: (error: Error) => void
    timeoutId: number
}
type BridgeMessageHandler = (message: KurisuBridgeMessage) => void
type BridgeTransport = {
    postMessage: (message: KurisuBridgeMessage) => void
    addListener: (handler: BridgeMessageHandler) => void
}
type WebViewBridgeHost = {
    postMessage: (message: unknown) => void
    addEventListener: (name: string, listener: (arg: any) => void) => void
}

const webEventListeners = new Map<string, Set<WebEventListener>>()
const pendingWebCalls = new Map<string, PendingWebCall>()
const outboundQueue: KurisuBridgeMessage[] = []

let requestCounter = 0
let transport: BridgeTransport | null = null
let transportReady = false

function nextRequestId(): string {
    if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") return crypto.randomUUID()

    requestCounter += 1
    return `${Date.now()}-${requestCounter}`
}

function postBridgeMessage(message: KurisuBridgeMessage) {
    if (!transport) throw new Error("Bridge transport 未初始化")

    if (!transportReady) {
        outboundQueue.push(message)
        return
    }

    transport.postMessage(message)
}

function flushOutboundQueue() {
    if (!transport || !transportReady || outboundQueue.length === 0) return

    while (outboundQueue.length > 0) {
        const message = outboundQueue.shift()
        if (!message) continue
        transport.postMessage(message)
    }
}

function resolvePendingWebCall(requestId: string, response: WebResponse) {
    const pending = pendingWebCalls.get(requestId)
    if (!pending) return

    pendingWebCalls.delete(requestId)
    clearTimeout(pending.timeoutId)
    pending.resolve(response)
}

function rejectPendingWebCall(requestId: string, error: Error) {
    const pending = pendingWebCalls.get(requestId)
    if (!pending) return

    pendingWebCalls.delete(requestId)
    clearTimeout(pending.timeoutId)
    pending.reject(error)
}

function rejectAllPendingWebCalls(error: Error) {
    pendingWebCalls.forEach((_, requestId) => {
        rejectPendingWebCall(requestId, error)
    })
}

function dispatchWebEvent(webEvent: WebLetter) {
    const args = webEvent.args ?? []
    console.log(TAG, `事件名称：${webEvent.name} 参数数：${args.length}`)
    const listeners = webEventListeners.get(webEvent.name)
    if (!listeners) return

    listeners.forEach(listener => {
        try {
            listener.call(null, args)
        } catch (error) {
            console.error(TAG, `WebEvent listener 执行失败: ${webEvent.name}`, error)
        }
    })
}

function handleIncomingBridgeMessage(message: KurisuBridgeMessage) {
    if (!message) throw new Error("无效Bridge消息")

    switch (message.kind) {
        case "webEvent":
            if (message.letter == null) throw new Error("webEvent消息缺少letter")
            dispatchWebEvent(message.letter)
            return

        case "webCallResponse":
            if (!message.requestId) throw new Error("webCallResponse消息缺少requestId")
            if (!message.response) throw new Error("webCallResponse消息缺少response")
            resolvePendingWebCall(message.requestId, message.response)
            return

        default:
            console.warn(TAG, "未知Bridge消息类型", message.kind)
    }
}

function getBridgeWebSocketUrl(): string {
    const searchParams = new URLSearchParams(window.location.search)
    const explicit = searchParams.get("kurisuBridgeWs")
    if (explicit) return explicit

    const protocol = window.location.protocol === "https:" ? "wss:" : "ws:"
    return `${protocol}//${window.location.host}${BRIDGE_WS_ROUTE}`
}

function getWebViewBridgeHost(): WebViewBridgeHost | null {
    const candidate = (globalThis as any)?.chrome?.webview
    if (!candidate) return null
    if (typeof candidate.postMessage !== "function") return null
    if (typeof candidate.addEventListener !== "function") return null
    return candidate as WebViewBridgeHost
}

function createWebViewTransport(host: WebViewBridgeHost): BridgeTransport {
    return {
        postMessage: (message: KurisuBridgeMessage) => {
            host.postMessage(message)
        },
        addListener: (handler: BridgeMessageHandler) => {
            host.addEventListener("message", (arg: any) => {
                handler(arg.data as KurisuBridgeMessage)
            })
        },
    }
}

function createWebSocketTransport(): BridgeTransport {
    const wsUrl = getBridgeWebSocketUrl()
    const ws = new WebSocket(wsUrl)
    const listeners = new Set<BridgeMessageHandler>()

    ws.addEventListener("open", () => {
        transportReady = true
        flushOutboundQueue()
        console.log(TAG, "WebSocket bridge connected", wsUrl)
    })

    ws.addEventListener("close", () => {
        transportReady = false
        rejectAllPendingWebCalls(new Error("Bridge连接已关闭"))
        console.warn(TAG, "WebSocket bridge closed")
    })

    ws.addEventListener("error", (error) => {
        console.error(TAG, "WebSocket bridge error", error)
    })

    ws.addEventListener("message", (event: MessageEvent<string>) => {
        try {
            const parsed = JSON.parse(event.data) as KurisuBridgeMessage
            listeners.forEach(listener => listener(parsed))
        } catch (error) {
            console.error(TAG, "解析WebSocket bridge消息失败", error)
        }
    })

    return {
        postMessage: (message: KurisuBridgeMessage) => {
            if (ws.readyState !== WebSocket.OPEN) {
                outboundQueue.push(message)
                return
            }

            ws.send(JSON.stringify(message))
        },
        addListener: (handler: BridgeMessageHandler) => {
            listeners.add(handler)
        },
    }
}

export function emitWebEvent(webEvent: WebLetter) {
    postBridgeMessage({
        kind: "webEvent",
        letter: webEvent,
    })
}

export function addWebEventListener(name: string, lambda: WebEventListener): () => void {
    console.log(TAG, `添加${name}WebEvent监听器`)
    if (!webEventListeners.has(name)) webEventListeners.set(name, new Set())
    webEventListeners.get(name)!.add(lambda)
    return () => removeWebEventListener(name, lambda)
}

export function removeWebEventListener(name: string, lambda?: WebEventListener) {
    console.log(TAG, `移除${name}WebEvent监听器`)
    if (!webEventListeners.has(name)) return

    if (!lambda) {
        webEventListeners.delete(name)
        return
    }

    const listeners = webEventListeners.get(name)!
    listeners.delete(lambda)
    if (listeners.size === 0) webEventListeners.delete(name)
}

export async function sendWebCall(call: WebLetter, timeout = DEFAULT_CALL_TIMEOUT): Promise<WebResponse> {
    const requestId = nextRequestId()
    const safeTimeout = Number.isFinite(timeout) && timeout > 0 ? timeout : DEFAULT_CALL_TIMEOUT

    return await new Promise<WebResponse>((resolve, reject) => {
        const timeoutId = window.setTimeout(() => {
            rejectPendingWebCall(requestId, new Error(`WebCall超时：${call.name}`))
        }, safeTimeout)

        pendingWebCalls.set(requestId, {
            resolve,
            reject,
            timeoutId,
        })

        try {
            postBridgeMessage({
                kind: "webCallRequest",
                requestId,
                letter: call,
            })
        } catch (error) {
            rejectPendingWebCall(requestId, error instanceof Error ? error : new Error("发送WebCall失败"))
        }
    })
}

export async function sendWebCallAndTakeItsReturnValues(call: WebLetter) {
    const response = await sendWebCall(call)
    if (response.state == WebResponseState.Success) return response.returnValues
    throw new Error(response.error?.message ?? "WebCall failed without error message")
}
export function makeWebLetter(name: string, ...args: any[]): WebLetter {
    return {name, args}
}

try {
    console.log(TAG, "Start init")

    const webViewBridgeHost = getWebViewBridgeHost()
    if (webViewBridgeHost) {
        transport = createWebViewTransport(webViewBridgeHost)
        transportReady = true
        console.log(TAG, "Using WebView2 bridge transport")
    } else {
        transport = createWebSocketTransport()
        console.log(TAG, "Using WebSocket bridge transport")
    }

    transport.addListener((message: KurisuBridgeMessage) => {
        try {
            handleIncomingBridgeMessage(message)
        } catch (err) {
            console.log(TAG, "处理Bridge消息异常", err)
        }
    })

    flushOutboundQueue()

    window.addEventListener("beforeunload", () => {
        rejectAllPendingWebCalls(new Error("页面卸载，WebCall已取消"))
    })
} catch (err) {
    console.error(TAG, "遇到问题，App初始化终止，将报错", err)
} finally {
    console.log(TAG, "Init over")
}
