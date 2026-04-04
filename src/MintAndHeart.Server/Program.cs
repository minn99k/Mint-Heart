// Entry point for the server
using MintAndHeart.Server.Hubs;
using MintAndHeart.Server.Services;

namespace MintAndHeart.Server;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure CORS to allow all origins
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyHeader()
                      .AllowAnyMethod()
                      .SetIsOriginAllowed(_ => true)
                      .AllowCredentials();
            });
        });

        builder.Services.AddSignalR();

        // Register singleton services (initialized at startup)
        builder.Services.AddSingleton<MapLoader>();
        builder.Services.AddSingleton<RoomService>();
        builder.Services.AddSingleton<GameService>();
        builder.Services.AddHostedService<GameLoopService>();

        var app = builder.Build();

        // Force load maps at startup
        app.Services.GetRequiredService<MapLoader>();

        app.UseCors("AllowAll");
        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapHub<GameHub>("/gamehub");

        // Map REST API endpoints
        app.MapGet("/api/maps/{mapId}", (string mapId, MapLoader mapLoader) =>
        {
            var map = mapLoader.GetMap(mapId);
            return map is null ? Results.NotFound() : Results.Ok(map);
        });

        app.MapGet("/api/maps", (MapLoader mapLoader) =>
            Results.Ok(mapLoader.GetAvailableMaps()));
        
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}
