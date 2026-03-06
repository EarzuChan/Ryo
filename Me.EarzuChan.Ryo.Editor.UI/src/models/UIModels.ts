const TAG: string = 'UIModels'

export interface TreeNodeModel {
    name: string
    children?: TreeNodeModel[]
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
    closeOnOverlayClick?: boolean
    actions?: DialogActionButtonModel[]
    onClosed?: () => void
    onClose?: () => void
    onOpened?: () => void
    onOpen?: () => void
}

export interface MenuItem {
    name: string
    kind?: "action" | "header" | "divider"
    disabled?: boolean
    action?: () => void
    children?: MenuItem[]
}

export interface ContextMenuContribution {
    title: string
    targetPath: string
    items: MenuItem[]
}

export interface MenuModel {
    items: MenuItem[]
    onClose?: (imm:boolean) => void
    onClosed?: () => void
    onCloseOnMenuItem?: (imm:boolean) => void
    top?: number
    left?: number
    attachToId?: string
    attachMethod?: AttachMethod
    anchorRightToAttach?: boolean
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
