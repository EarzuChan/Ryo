using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Kurisu;
using Me.EarzuChan.Ryo.Kurisu.AppEvents;
using Me.EarzuChan.Ryo.Kurisu.AppEvents.Handlers;
using Me.EarzuChan.Ryo.Kurisu.WebCalls;
using Me.EarzuChan.Ryo.Kurisu.WebCalls.Responders;
using Me.EarzuChan.Ryo.Kurisu.WebEvents;
using Me.EarzuChan.Ryo.Kurisu.WebEvents.Handlers;
using System.Diagnostics;
using System.Windows;
using Me.EarzuChan.Ryo.Kurisu.WindowBackends;

namespace Me.EarzuChan.Ryo.Editor.RespondersAndHandlers
{
    [WebCallResponder("GetAllDataTypes")]
    public class GetAllDataTypesResponder : IWebCallResponder
    {
        public WebResponse Respond(KurisuAppContext context) => new(WebResponseState.Success, DataTypeSchemaUtils.GetAllDataTypeSchemas());
    }

    [WebEventHandler("StopApp")]
    public class StopAppHandler : IWebEventHandler
    {
        public void Handle(KurisuAppContext context)
        {
            Trace.WriteLine("结束应用程序");
            context.StopApp();
        }
    }

    [WebEventHandler("SetAppWindowState")]
    public class SetAppWindowStateHandler : IWebEventHandler
    {
        private readonly KurisuAppWindowState State;

        public SetAppWindowStateHandler(long state) => State = (KurisuAppWindowState)state;

        public void Handle(KurisuAppContext context) => context.SetAppWindowState(State);
    }

    [AppEventHandler(AppEventType.AppWindowStateChanged)]
    public class AppWindowStateChangedHandler : IAppEventHandler
    {
        private readonly KurisuAppWindowState AppWindowState;

        public AppWindowStateChangedHandler(KurisuAppWindowState state) => AppWindowState = state;

        public void Handle(KurisuAppContext context) => context.EmitWebEvent(new WebLetter("AppWindowStateChanged", AppWindowState));
    }

    [WebEventHandler("NotifyAppWindowState")]
    public class NotifyAppWindowStateHandler : IWebEventHandler
    {
        public void Handle(KurisuAppContext context) => context.EmitWebEvent(new WebLetter("AppWindowStateChanged", context.GetAppWindowState()));
    }
}
