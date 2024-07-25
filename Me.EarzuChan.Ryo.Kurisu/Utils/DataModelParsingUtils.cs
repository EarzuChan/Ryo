using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Linq;
using Me.EarzuChan.Ryo.Kurisu.Exceptions;

namespace Me.EarzuChan.Ryo.Kurisu.Utils;

public static class DataModelParsingUtils
{
    public static WebLetter ParseWebLetterJson(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) throw new ArgumentNullException("无效事件：源文本为Null");

        Trace.WriteLine($"WebLetter源文本：{str}");

        WebLetter? model;
        try
        {
            model = JsonConvert.DeserializeObject<WebLetter>(str);
        }
        catch (Exception e)
        {
            string msg = $"无效WebLetter：{str}\nJson反序列化失败：{e}";
            Trace.WriteLine(msg);

            throw new WebEventParsingException(msg);
        }

        if (model == null) throw new WebEventParsingException($"无效WebLetter：{str}\nModel仍为Null");
        if (model.Name == null) throw new IllegalWebEventException($"无效WebLetter：{str}\nModel的Name为Null");

        model.Name = model.Name.Trim();
        model.Args ??= [];

        // 尝试把值在int内的long转化为int
        model.Args = model.Args.Select(it => it is long l ? (int)l : it).ToArray();

        return model;
    }
}