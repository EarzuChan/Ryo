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
    type: RyoType
    parseSuccess: boolean
    data?: any
}

export interface RecentFile {
    name: string
    path: string
}