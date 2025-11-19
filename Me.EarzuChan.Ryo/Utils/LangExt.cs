using Me.EarzuChan.Ryo.Exceptions;

namespace Me.EarzuChan.Ryo.Utils;

public static class LangExt
{
    public static T Also<T>(this T obj, Action<T> action)
    {
        action(obj);

        return obj;
    }

    public static void EnsureNotNull<T>(this T? obj, Action<T> action)
    {
        if (obj == null) throw new ArgumentNullException(nameof(obj), "Ensure failed: Value is null");

        action(obj);
    }

    public static void WrappedTry(string errorPrefix, Action action, Dictionary<Type, string>? exceptionReplacements = null) =>
        WrappedTry<object>(errorPrefix, () =>
        {
            action();
            return null;
        }, exceptionReplacements);

    public static T? WrappedTry<T>(string errorPrefix, Func<T?> action,
        Dictionary<Type, string>? exceptionReplacements = null)
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