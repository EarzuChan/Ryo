import type {KurisuBridgeMessage, WebLetter, WebResponse} from "@/models/KurisuModels"
import {WebResponseState} from "@/models/KurisuModels"

const TAG = "KurisuUtils"
const DEFAULT_CALL_TIMEOUT = 10000

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
type PhotinoBridgeHost = {
    sendMessage: (message: string) => void
    receiveMessage: (listener: (message: string) => void) => void
}

const webEventListeners = new Map<string, Set<WebEventListener>>()
const pendingWebCalls = new Map<string, PendingWebCall>()
const outboundQueue: KurisuBridgeMessage[] = []

let requestCounter = 0
let transport: BridgeTransport | null = null
let transportReady = false
let usingPhotinoTransport = false
let photinoWindowGestureInstalled = false

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

function getWebViewBridgeHost(): WebViewBridgeHost | null {
    const candidate = (globalThis as any)?.chrome?.webview
    if (!candidate) return null
    else if (typeof candidate.postMessage !== "function") return null
    else if (typeof candidate.addEventListener !== "function") return null
    else return candidate as WebViewBridgeHost
}

function getPhotinoBridgeHost(): PhotinoBridgeHost | null {
    const candidate = (globalThis as any)?.external
    if (!candidate) return null
    else if (typeof candidate.sendMessage !== "function") return null
    else if (typeof candidate.receiveMessage !== "function") return null
    else return candidate as PhotinoBridgeHost
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

function createPhotinoTransport(host: PhotinoBridgeHost): BridgeTransport {
    return {
        postMessage: (message: KurisuBridgeMessage) => {
            host.sendMessage(JSON.stringify(message))
        },
        addListener: (handler: BridgeMessageHandler) => {
            host.receiveMessage((raw: string) => {
                try {
                    const parsed = JSON.parse(raw) as KurisuBridgeMessage
                    handler(parsed)
                } catch (error) {
                    console.error(TAG, "解析Photino bridge消息失败", error, raw)
                }
            })
        },
    }
}

function installPhotinoWindowGestures() {
    if (photinoWindowGestureInstalled) return
    photinoWindowGestureInstalled = true

    let dragging = false
    let activePointerId: number | null = null
    let pendingDragPoint: { x: number; y: number } | null = null
    let lastSentDragPoint: { x: number; y: number } | null = null
    let dragMoveFrameId: number | null = null

    const readTarget = (event: Event) => event.target as HTMLElement | null
    const tryCapturePointer = (target: HTMLElement, pointerId: number) => {
        if (typeof target.setPointerCapture !== "function") return
        try {
            target.setPointerCapture(pointerId)
        } catch (error) {
            console.warn(TAG, "设置 pointer capture 失败", error)
        }
    }
    const toScreenPoint = (event: PointerEvent) => {
        const fallbackX = window.screenX + event.clientX
        const fallbackY = window.screenY + event.clientY
        const x = Number.isFinite(event.screenX) ? event.screenX : fallbackX
        const y = Number.isFinite(event.screenY) ? event.screenY : fallbackY
        return {
            x: Math.round(x),
            y: Math.round(y),
        }
    }
    const flushDragMove = () => {
        dragMoveFrameId = null
        if (!dragging || !pendingDragPoint) return

        const nextPoint = pendingDragPoint
        pendingDragPoint = null
        if (lastSentDragPoint?.x === nextPoint.x && lastSentDragPoint.y === nextPoint.y) return

        lastSentDragPoint = nextPoint
        emitWebEvent(makeWebLetter("HostWindow:DragMove", nextPoint.x, nextPoint.y))
    }
    const scheduleDragMove = () => {
        if (dragMoveFrameId !== null) return
        dragMoveFrameId = window.requestAnimationFrame(flushDragMove)
    }
    const clearDragState = () => {
        if (dragMoveFrameId !== null) {
            window.cancelAnimationFrame(dragMoveFrameId)
            dragMoveFrameId = null
        }
        pendingDragPoint = null
        lastSentDragPoint = null
        activePointerId = null
        dragging = false
    }
    const stopDrag = () => {
        if (dragging) emitWebEvent(makeWebLetter("HostWindow:DragEnd"))
        clearDragState()
    }
    const stopDragByPointer = (event: PointerEvent) => {
        if (activePointerId !== null && event.pointerId !== activePointerId) return
        stopDrag()
    }

    window.addEventListener("pointerdown", (event: PointerEvent) => {
        if (event.button !== 0) return

        const target = readTarget(event)
        if (!target) return

        if (!target.closest("[data-kurisu-drag]")) return
        if (target.closest("[data-kurisu-no-drag]")) return

        event.preventDefault()
        event.stopPropagation()
        dragging = true
        activePointerId = event.pointerId
        tryCapturePointer(target, event.pointerId)

        const screen = toScreenPoint(event)
        lastSentDragPoint = screen
        pendingDragPoint = null
        emitWebEvent(makeWebLetter("HostWindow:DragBegin", screen.x, screen.y))
    })

    window.addEventListener("pointermove", (event: PointerEvent) => {
        if (!dragging) return
        if (activePointerId !== null && event.pointerId !== activePointerId) return
        pendingDragPoint = toScreenPoint(event)
        scheduleDragMove()
    })

    window.addEventListener("pointerup", stopDragByPointer)
    window.addEventListener("pointercancel", stopDragByPointer)
    window.addEventListener("blur", stopDrag)
}

export function isPhotinoTransportActive() {
    return usingPhotinoTransport
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

    const photinoHost = getPhotinoBridgeHost()
    if (photinoHost) {
        transport = createPhotinoTransport(photinoHost)
        transportReady = true
        usingPhotinoTransport = true
        installPhotinoWindowGestures()
        console.log(TAG, "Using Photino bridge transport")
    } else {
        const webViewBridgeHost = getWebViewBridgeHost()
        if (!webViewBridgeHost) throw new Error("未找到可用Bridge宿主（Photino/WebView2）")

        transport = createWebViewTransport(webViewBridgeHost)
        transportReady = true
        usingPhotinoTransport = false
        console.log(TAG, "Using WebView2 bridge transport")
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
