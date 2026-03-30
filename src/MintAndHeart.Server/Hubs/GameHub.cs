using Microsoft.AspNetCore.SignalR;
using MintAndHeart.Server.Services;

namespace MintAndHeart.Server.Hubs;

// Hub for real-time client-server communication
public class GameHub : Hub
{
    private readonly RoomService _roomService;

    public GameHub(RoomService roomService)
    {
        _roomService = roomService;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Player connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Player disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task Ping()
    {
        var message = _roomService.TestConnection();
        Console.WriteLine($"Received Ping from {Context.ConnectionId}");
        await Clients.Caller.SendAsync("Pong", message);
    }

    // Join a game room
    public async Task JoinRoom(string roomId, string playerName)
    {
        // Step 1: Call RoomService to process room join
        var result = _roomService.JoinRoom(roomId, Context.ConnectionId, playerName);

        // Step 2: Handle failure case
        if (!result.IsSuccess)
        {
            // Send error message only to caller
            await Clients.Caller.SendAsync("Error", result.ErrorMessage);
            return;
        }

        // Step 3: Add this connection to the SignalR group (by room ID)
        // Now all messages sent to this group will reach this player
        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

        // Step 4: Broadcast to all players in the room
        // All users in the same group (room) will receive this message
        await Clients.Group(roomId).SendAsync("PlayerJoined", playerName);

        Console.WriteLine($"Player '{playerName}' joined room '{roomId}'");
    }
}