using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.WebCalls.Responders
{
    public enum WebCallResponderRegistrationStrategy
    {
        ScanAndRegisterAutomatically,
        RegisterManually,
    }

    public interface IWebCallResponder
    {
        public WebResponse Respond(WinWebAppContext context);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class WebCallResponderAttribute : Attribute
    {
        public readonly bool Scannable;
        public readonly bool IsDev;
        public readonly string EventName;

        // TODO:HandlerType

        public WebCallResponderAttribute(string name, bool scannable = true, bool isDev = false)
        {
            EventName = name;
            Scannable = scannable;
            IsDev = isDev;
        }
    }
}
