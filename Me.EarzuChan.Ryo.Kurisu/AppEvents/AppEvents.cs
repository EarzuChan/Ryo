using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Kurisu.AppEvents
{
    public class AppEvent
    {
        public AppEventType EventType;

        public object[] Args;

        public AppEvent(AppEventType eventType, params object[] args)
        {
            EventType = eventType;
            Args = args;
        }
    }

    public enum AppEventType
    {
        AppWindowStateChanged
    }
}
