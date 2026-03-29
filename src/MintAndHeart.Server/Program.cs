// This file is the entry point for the server
using MintAndHeart.Server.Hubs;
using MintAndHeart.Server.Services;

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

        // Singleton: 서버 시작 시 한 번 생성, 이후 계속 재사용
        // MapLoader → 시작 시 Data/Maps/*.json 파일 전부 읽어서 메모리에 보관
        // RoomService → 나중에 방 생성/관리 로직 추가 예정
        builder.Services.AddSingleton<MapLoader>();
        builder.Services.AddSingleton<RoomService>();


        var app = builder.Build();

        // Singleton은 기본적으로 처음 요청될 때 생성됨 (지연 초기화)
        // MapLoader는 서버 시작 시 바로 파일을 읽어야 하므로 여기서 강제로 생성
        // GetRequiredService<T>() → DI 컨테이너에서 T를 꺼내옴 (없으면 예외)
        app.Services.GetRequiredService<MapLoader>();

        // Must be after Routing, before Endpoints
        app.UseCors("AllowAll"); // Use the CORS policy

        app.UseBlazorFrameworkFiles();
        app.UseStaticFiles();

        app.MapHub<GameHub>("/gamehub"); // Map the GameHub to the /gamehub endpoint

        // --- 맵 데이터 REST API ---
        // Minimal API: app.MapGet(경로, 핸들러) → GET 요청을 처리하는 엔드포인트 등록
        // ASP.NET Core가 MapLoader를 자동으로 주입해줌 (DI)
        // Results.Ok(data)    → HTTP 200 + JSON 직렬화해서 응답
        // Results.NotFound()  → HTTP 404 응답
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
