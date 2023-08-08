namespace Me.EarzuChan.Ryo.Utils
{
    public static class TextUtils
    {
        public static string MakeFirstCharLower(this string text) => char.ToLower(text[0]) + text[1..];
    }
}
