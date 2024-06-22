const TAG = "Models"

export interface WebLetter {
    name: string
    args: any[]
}

export enum WinWebAppWindowState {
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

export interface TreeNodeModel {
    name: string
    children?: TreeNodeModel[]
}

export interface MassFile {
    name: string
    items: MassItem[]
}

export interface MassItem {
    id: number,
    name: string
}

export interface TabModel {
    name: string,
    nonResident?: boolean
    unsaved?: boolean
    page?: any
    data?: any
}

export interface MemberType {
    name: string
    type: string
}

export interface TypeSchema {
    type: string
    members?: MemberType[]
}

export interface RyoType {
    baseType?: TypeSchema
    isArray: boolean
    typeName: string
}

export interface ItemModel {
    id: number
    name?: string
    type: RyoType
    parseSuccess: boolean
    data?: any
}

export interface DialogActionButtonModel {
    text: string
    onClick?: () => boolean | void
}

export interface DialogModel {
    icon?: string
    headline?: string
    description?: string
    showOverlay?: boolean
    actions?: DialogActionButtonModel[]
    onClosed?: () => void
    onClose?: () => void
    onOpened?: () => void
    onOpen?: () => void
}

export interface MenuItem {
    name: string
    action?: () => void
    children?: MenuItem[]
}

export interface MenuModel {
    items: MenuItem[]
    onClose?: () => void
    onClosed?: () => void
    onCloseOnMenuItem?: () => void
    top?: number
    left?: number
    attachToId?: string
    attachMethod?: AttachMethod
    closeOnClickOverlay?: boolean
}

export enum AttachMethod {
    UpLeft,
    UpRight,
    DownLeft,
    DownRight
}

export interface MenuBarItem {
    name: string
    id: string
}