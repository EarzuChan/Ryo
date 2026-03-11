const TAG = "AppModels"

export interface VolumeItemModel {
    id: number
    name: string
    dirtyInVolume?: boolean
}

export interface VolumeModel {
    id: string
    name: string
    localPath?: string | null
    revision?: number
    unsaved?: boolean
    items: VolumeItemModel[]
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

export type EditorSurface = "inline" | "fullpage" | "dialog"

export interface EditorContext {
    itemKey?: string
    dataTypeName: string
    ryoType: RyoType
    path: string
    isRoot: boolean
}

export interface EditorDescriptor {
    id: string
    titleKey: string
    surface: EditorSurface
    component: any
    priority?: number
    supports?: (context: EditorContext) => boolean
}

export type EditorOverrideScope = "path" | "type"

export interface EditorOverrideRule {
    id: string
    scope: EditorOverrideScope
    pattern: string
    editorId: string
    enabled: boolean
    typeConstraint?: string
    updatedAt: number
}

export interface OverrideRuleSet {
    pathRules: EditorOverrideRule[]
    typeRules: EditorOverrideRule[]
}

export type EditorResolutionSource = "requested" | "once" | "path" | "type" | "default" | "none"

export interface ResolvedEditorSelection {
    editor?: EditorDescriptor
    source: EditorResolutionSource
    matchedRule?: EditorOverrideRule
}

export interface FileModel {
    id: number
    itemKey?: string
    preferredRootEditorId?: string
    fromVolumeId?: string
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
    onceEditorOverrides: Record<string, string>
    editorOverrideVersion: number
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
