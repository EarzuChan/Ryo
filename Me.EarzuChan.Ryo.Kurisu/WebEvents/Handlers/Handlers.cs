using System;

namespace Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;

public interface IWebEventHandler
{
    public void Handle(KurisuAppContext context);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class WebEventHandlerAttribute : Attribute
{
    public readonly bool Scannable;
    public readonly bool IsDev;
    public readonly string EventName;

    // TODO：HandlerType

    public WebEventHandlerAttribute(string name, bool scannable = true, bool isDev = false)
    {
        EventName = name;
        Scannable = scannable;
        IsDev = isDev;
    }
}
