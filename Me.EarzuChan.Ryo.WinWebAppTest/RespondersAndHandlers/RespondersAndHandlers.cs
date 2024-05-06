using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.WinWebAppSystem;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents.Handlers;
using Me.EarzuChan.Ryo.WinWebAppSystem.Windows;
using System.Diagnostics;

namespace Me.EarzuChan.Ryo.WinWebAppTest.RespondersAndHandlers
{
    [WebEventHandler("Test")]
    public class TestHandler : IWebEventHandler
    {
        private readonly string Msg;

        public TestHandler(string msg) { Msg = msg; }

        /*public TestHandler(int num) { Msg = "这这不能"; } // 现在不能，以后有得你能*/

        public TestHandler() { Msg = "Members out!"; }

        public void Handle(WinWebAppContext context)
        {
            Trace.WriteLine($"What can he say? {Msg}");
        }
    }

    [WebEventHandler("CallBack")]
    public class CallBackHandler : IWebEventHandlerForCallBack
    {
        public object[] Handle(WinWebAppContext context)
        {
            Trace.WriteLine("Copied that, sir!");
            return new object[] { "What", "Can", 0, false };
        }
    }

    [WebEventHandler("GetAllDataTypes")]
    public class GetAllDataTypesHandler : IWebEventHandlerForCallBack
    {
        public object[] Handle(WinWebAppContext context) => DataTypeSchemaUtils.GetAllDataTypeSchemas();
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
    public class ZdhHandler : IWebEventHandler
    {
        private readonly WinWebAppWindowState State;

        public ZdhHandler(Int64 state) => State = (WinWebAppWindowState)state;

        public void Handle(WinWebAppContext context) => context.SetAppWindowState(State);
    }
}
