const TAG = "AppModels"

export interface VolumeModel {
    name: string
    revision?: number
    items: FileModel[]
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

export interface EditorDescriptor {
    id: string
    titleKey: string
    component: any
    priority?: number
}

export interface FileModel {
    id: number
    itemKey?: string
    preferredRootEditorId?: string
    fromFile?: string
    name?: string
    unsaved?: boolean
    dataTypeName?: string
    ryoType?: RyoType
    parseSuccess: boolean
    volumeRevision?: number
    data?: any
    history?: SessionFrame[]
    tempData?: any
    session?: EditorSession
}

export interface RecentFile {
    name: string
    path: string
}

export interface Pair<T, U> {
    first: T
    second: U
}

export interface TabModel {
    key?: string
    name: string,
    nonResident?: boolean
    page?: any
    data?: any
}

export enum TabType {
    Welcome,
    Item,
    Empty,
    Settings
}

export type SessionPathSegment = string | number

export interface SessionOperation {
    type: 'set' | 'remove'
    path: SessionPathSegment[]
    value?: any
}

export interface SessionFrame {
    forward: SessionOperation[]
    backward: SessionOperation[]
    timestamp: number
}

export interface EditorSession {
    sessionId: string
    baselineData: any
    currentData: any
    undoStack: SessionFrame[]
    redoStack: SessionFrame[]
    applying: boolean
    maxUndo: number
}

export interface HistoryRecord extends SessionFrame {
}

export interface ResLinkModel {
    title: string
    description: string
    link: string
}
