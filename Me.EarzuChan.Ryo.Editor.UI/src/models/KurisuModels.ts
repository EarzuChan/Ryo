const TAG = "KurisuModels"

export interface WebLetter {
    name: string
    args: any[]
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
}