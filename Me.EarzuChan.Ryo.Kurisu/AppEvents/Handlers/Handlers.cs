using System;

namespace Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;

public interface IAppEventHandler
{
    public void Handle(KurisuAppContext context);
}

public interface IAppEventHandlerForCallBack
{
    public object[] Handle(KurisuAppContext context);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AppEventHandlerAttribute : Attribute
{
    public readonly bool Scannable;
    public readonly bool IsDev;
    public readonly AppEventType EventType;

    // TODO：HandlerType

    public AppEventHandlerAttribute(AppEventType eventType, bool scannable = true, bool isDev = false)
    {
        EventType = eventType;
        Scannable = scannable;
        IsDev = isDev;
    }
}
