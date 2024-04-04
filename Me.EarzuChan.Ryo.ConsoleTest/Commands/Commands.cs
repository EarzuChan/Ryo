using Me.EarzuChan.Ryo.ConsoleSystem;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.ConsoleSystem.OldCommands;
using Me.EarzuChan.Ryo.Core.Adaptions;
using Me.EarzuChan.Ryo.Core.Formations.Universe;
using Me.EarzuChan.Ryo.Core.Formations.WeakPipe;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Exceptions.FileExceptions;
using Me.EarzuChan.Ryo.Extensions.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.ConsoleSystem.OldCommands.IOldCommand;

namespace Me.EarzuChan.Ryo.ConsoleTest.Commands
{
    [Command("TsCc", "4T0")]
    public class TsCsCommand : ICommand
    {
        [AdaptableFormat("我勒个骚纲")]
        public class 骚纲
        {
            public int Inner;
            public string Origin;
            public bool Cnmd;
            public Inner Dinner;
            public Inner[] Wlgd;
            public List<Inner> Wlgbd;
            /*public AdaptableFormat Wlgsg;
            public AdaptableFormat[] Wlgdw;*/
        }

        public void Execute(ConsoleApplicationContext ctx)
        {
            ctx.PrintLine(SerializationUtils.GenerateAdaptableFormatStructure(typeof(骚纲), TsTypeUtils.NonAdaptableFormatHandling.Error));
            ctx.PrintLine(SerializationUtils.GenerateAdaptableFormatStructure(typeof(bool), TsTypeUtils.NonAdaptableFormatHandling.Error));

            var mng = ctx.Inject<MassManager>();
            mng.AddMassFile(new(), "Babe");
            ctx.PrintLine($"爷爷：{mng.MassFiles.Count}");
        }
    }
}
