using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Utils;

public static class TypeUtils
{
    public static IEnumerable<Type> GetAppAllTypes() => AppDomain.CurrentDomain.GetAssemblies().SelectMany(x =>
    {
        try
        {
            return x.GetTypes();
        }
        catch (Exception e)
        {
            LogUtils.PrintError("无法取得某程序集的类型", e);
            return Array.Empty<Type>();
        }
    });
}