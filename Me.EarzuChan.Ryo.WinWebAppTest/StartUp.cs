using Me.EarzuChan.Ryo.WinWebAppSystem;

namespace Me.EarzuChan.Ryo.WinWebAppTest
{
    public class StartUp
    {
        [STAThread]
        private static void Main(string[] args)
        {
            WinWebApp
                .CreateBuilder(new()
                {
#if DEBUG
                    IsDev = true,
#endif
                })
                .Build()
                .Run();
        }
    }
}