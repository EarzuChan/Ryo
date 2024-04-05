using Me.EarzuChan.Ryo.WinWebAppSystem;
using Me.EarzuChan.Ryo.WinWebAppSystem.WebEvents.Handlers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.WinWebAppTest.Handlers
{
    [Handler("Test")]
    public class TestHandler : IHandler
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
}
