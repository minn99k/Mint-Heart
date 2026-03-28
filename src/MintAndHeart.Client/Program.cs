using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MintAndHeart.Client.Services;

namespace MintAndHeart.Client;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        // Add the GameHubService to the services
        builder.Services.AddSingleton(sp => new GameHubService(
            builder.HostEnvironment.BaseAddress.TrimEnd('/') + "/gamehub")); // Pass the address directly to the GameHubService
        // Singleton: Only one instance of the service is created and shared throughout the application

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        builder.Services.AddScoped(sp => new HttpClient {
            BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
        }); // Add the HttpClient to the services
        // Scoped: A new instance of the HttpClient is created for each request

        await builder.Build().RunAsync();
    }
}
