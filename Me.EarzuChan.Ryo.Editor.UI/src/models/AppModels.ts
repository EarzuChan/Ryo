const TAG = "AppModels"

export interface MassFile {
    name: string
    items: MassItem[]
}

export interface MassItem {
    id: number,
    name: string
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
    unsaved?: boolean
    type?: RyoType
    parseSuccess: boolean
    data?: any
    history?: HistoryRecord[]
    tempData?: any
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
    name: string,
    nonResident?: boolean
    page?: any
    data?: any
}

export enum TabType {
    Welcome,
    Item,
    Empty
}

export interface HistoryRecord {
}

export interface LearningResourceModel {
    title: string
    description: string
    link: string
}