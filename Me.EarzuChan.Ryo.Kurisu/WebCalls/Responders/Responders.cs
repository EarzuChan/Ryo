using System;

namespace Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;

public interface IWebCallResponder
{
    public WebResponse Respond(KurisuAppContext context);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class WebCallResponderAttribute(string name, bool scannable = true, bool isDev = false) : Attribute
{
    public readonly bool Scannable = scannable;
    public readonly bool IsDev = isDev;
    public readonly string EventName = name;

    // TODO：HandlerType，什么TYPE？？？
}
