const TAG: string = 'UIModels'

export interface TreeNodeModel {
    name: string
    children?: TreeNodeModel[]
}

export interface TabModel {
    name: string,
    nonResident?: boolean
    unsaved?: boolean
    page?: any
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
    locateToIndex?: number
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

export interface WelcomePageResource {
    title: string
    description: string
    link: string
}