using Me.EarzuChan.Ryo.Extensions.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Me.EarzuChan.Ryo.Kurisu.Utils;

namespace Me.EarzuChan.Ryo.Kurisu.Misc
{
    public class KurisuAppApiBridge(KurisuApp app)
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
