using Me.EarzuChan.Ryo.WinWebAppSystem;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents.WebEventHandlers;
using System.Collections;
using System.Diagnostics;

namespace Me.EarzuChan.Ryo.WinWebAppTest.RespondersAndHandlers
{
    [WebEventHandler("Test")]
    public class TestHandler : IWebEventHandler
    {
        private readonly string Msg;

        public TestHandler(string msg) { Msg = msg; }

        /*public TestHandler(int num) { Msg = "这这不能"; } // 呃呃确实不能*/

        public TestHandler() { Msg = "Members out!"; }

        public void Handle(WinWebAppContext context)
        {
            Trace.WriteLine($"What can he say? {Msg}");
        }
    }

    [WebEventHandler("CallBack")]
    public class CallBackHandler : IWebEventHandler
    {
        public void Handle(WinWebAppContext context)
        {
            Trace.WriteLine("Copied that, sir!");
            context.EmitWebEvent(new WebEvent { Name = "太美丽", Args = new object[] { "What", "Can", 0, false } });
        }
    }

    [WebEventHandler("GetAllDataTypes")]
    public class GetAllDataTypesHandler : IWebEventHandler
    {
        public void Handle(WinWebAppContext context)
        {
            context.EmitWebEvent(new() { Name = "PushAllDataTypes", Args = new object[] { new ArrayList() { } } });
        }
    }
}
