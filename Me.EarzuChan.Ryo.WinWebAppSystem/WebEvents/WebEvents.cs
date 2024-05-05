using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents
{
    public class WebEvent
    {
        public string Name;

        public object[] Args;

        public WebEvent(string name, object[]? args = null)
        {
            args ??= Array.Empty<object>();
            Name = name;
            Args = args;
        }
    }
}
