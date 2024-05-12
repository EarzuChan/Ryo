using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.WinWebAppSystem.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.Misc
{
    public class WinWebAppApiBridge
    {
        public readonly WinWebApp App;

        public WinWebAppApiBridge(WinWebApp app) => App = app;

        public string SendWebCall(string webCallJson)
        {
            var response = App.RespondWebCall(DataModelParsingUtils.ParseWebLetterJson(webCallJson));
            return response.ToJson();
        }

        public void EmitWebEvent(string webEventJson)
        {
            App.HandleWebEvent(DataModelParsingUtils.ParseWebLetterJson(webEventJson));
        }
    }
}
