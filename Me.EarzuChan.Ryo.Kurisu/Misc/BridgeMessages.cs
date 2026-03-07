using Me.EarzuChan.Ryo.Kurisu.WebCalls;

namespace Me.EarzuChan.Ryo.Kurisu.Misc;

public static class KurisuBridgeMessageKinds {
    public const string WebEvent = "webEvent";
    public const string WebCallRequest = "webCallRequest";
    public const string WebCallResponse = "webCallResponse";
}

public sealed class KurisuBridgeMessage {
    public string Kind = string.Empty;
    public string? RequestId;
    public WebLetter? Letter;
    // ReSharper disable once NotAccessedField.Global
    public WebResponse? Response; // 因为发回前端需要
}
