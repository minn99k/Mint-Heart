// This file is the entry point for the server
using MintAndHeart.Server.Hubs;

namespace MintAndHeart.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add CORS policy: Define CORS rules for later use
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyHeader() // Allow any header
                      .AllowAnyMethod() // Allow any method
                      .SetIsOriginAllowed(_ => true) // Allow any origin
                      .AllowCredentials(); // Allow credentials
            });
        });

        builder.Services.AddSignalR(); // Add SignalR service
        var app = builder.Build();

        // Must be after Routing, before Endpoints
        app.UseCors("AllowAll"); // Use the CORS policy

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapFallbackToFile("index.html");

        app.MapHub<GameHub>("/gamehub"); // Map the GameHub to the /gamehub endpoint
        // GameHub: Handle game logic and communication between clients
        // TODO: Add other endpoints here

        app.Run();
    }
}
