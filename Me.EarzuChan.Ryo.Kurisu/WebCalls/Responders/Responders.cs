using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;

public enum WebCallResponderRegistrationStrategy
{
    ScanAndRegisterAutomatically,
    RegisterManually,
}

public interface IOldWebCallResponder
{
    public OldWebResponse Respond(KurisuAppContext context);
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class OldWebCallResponderAttribute : Attribute
{
    public readonly bool Scannable;
    public readonly bool IsDev;
    public readonly string EventName;

    // TODO:HandlerType

    public OldWebCallResponderAttribute(string name, bool scannable = true, bool isDev = false)
    {
        EventName = name;
        Scannable = scannable;
        IsDev = isDev;
    }
}