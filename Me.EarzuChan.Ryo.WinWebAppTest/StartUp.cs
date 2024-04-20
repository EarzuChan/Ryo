using Me.EarzuChan.Ryo.WinWebAppSystem;

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