using Microsoft.AspNetCore.SignalR.Client;

namespace MintAndHeart.Client.Services;

public class GameHubService
{
    // SignalR connection to server
    private readonly HubConnection _connection;

    public GameHubService(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();
    }

    public async Task StartAsync()
    {
        await _connection.StartAsync();
        Console.WriteLine("Connection started");
    }

    public void OnPong(Action<string> handler)
    {
        _connection.On<string>("Pong", handler);
    }

    public void OnPlayerJoined(Action<string> handler)
    {
        // Listen for "PlayerJoined" message from server
        // When a player joins, server sends: Clients.Group(roomId).SendAsync("PlayerJoined", nickname)
        _connection.On<string>("PlayerJoined", handler);
    }

    public void OnError(Action<string> handler)
    {
        // Listen for "Error" message from server
        // If join fails: Clients.Caller.SendAsync("Error", errorMessage)
        _connection.On<string>("Error", handler);
    }

    // Send Ping message to server
    public async Task SendPingAsync()
    {
        await _connection.InvokeAsync("Ping");
    }

    // Send JoinRoom request to server
    // Usage: await gameHubService.JoinRoomAsync("1234", "철수");
    public async Task JoinRoomAsync(string roomId, string nickname)
    {
        try
        {
            // Call server's JoinRoom method
            // Server will respond with either "PlayerJoined" (success) or "Error" (failure)
            await _connection.InvokeAsync("JoinRoom", roomId, nickname);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Join failed: {ex.Message}");
        }
    }
}
