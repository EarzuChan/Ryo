using Me.EarzuChan.Ryo.ConsoleSystem;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.Formations.DataFormations.WeakPipe;
using Me.EarzuChan.Ryo.Core.Utils;
using Me.EarzuChan.Ryo.Extensions.Utils;
using Me.EarzuChan.Ryo.Utils;
using System.Reflection;

namespace Me.EarzuChan.Ryo.ConsoleTest.Commands
{
    [AdaptableFormation("我勒个骚纲")]
    public class 骚纲
    {
        public int Inner;
        public string Origin;
        public bool Cnmd;
        public Inner Dinner;
        public Inner[] Wlgd;
        public List<Inner> Wlgbd;
    }

    [Command("TsCs")]
    public class TsCsCommand : ICommand
    {

        public void Execute(ConsoleApplicationContext ctx)
        {
            /*var hdl = DataTypeSchemaUtils.NonDataTypeHandling.Error;*/

            ctx.PrintLine(typeof(骚纲).ToRyoType().GetDataTypeSchema().ToJsonWithNewtonJson());
            ctx.PrintLine(typeof(bool).ToRyoType().GetDataTypeSchema().ToJsonWithNewtonJson());
            ctx.PrintLine(typeof(List<List<int>[][]>[]).ToRyoType().ToCsType()!.ToString());
        }
    }

    [Command("GaDt")]
    public class GaDtCommand : ICommand
    {
        public void Execute(ConsoleApplicationContext context)
        {
            context.PrintLine(SerializationUtils.ToJsonWithNewtonJson(DataTypeSchemaUtils.GetAllDataTypeSchemas().ToJsonWithNewtonJson()));
        }
    }

    [Command("PsJc")]
    public class PsJcCommand : ICommand
    {
        private readonly string JavaClz;

        public PsJcCommand(string javaClz)
        {
            JavaClz = javaClz;
        }

        public void Execute(ConsoleApplicationContext context)
        {
            var ryoType = JavaClz.JavaClassToRyoType();
            context.PrintLine(ryoType.ToString());
            context.PrintLine($"Java:{ryoType.ToJavaClass()}");
            context.PrintLine($"C#:{ryoType.ToCsType()}");
        }
    }

    [Command("PsCt")]
    public class PsCtCommand : ICommand
    {
        private readonly Type CsType;

        public PsCtCommand(string csType)
        {
            CsType = Type.GetType(csType)!;
        }

        public void Execute(ConsoleApplicationContext context)
        {
            var ryoType = CsType.ToRyoType();
            context.PrintLine(ryoType.ToString());
            context.PrintLine($"Java:{ryoType.ToJavaClass()}");
            context.PrintLine($"C#:{ryoType.ToCsType()}");
        }
    }

    [Command("Ff")]
    public class ForFunCommand : ICommand
    {

        public void Execute(ConsoleApplicationContext context)
        {
            var ts = new Ts();
            context.PrintLine(ts.str);
            ts.GaiTs();
            context.PrintLine(ts.str);
        }
    }


    public class Ts
    {
        public string str = "Ok";
    }

    public static class TsUtils
    {
        public static void GaiTs(this Ts ts)
        {
            var temp = ts;
            temp.str = "太美丽";
            ts = new Ts { str = "只有为你感慨" };
        }
    }
}
