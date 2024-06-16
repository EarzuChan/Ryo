using Me.EarzuChan.Ryo.WinWebAppSystem.Exceptions;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebCalls;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.Utils
{
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
            model.Args ??= Array.Empty<object>();

            return model;
        }
    }
}
