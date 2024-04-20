using Me.EarzuChan.Ryo.ConsoleSystem;
using Me.EarzuChan.Ryo.ConsoleSystem.Commands;
using Me.EarzuChan.Ryo.Core.Adaptions;
using Me.EarzuChan.Ryo.Core.Formations.DataFormations.WeakPipe;
using Me.EarzuChan.Ryo.Core.Masses;
using Me.EarzuChan.Ryo.Extensions.Utils;
using System.Reflection;

namespace Me.EarzuChan.Ryo.ConsoleTest.Commands
{
    [Command("TsCc")]
    public class TsCsCommand : ICommand
    {
        [AdaptableFormationAttribute("我勒个骚纲")]
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
            /*var hdl = DataTypeSchemaUtils.NonDataTypeHandling.Error;*/

            ctx.PrintLine(typeof(骚纲).ToRyoType().GetDataTypeSchema().NewtonsoftItemToJson());
            ctx.PrintLine(typeof(bool).ToRyoType().GetDataTypeSchema().NewtonsoftItemToJson());
            ctx.PrintLine(typeof(bool[][]).ToRyoType().GetDataTypeSchema().NewtonsoftItemToJson());
        }
    }

    [Command("GaDt")]
    public class GaDtCommand : ICommand
    {
        public void Execute(ConsoleApplicationContext context)
        {
            // 第一步：遍历源集合，调用GetDataTypeSchema方法
            var sourceSchemas = AdaptionManager.INSTANCE.BasicRyoTypes.Select(ryoType => ryoType.GetDataTypeSchema()).ToArray();

            // 第二步：遍历当前程序集中所有带有AdaptableFormat注解的类，调用GetDataTypeSchema方法
            var adaptableSchemas = AppDomain.CurrentDomain.GetAssemblies().SelectMany(asm => asm.GetTypes())
                                          .Where(type => type.GetCustomAttributes<AdaptableFormationAttribute>().Any())
                                          .Select(type => type.ToRyoType().GetDataTypeSchema()).ToArray();

            // 第三步：合并两个列表
            var combinedSchemas = sourceSchemas.Union(adaptableSchemas);

            context.PrintLine(SerializationUtils.NewtonsoftItemToJson(combinedSchemas));
        }
    }
}
