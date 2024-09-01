using Me.EarzuChan.Ryo.Exceptions;

namespace Me.EarzuChan.Ryo.Utils;

public static class LanguageExtensiveUtils
{
    public static T Also<T>(this T obj, Action<T> action)
    {
        action(obj);

        return obj;
    }

    public static void Ensure<T>(this T? obj, Action<T> action)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj), "Ensure failed: Value is null");

        action(obj!);
    }

    public static void TryCatchingThenThrow(string errorPrefix, Action action,
        Dictionary<Type, String>? exceptionReplacements = null) =>
        TryCatchingThenThrow<object>(errorPrefix, () =>
        {
            action();
            return null;
        }, exceptionReplacements);

    public static T? TryCatchingThenThrow<T>(string errorPrefix, Func<T?> action,
        Dictionary<Type, String>? exceptionReplacements = null)
    {
        try
        {
            return action.Invoke();
        }
        catch (Exception ex)
        {
            if (exceptionReplacements == null)
                throw new RyoException($"{errorPrefix}, due to {ex.Message.MakeFirstCharLower()}.", ex);

            foreach (var customEx in exceptionReplacements.Where(customEx => ex.GetType() == customEx.Key))
            {
                ex = new RyoException(customEx.Value);
                break;
            }

            throw new RyoException($"{errorPrefix}, due to {ex.Message.MakeFirstCharLower()}.", ex);
        }
    }
}