using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents.Handlers
{
    public enum HandlerRegistrationStrategy
    {
        ScanAndRegisterAutomatically,
        RegisterManually,
    }

    public interface IHandler
    {
        public void Handle(WinWebAppContext context);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class HandlerAttribute : Attribute
    {
        public readonly bool Scannable;
        private readonly bool IsDev;
        public readonly string EventName;

        // TODO:HandlerType

        public HandlerAttribute(string name, bool scannable = true, bool isDev = false)
        {
            EventName = name;
            Scannable = scannable;
            IsDev = isDev;
        }
    }
}
