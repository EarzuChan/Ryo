using Me.EarzuChan.Ryo.ConsoleSystem;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.Core.Adaptations;
using Me.EarzuChan.Ryo.Core.Formations.DataFormations.WeakPipe;
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
        /*public AdaptableFormat Wlgsg;
        public AdaptableFormat[] Wlgdw;*/
    }

    [Command("TsCs")]
    public class TsCsCommand : ICommand
    {

        public void Execute(ConsoleApplicationContext ctx)
        {
            /*var hdl = DataTypeSchemaUtils.NonDataTypeHandling.Error;*/

            ctx.PrintLine(typeof(骚纲).ToRyoType().GetDataTypeSchema().ToJsonWithNewtonJson());
            ctx.PrintLine(typeof(bool).ToRyoType().GetDataTypeSchema().ToJsonWithNewtonJson());
            ctx.PrintLine(typeof(bool[][]).ToRyoType().GetDataTypeSchema().ToJsonWithNewtonJson());
        }
    }

    [Command("GaDt")]
    public class GaDtCommand : ICommand
    {
        public void Execute(ConsoleApplicationContext context)
        {
            // 第一步：遍历源集合，调用GetDataTypeSchema方法
            var sourceSchemas = AdaptationManager.INSTANCE.BasicRyoTypes.Select(ryoType => ryoType.GetDataTypeSchema()).ToArray();

            // 第二步：遍历当前程序集中所有带有AdaptableFormat注解的类，调用GetDataTypeSchema方法
            var adaptableSchemas = TypeUtils.GetAppAllTypes().Where(type => type.GetCustomAttributes<AdaptableFormationAttribute>().Any())
                                          .Select(type => type.ToRyoType().GetDataTypeSchema()).ToArray();

            // 第三步：合并两个列表
            var combinedSchemas = sourceSchemas.Concat(adaptableSchemas);

            context.PrintLine(SerializationUtils.ToJsonWithNewtonJson(combinedSchemas));
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
            context.PrintLine($"Java:{ryoType.ToJavaClass()}, C#:{ryoType.ToCsType()}");
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
            context.PrintLine($"Java:{ryoType.ToJavaClass()}, C#:{ryoType.ToCsType()}");
        }
    }
}
