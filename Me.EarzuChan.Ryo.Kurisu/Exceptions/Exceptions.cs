using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Kurisu.Exceptions;

public class WebEventParsingException(string reason) : Exception($"Parsing Web Event error: {reason}");

public class IllegalWebEventException(string reason) : Exception($"An illegal Web Event: {reason}");

public class KurisuAppBuildingException(string reason) : Exception($"Building your Kurisu App error: {reason}");

public sealed class KurisuErrorCode {
    public string Value { get; }

    private KurisuErrorCode(string value) => Value = value;

    // Builtin 相关
    public static readonly KurisuErrorCode BuiltinApiNotFound = new("builtin_api_not_found");
    public static readonly KurisuErrorCode BuiltinPropertyParseFailed = new("builtin_property_parse_failed");
    public static readonly KurisuErrorCode BuiltinPropertyNotFound = new("builtin_property_not_found");
    public static readonly KurisuErrorCode BuiltinPropertyReadFailed = new("builtin_property_read_failed");
    public static readonly KurisuErrorCode BuiltinPreferenceNotFound = new("builtin_preference_not_found");
    public static readonly KurisuErrorCode BuiltinCommandParseFailed = new("builtin_command_parse_failed");
    public static readonly KurisuErrorCode BuiltinCommandFailed = new("builtin_command_failed");

    // WebCall 相关
    public static readonly KurisuErrorCode WebCallNotFound = new("web_call_not_found");
    public static readonly KurisuErrorCode WebCallInvalidArguments = new("web_call_invalid_arguments");
    public static readonly KurisuErrorCode WebCallInvalidResponder = new("web_call_invalid_responder");
    public static readonly KurisuErrorCode WebCallNullResponse = new("web_call_null_response");
    public static readonly KurisuErrorCode WebCallExecutionError = new("web_call_execution_error");

    // Bridge 相关
    public static readonly KurisuErrorCode BridgeUnknownKind = new("bridge_unknown_kind");
    public static readonly KurisuErrorCode BridgeInvalidRequest = new("bridge_invalid_request");
    public static readonly KurisuErrorCode BridgeWebCallException = new("bridge_web_call_exception");

    // 通用
    public static readonly KurisuErrorCode CapabilityNotSupported = new("capability_not_supported");
    public static readonly KurisuErrorCode InvalidArgument = new("invalid_argument");
    public static readonly KurisuErrorCode HostOperationFailed = new("host_operation_failed");

    // 隐式转换：允许直接把对象当字符串用（例如日志输出）
    public static implicit operator string(KurisuErrorCode code) => code.Value;
}

// 对应的 Exception 修改
public sealed class KurisuKnownException(KurisuErrorCode errorCode, string message, object? details = null) : Exception(message) {
    public KurisuErrorCode ErrorCode { get; } = errorCode;
    public object? Details { get; } = details;
}