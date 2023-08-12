using Me.EarzuChan.Ryo.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using Me.EarzuChan.Ryo.Utils;
using System.Text;
using System.Threading.Tasks;

namespace Me.EarzuChan.Ryo.Utils
{
    public static class ControlFlowUtils
    {
        public static void TryCatchingThenThrow(string errorPrefix, Action action, Dictionary<Type, String>? exceptionReplacements = null) =>
        TryCatchingThenThrow<object>(errorPrefix, () => { action(); return null; }, exceptionReplacements);

        public static T TryCatchingThenThrow<T>(string errorPrefix, Func<T> action, Dictionary<Type, String>? exceptionReplacements = null)
        {
            try
            {
                return action.Invoke();
            }
            catch (Exception ex)
            {
                if (exceptionReplacements != null)
                {
                    foreach (var customEx in exceptionReplacements)
                    {
                        if (ex.GetType() == customEx.Key)
                        {
                            ex = new RyoException(customEx.Value);
                            break;
                        }
                    }
                }

                throw new RyoException($"{errorPrefix}, due to {ex.Message.MakeFirstCharLower()}.", ex);
            }
        }
    }
}