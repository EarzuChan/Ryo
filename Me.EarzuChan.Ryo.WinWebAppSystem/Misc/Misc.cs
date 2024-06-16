using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.WinWebAppSystem.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.Misc
{
    public class WinWebAppApiBridge(WinWebApp app)
    {
        public string SendWebCall(string webCallJson)
        {
            var response = app.RespondWebCall(DataModelParsingUtils.ParseWebLetterJson(webCallJson));
            return response.ToJson();
        }

        public void EmitWebEvent(string webEventJson)
        {
            app.HandleWebEvent(DataModelParsingUtils.ParseWebLetterJson(webEventJson));
        }
    }
}
