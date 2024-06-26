﻿using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.WinWebAppSystem;
using Me.EarzuChan.Ryo.WinWebAppSystem.AppEvents;
using Me.EarzuChan.Ryo.WinWebAppSystem.AppEvents.Handlers;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebCalls;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebCalls.Responders;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents.Handlers;
using System.Diagnostics;
using System.Windows;
using Me.EarzuChan.Ryo.WinWebAppSystem.WindowBackends;

namespace Me.EarzuChan.Ryo.Editor.RespondersAndHandlers
{
    [WebCallResponder("GetAllDataTypes")]
    public class GetAllDataTypesResponder : IWebCallResponder
    {
        public WebResponse Respond(WinWebAppContext context) => new(WebResponseState.Success, DataTypeSchemaUtils.GetAllDataTypeSchemas());
    }

    [WebEventHandler("StopApp")]
    public class StopAppHandler : IWebEventHandler
    {
        public void Handle(WinWebAppContext context)
        {
            Trace.WriteLine("结束应用程序");
            context.StopApp();
        }
    }

    [WebEventHandler("SetAppWindowState")]
    public class SetAppWindowStateHandler : IWebEventHandler
    {
        private readonly WinWebAppWindowState State;

        public SetAppWindowStateHandler(long state) => State = (WinWebAppWindowState)state;

        public void Handle(WinWebAppContext context) => context.SetAppWindowState(State);
    }

    [AppEventHandler(AppEventType.AppWindowStateChanged)]
    public class AppWindowStateChangedHandler : IAppEventHandler
    {
        private readonly WinWebAppWindowState AppWindowState;

        public AppWindowStateChangedHandler(WinWebAppWindowState state) => AppWindowState = state;

        public void Handle(WinWebAppContext context) => context.EmitWebEvent(new WebLetter("AppWindowStateChanged", AppWindowState));
    }

    [WebEventHandler("NotifyAppWindowState")]
    public class NotifyAppWindowStateHandler : IWebEventHandler
    {
        public void Handle(WinWebAppContext context) => context.EmitWebEvent(new WebLetter("AppWindowStateChanged", context.GetAppWindowState()));
    }
}
