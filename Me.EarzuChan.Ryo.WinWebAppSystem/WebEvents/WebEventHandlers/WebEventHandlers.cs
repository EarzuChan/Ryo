using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents.WebEventHandlers
{
    public enum WebEventHandlerRegistrationStrategy
    {
        ScanAndRegisterAutomatically,
        RegisterManually,
    }

    public interface IWebEventHandler
    {
        public void Handle(WinWebAppContext context);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class WebEventHandlerAttribute : Attribute
    {
        public readonly bool Scannable;
        public readonly bool IsDev;
        public readonly string EventName;

        // TODO:HandlerType

        public WebEventHandlerAttribute(string name, bool scannable = true, bool isDev = false)
        {
            EventName = name;
            Scannable = scannable;
            IsDev = isDev;
        }
    }
}
