using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.AppEvents.Handlers
{
    public enum AppEventHandlerRegistrationStrategy
    {
        ScanAndRegisterAutomatically,
        RegisterManually,
    }

    public interface IAppEventHandler
    {
        public void Handle(WinWebAppContext context);
    }

    public interface IAppEventHandlerForCallBack
    {
        public object[] Handle(WinWebAppContext context);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class AppEventHandlerAttribute : Attribute
    {
        public readonly bool Scannable;
        public readonly bool IsDev;
        public readonly AppEventType EventType;

        // TODO:HandlerType

        public AppEventHandlerAttribute(AppEventType eventType, bool scannable = true, bool isDev = false)
        {
            EventType = eventType;
            Scannable = scannable;
            IsDev = isDev;
        }
    }
}
