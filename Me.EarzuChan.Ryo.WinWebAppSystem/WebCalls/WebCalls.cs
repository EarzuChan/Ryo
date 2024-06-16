using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.WebCalls
{
    public class WebResponse
    {
        public WebResponseState State;
        public object[] ReturnValues;

        public WebResponse(WebResponseState state, params object[] returnValues)
        {
            returnValues ??= Array.Empty<object>();

            State = state;
            ReturnValues = returnValues;
        }
    }

    public enum WebResponseState
    {
        Failure, Success
    }
}
