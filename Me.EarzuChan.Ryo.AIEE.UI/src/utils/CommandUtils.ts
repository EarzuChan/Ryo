import {CommandErrorCode, type CommandError, type CommandResult} from "@/models/CommandModels"

export function ok<T>(value: T): CommandResult<T> {
    return {ok: true, value}
}

export function fail<T = never>(error: CommandError): CommandResult<T> {
    return {ok: false, error}
}

export function normalizeCommandError(err: any, fallbackCode: CommandErrorCode = CommandErrorCode.Unknown): CommandError {
    if (typeof err === "object" && err && "code" in err && "message" in err) return err as CommandError

    const message =
        err instanceof Error ? err.message : typeof err === "string" ? err : (() => {
            try {
                return JSON.stringify(err)
            } catch {
                return `${err}`
            }
        })()

    if (message.includes("卷版本冲突")) return {
        code: CommandErrorCode.Conflict,
        message,
        cause: err,
    }

    return {
        code: fallbackCode,
        message,
        cause: err,
    }
}
