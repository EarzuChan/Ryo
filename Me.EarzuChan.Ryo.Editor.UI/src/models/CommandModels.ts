export enum CommandErrorCode {
    Unknown = "unknown",
    Validation = "validation",
    Conflict = "conflict",
    NotFound = "not_found",
    Transport = "transport"
}

export interface CommandError {
    code: CommandErrorCode
    message: string
    recoverHint?: string
    cause?: any
}

export type CommandResult<T> =
    | { ok: true; value: T }
    | { ok: false; error: CommandError }