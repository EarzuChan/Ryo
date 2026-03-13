import type {RyoType} from "@/models/AppModels"

export const ITEM_EXCHANGE_FORMAT = "aiee.item+json"
export const ITEM_EXCHANGE_VERSION = 1
const BYTE_WRAPPER_KEY = "$bytes"

export interface ItemExchangeEnvelope {
    format: string
    version: number
    dataTypeName?: string
    data: any
}

export interface ParsedItemExchangeDocument {
    envelope?: ItemExchangeEnvelope
    suggestedDataTypeName?: string
    data: any
}

type ResolveTypeByName = (dataTypeName: string) => RyoType

function isPlainObject(value: any): value is Record<string, any> {
    return !!value && typeof value === "object" && !Array.isArray(value)
}

function isByteArrayType(ryoType: RyoType): boolean {
    return ryoType.isArray && ryoType.typeName === "java.lang.Byte"
}

function encodeBytesToBase64(value: any): string {
    if (!Array.isArray(value)) throw new Error("字节数组必须是数组")

    const chars: string[] = []
    for (const entry of value) {
        if (!Number.isInteger(entry) || entry < 0 || entry > 255)
            throw new Error(`无效字节值：${entry}`)
        chars.push(String.fromCharCode(entry))
    }

    let binary = ""
    const chunkSize = 0x8000
    for (let i = 0; i < chars.length; i += chunkSize) binary += chars.slice(i, i + chunkSize).join("")
    return btoa(binary)
}

function decodeBytesFromBase64(base64: string): number[] {
    if (typeof base64 !== "string" || !base64.trim()) return []

    const binary = atob(base64)
    const bytes = new Array<number>(binary.length)
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i)
    return bytes
}

function encodeExchangeValue(value: any, ryoType: RyoType, resolveTypeByName: ResolveTypeByName): any {
    if (value === null || value === undefined) return value

    if (isByteArrayType(ryoType)) {
        return {
            [BYTE_WRAPPER_KEY]: encodeBytesToBase64(value),
        }
    }

    if (ryoType.isArray) {
        if (!Array.isArray(value)) return value
        const elementType = resolveTypeByName(ryoType.typeName)
        return value.map(entry => encodeExchangeValue(entry, elementType, resolveTypeByName))
    }

    if (!ryoType.baseType) return value

    switch (ryoType.baseType.type) {
        case "java.lang.String":
        case "java.lang.Character":
        case "java.lang.Integer":
        case "java.lang.Long":
        case "java.lang.Float":
        case "java.lang.Double":
        case "java.lang.Short":
        case "java.lang.Byte":
        case "java.lang.Boolean":
        case "java.lang.Void":
            return value
        default:
            if (!isPlainObject(value)) return value

            return Object.fromEntries(
                Object.entries(value).map(([key, entryValue]) => {
                    const memberTypeName = ryoType.baseType?.members?.find(member => member.name === key)?.type
                    if (!memberTypeName) return [key, entryValue]
                    return [key, encodeExchangeValue(entryValue, resolveTypeByName(memberTypeName), resolveTypeByName)]
                })
            )
    }
}

function resolveMemberType(parentType: RyoType, memberName: string, resolveTypeByName: ResolveTypeByName): RyoType | undefined {
    const memberTypeName = parentType.baseType?.members?.find(member => member.name === memberName)?.type
    if (!memberTypeName) return undefined
    return resolveTypeByName(memberTypeName)
}

function decodeExchangeValue(value: any, ryoType: RyoType, resolveTypeByName: ResolveTypeByName): any {
    if (value === null || value === undefined) return value

    if (isByteArrayType(ryoType)) {
        if (isPlainObject(value) && typeof value[BYTE_WRAPPER_KEY] === "string")
            return decodeBytesFromBase64(value[BYTE_WRAPPER_KEY])
        return value
    }

    if (ryoType.isArray) {
        if (!Array.isArray(value)) return value
        const elementType = resolveTypeByName(ryoType.typeName)
        return value.map(entry => decodeExchangeValue(entry, elementType, resolveTypeByName))
    }

    if (!ryoType.baseType) return value

    switch (ryoType.baseType.type) {
        case "java.lang.String":
        case "java.lang.Character":
        case "java.lang.Integer":
        case "java.lang.Long":
        case "java.lang.Float":
        case "java.lang.Double":
        case "java.lang.Short":
        case "java.lang.Byte":
        case "java.lang.Boolean":
        case "java.lang.Void":
            return value
        default:
            if (!isPlainObject(value)) return value

            const result: Record<string, any> = {...value}
            for (const [key, entryValue] of Object.entries(value)) {
                const memberType = resolveMemberType(ryoType, key, resolveTypeByName)
                if (!memberType) continue
                result[key] = decodeExchangeValue(entryValue, memberType, resolveTypeByName)
            }

            return result
    }
}

export function buildItemExchangeEnvelope(
    data: any,
    ryoType: RyoType,
    dataTypeName: string,
    resolveTypeByName: ResolveTypeByName
): ItemExchangeEnvelope {
    return {
        format: ITEM_EXCHANGE_FORMAT,
        version: ITEM_EXCHANGE_VERSION,
        dataTypeName,
        data: encodeExchangeValue(data, ryoType, resolveTypeByName),
    }
}

export function parseItemExchangeDocument(jsonText: string): ParsedItemExchangeDocument {
    const parsed = JSON.parse(jsonText)

    if (
        isPlainObject(parsed)
        && parsed.format === ITEM_EXCHANGE_FORMAT
        && typeof parsed.version === "number"
        && "data" in parsed
    ) {
        const envelope = parsed as ItemExchangeEnvelope
        return {
            envelope,
            suggestedDataTypeName: envelope.dataTypeName,
            data: envelope.data,
        }
    }

    return {
        data: parsed,
    }
}

export function decodeImportedItemData(data: any, ryoType: RyoType, resolveTypeByName: ResolveTypeByName): any {
    return decodeExchangeValue(data, ryoType, resolveTypeByName)
}
