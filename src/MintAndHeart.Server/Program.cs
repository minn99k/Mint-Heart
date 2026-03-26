namespace MintAndHeart.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var app = builder.Build();

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
