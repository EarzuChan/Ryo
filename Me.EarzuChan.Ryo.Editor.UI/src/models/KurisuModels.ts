const TAG = "KurisuModels"

export interface WebLetter {
    name: string
    args: any[]
}

export interface KurisuHostCapabilities {
    supportsWindowControls: boolean
    supportsWindowStateRead: boolean
    supportsWindowStateWrite: boolean
    supportsOpenFileDialog: boolean
    supportsSaveFileDialog: boolean
}

export enum KurisuWindowState {
    Normal,
    Maximized,
    Minimized
}

export enum WebResponseState {
    Failure,
    Success
}

export interface WebResponse {
    state: WebResponseState
    returnValues: any[]
    error?: {
        code: string
        message: string
        details?: any
    }
}

export type KurisuBridgeMessageKind = "webEvent" | "webCallRequest" | "webCallResponse"

export interface KurisuBridgeMessage {
    kind: KurisuBridgeMessageKind
    requestId?: string
    letter?: WebLetter
    response?: WebResponse
}
