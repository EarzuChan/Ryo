import type {SessionFrame, SessionOperation, SessionPathSegment} from "@/models/AppModels"
import {deepCopy} from "@/utils/UsefulUtils"

const TAG = "SessionUtils"

function cloneValue<T>(value: T): T {
    if (value === undefined) return value
    return deepCopy(value)
}

function isObjectLike(value: any): value is Record<string, any> {
    return value !== null && typeof value === "object" && !Array.isArray(value)
}

function isEqualValue(a: any, b: any): boolean {
    if (a === b) return true

    if (Array.isArray(a) && Array.isArray(b)) {
        if (a.length !== b.length) return false
        for (let i = 0; i < a.length; i++) if (!isEqualValue(a[i], b[i])) return false
        return true
    }

    if (isObjectLike(a) && isObjectLike(b)) {
        const aKeys = Object.keys(a)
        const bKeys = Object.keys(b)
        if (aKeys.length !== bKeys.length) return false

        for (const key of aKeys) {
            if (!(key in b)) return false
            if (!isEqualValue(a[key], b[key])) return false
        }
        return true
    }

    return false
}

function appendSet(path: SessionPathSegment[], value: any, forward: SessionOperation[], backward: SessionOperation[], oldValue: any) {
    forward.push({type: "set", path, value: cloneValue(value)})
    backward.push({type: "set", path, value: cloneValue(oldValue)})
}

function appendRemove(path: SessionPathSegment[], oldValue: any, forward: SessionOperation[], backward: SessionOperation[]) {
    forward.push({type: "remove", path})
    backward.push({type: "set", path, value: cloneValue(oldValue)})
}

function appendAdd(path: SessionPathSegment[], value: any, forward: SessionOperation[], backward: SessionOperation[]) {
    forward.push({type: "set", path, value: cloneValue(value)})
    backward.push({type: "remove", path})
}

function buildDiff(path: SessionPathSegment[], oldValue: any, newValue: any, forward: SessionOperation[], backward: SessionOperation[]) {
    if (isEqualValue(oldValue, newValue)) return

    const oldArray = Array.isArray(oldValue)
    const newArray = Array.isArray(newValue)
    if (oldArray || newArray) {
        if (!oldArray || !newArray || oldValue.length !== newValue.length) {
            appendSet(path, newValue, forward, backward, oldValue)
            return
        }

        for (let i = 0; i < oldValue.length; i++) buildDiff(path.concat(i), oldValue[i], newValue[i], forward, backward)
        return
    }

    const oldObject = isObjectLike(oldValue)
    const newObject = isObjectLike(newValue)
    if (oldObject && newObject) {
        const oldKeys = Object.keys(oldValue)
        const newKeys = Object.keys(newValue)

        for (const key of oldKeys) if (!(key in newValue)) appendRemove(path.concat(key), oldValue[key], forward, backward)

        for (const key of newKeys) if (!(key in oldValue)) appendAdd(path.concat(key), newValue[key], forward, backward)

        for (const key of newKeys) if (key in oldValue) buildDiff(path.concat(key), oldValue[key], newValue[key], forward, backward)
        
        return
    }

    appendSet(path, newValue, forward, backward, oldValue)
}

export function buildSessionFrame(oldData: any, newData: any): SessionFrame | null {
    const forward: SessionOperation[] = []
    const backward: SessionOperation[] = []

    buildDiff([], oldData, newData, forward, backward)

    if (forward.length === 0) return null

    backward.reverse()
    return {
        forward,
        backward,
        timestamp: Date.now(),
    }
}

function ensureContainer(parent: any, key: SessionPathSegment): any {
    if (Array.isArray(parent)) {
        const index = key as number
        if (parent[index] === undefined) parent[index] = {}
        return parent[index]
    }

    if (!(key in parent) || parent[key] === undefined || parent[key] === null) parent[key] = {}
    return parent[key]
}

function resolveParent(root: any, path: SessionPathSegment[]): [any, SessionPathSegment] {
    let parent = root
    for (let i = 0; i < path.length - 1; i++) parent = ensureContainer(parent, path[i])
    return [parent, path[path.length - 1]]
}

function applyOperation(root: any, operation: SessionOperation): any {
    if (operation.path.length === 0) {
        if (operation.type === "remove") return undefined
        return cloneValue(operation.value)
    }

    const [parent, key] = resolveParent(root, operation.path)
    if (operation.type === "remove") {
        if (Array.isArray(parent)) {
            const index = key as number
            if (index >= 0 && index < parent.length) parent.splice(index, 1)
        } else delete parent[key]
        
        return root
    }

    const value = cloneValue(operation.value)
    if (Array.isArray(parent)) parent[key as number] = value
    else parent[key] = value
    return root
}

export function applyOperations(rootData: any, operations: SessionOperation[]): any {
    let root = cloneValue(rootData)
    for (const operation of operations) root = applyOperation(root, operation)
    return root
}

export function hasUndo(undoStack: SessionFrame[]): boolean {
    return undoStack.length > 0
}

export function hasRedo(redoStack: SessionFrame[]): boolean {
    return redoStack.length > 0
}

export function pushUndoFrame(undoStack: SessionFrame[], frame: SessionFrame, maxUndo: number) {
    undoStack.push(frame)
    if (undoStack.length > maxUndo) undoStack.shift()
}

export function debugFrame(frame: SessionFrame) {
    console.debug(TAG, `frame ops: +${frame.forward.length} -${frame.backward.length}`, frame)
}
