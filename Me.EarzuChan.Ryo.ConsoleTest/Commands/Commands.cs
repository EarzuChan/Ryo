using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.Core.Adaptions;
using Me.EarzuChan.Ryo.Core.Formations.Universe;
using Me.EarzuChan.Ryo.Core.Formations.WeakPipe;
using Me.EarzuChan.Ryo.Exceptions.FileExceptions;
using Me.EarzuChan.Ryo.Extensions.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Me.EarzuChan.Ryo.ConsoleSystem.Commands.ICommand;

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
            public AdaptableFormat Wlgsg;
            public AdaptableFormat[] Wlgdw;
        }

        public void Execute(CommandFrame commandFrame)
        {
            commandFrame.PrintLine(SerializationUtils.GenerateAdaptableFormatStructure(typeof(骚纲), TsTypeUtils.NonAdaptableFormatHandling.Error));
        }
    }
}
