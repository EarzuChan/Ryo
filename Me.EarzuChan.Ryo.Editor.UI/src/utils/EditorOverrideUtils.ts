import type {EditorContext, EditorOverrideRule, RyoType, SessionPathSegment} from "@/models/AppModels"

export function appendEditorPath(basePath: string, segment: SessionPathSegment): string {
    if (typeof segment === "number") return `${basePath}[${segment}]`
    return basePath === "$" ? `$.${segment}` : `${basePath}.${segment}`
}

export function dataTypeNameFromRyoType(ryoType: RyoType): string {
    return ryoType.isArray ? `${ryoType.typeName}[]` : ryoType.typeName
}

type PathToken =
    | { kind: "root" }
    | { kind: "prop"; value: string }
    | { kind: "index"; value: number }
    | { kind: "prop-wildcard" }
    | { kind: "index-wildcard" }

function tokenizePathPattern(path: string): PathToken[] {
    if (!path || path[0] !== "$") return []
    const tokens: PathToken[] = [{kind: "root"}]
    let i = 1

    while (i < path.length) {
        const char = path[i]
        if (char === ".") {
            i++
            let start = i
            while (i < path.length && path[i] !== "." && path[i] !== "[") i++
            const segment = path.slice(start, i)
            tokens.push(segment === "*" ? {kind: "prop-wildcard"} : {kind: "prop", value: segment})
            continue
        }

        if (char === "[") {
            const end = path.indexOf("]", i)
            if (end === -1) return []
            const raw = path.slice(i + 1, end)
            tokens.push(raw === "*" ? {kind: "index-wildcard"} : {kind: "index", value: Number.parseInt(raw, 10)})
            i = end + 1
            continue
        }

        return []
    }

    return tokens
}

export function matchPathPattern(pattern: string, path: string): { matched: boolean; score: number } {
    const patternTokens = tokenizePathPattern(pattern)
    const pathTokens = tokenizePathPattern(path)
    if (patternTokens.length === 0 || patternTokens.length !== pathTokens.length) return {matched: false, score: -1}

    let score = 0

    for (let i = 0; i < patternTokens.length; i++) {
        const patternToken = patternTokens[i]
        const pathToken = pathTokens[i]

        if (patternToken.kind === "root" && pathToken.kind === "root") {
            score += 1
            continue
        }

        if (patternToken.kind === "prop-wildcard" && pathToken.kind === "prop") continue
        if (patternToken.kind === "index-wildcard" && pathToken.kind === "index") continue

        if (patternToken.kind === "prop" && pathToken.kind === "prop" && patternToken.value === pathToken.value) {
            score += 3
            continue
        }

        if (patternToken.kind === "index" && pathToken.kind === "index" && patternToken.value === pathToken.value) {
            score += 3
            continue
        }

        return {matched: false, score: -1}
    }

    return {matched: true, score}
}

export function matchTypePattern(pattern: string, typeName: string): { matched: boolean; score: number } {
    if (!pattern) return {matched: false, score: -1}
    if (pattern === typeName) return {matched: true, score: 1000}

    const escaped = pattern.replace(/[.+?^${}()|[\]\\]/g, "\\$&").replace(/\*/g, ".*")
    const regex = new RegExp(`^${escaped}$`)
    if (!regex.test(typeName)) return {matched: false, score: -1}

    const wildcardCount = (pattern.match(/\*/g) ?? []).length
    return {matched: true, score: 100 - wildcardCount}
}

export function ruleMatchesContext(rule: EditorOverrideRule, context: EditorContext): { matched: boolean; score: number } {
    if (!rule.enabled) return {matched: false, score: -1}
    if (rule.typeConstraint) {
        const typeConstraintMatch = matchTypePattern(rule.typeConstraint, context.dataTypeName)
        if (!typeConstraintMatch.matched) return {matched: false, score: -1}
    }

    return rule.scope === "path"
        ? matchPathPattern(rule.pattern, context.path)
        : matchTypePattern(rule.pattern, context.dataTypeName)
}
