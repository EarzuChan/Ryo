using Newtonsoft.Json;
using System;
using System.Diagnostics;
using Me.EarzuChan.Ryo.Kurisu.Exceptions;
using Me.EarzuChan.Ryo.Kurisu.Misc;

namespace Me.EarzuChan.Ryo.Kurisu.Utils;

public static class DataModelParsingUtils {
    public static WebLetter ParseWebLetterJson(string str) {
        if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException("无效事件：源文本为Null");

        Trace.WriteLine($"WebLetter源文本：{str}");

        WebLetter? model;
        try {
            model = JsonConvert.DeserializeObject<WebLetter>(str);
        } catch (Exception e) {
            var msg = $"无效WebLetter：{str}\nJson反序列化失败：{e}";
            Trace.WriteLine(msg);
            throw new WebEventParsingException(msg);
        }

        return NormalizeWebLetter(model, str, "WebLetter");
    }

    public static KurisuBridgeMessage ParseBridgeMessageJson(string str) {
        if (string.IsNullOrWhiteSpace(str)) throw new NullReferenceException("无效消息：源文本为Null");

        Trace.WriteLine($"Bridge消息源文本：{str}");

        KurisuBridgeMessage? model;
        try {
            model = JsonConvert.DeserializeObject<KurisuBridgeMessage>(str);
        } catch (Exception e) {
            var msg = $"无效Bridge消息：{str}\nJson反序列化失败：{e}";
            Trace.WriteLine(msg);
            throw new WebEventParsingException(msg);
        }

        if (model == null) throw new WebEventParsingException($"无效Bridge消息：{str}\nModel仍为Null");
        if (string.IsNullOrWhiteSpace(model.Kind)) throw new IllegalWebEventException($"无效Bridge消息：{str}\nKind为空");

        model.Kind = model.Kind.Trim();
        if (!string.IsNullOrWhiteSpace(model.RequestId)) model.RequestId = model.RequestId.Trim();

        switch (model.Kind) {
            case KurisuBridgeMessageKinds.WebEvent:
                model.Letter = NormalizeWebLetter(model.Letter, str, "Bridge.WebEvent.letter");
                break;

            case KurisuBridgeMessageKinds.WebCallRequest:
                model.Letter = NormalizeWebLetter(model.Letter, str, "Bridge.WebCallRequest.letter");
                if (string.IsNullOrWhiteSpace(model.RequestId))
                    throw new IllegalWebEventException($"无效Bridge消息：{str}\nWebCallRequest缺少requestId");

                break;
        }

        return model;
    }

    private static WebLetter NormalizeWebLetter(WebLetter? model, string rawText, string sourceName) {
        if (model == null) throw new WebEventParsingException($"无效{sourceName}：{rawText}\nModel仍为Null");
        if (string.IsNullOrWhiteSpace(model.Name)) throw new IllegalWebEventException($"无效{sourceName}：{rawText}\nModel的Name为空");

        model.Name = model.Name.Trim();
        model.Args ??= [];
        return model;
    }
}
