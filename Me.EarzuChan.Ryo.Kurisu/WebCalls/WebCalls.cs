using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Kurisu.WebCalls;

public class WebResponse
{
    public WebResponseState State;
    public object[] ReturnValues;
    // 脑瘫Api设计，返回值为什么是[]而不是单个呢，抽象

    public WebResponse(WebResponseState state, params object[] returnValues)
    {
        returnValues ??= Array.Empty<object>();

        State = state;
        ReturnValues = returnValues;
    }
}

public enum WebResponseState
{
    Failure,
    Success
}