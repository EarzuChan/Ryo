namespace Me.EarzuChan.Ryo.Utils
{
    public static class TextUtils
    {
        public static string MakeFirstCharLower(this string text) => char.ToLower(text[0]) + text[1..];

        public static string MakeFirstCharUpper(this string text) => char.ToUpper(text[0]) + text[1..];

        public static string MakeErrorText(string info, Exception? e, bool withStack = false)
        {
            var str = $"Error: {info}";
            int eNum = 1;
            while (e != null)
            {
                str += $"\n\nError Details {eNum}:\n--------------\n{e.Message}\n\nException: {e.GetType()}\nSource: {e.Source}\nStack Trace:\n";
                if (withStack) str += e.StackTrace;
                e = e.InnerException;
                eNum++;
            }
            return str;
        }
    }
}
