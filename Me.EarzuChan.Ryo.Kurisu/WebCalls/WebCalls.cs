using System;
using System.Diagnostics.CodeAnalysis;

namespace Me.EarzuChan.Ryo.Kurisu.WebCalls;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class WebError(string code, string message, object? details = null) {
    public string Code = code;
    public string Message = message;
    public object? Details = details;
}

public class WebResponse {
    // ReSharper disable once NotAccessedField.Global
    public WebResponseState State;
    // ReSharper disable once NotAccessedField.Global
    public object[] ReturnValues;
    // ReSharper disable once NotAccessedField.Global
    public WebError? Error;

    public static WebResponse Success(params object[] returnValues) => new(WebResponseState.Success, returnValues);

    public static WebResponse Failure(String code, string message, object? details = null, params object[] returnValues) {
        var response = new WebResponse(WebResponseState.Failure, returnValues.Length > 0 ? returnValues : Array.Empty<object>()) {
            Error = new WebError(code, message, details)
        };
        return response;
    }

    public WebResponse(WebResponseState state, params object[] returnValues) {
        returnValues ??= Array.Empty<object>();

        State = state;
        ReturnValues = returnValues;
        Error = null;
    }
}

public enum WebResponseState {
    Failure,
    Success
}