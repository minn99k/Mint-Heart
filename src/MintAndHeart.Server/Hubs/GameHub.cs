using Microsoft.AspNetCore.SignalR;
using MintAndHeart.Server.Services;

namespace MintAndHeart.Server.Hubs{
    // This class is the 'channel' that the client and server can communicate through
    public class GameHub : Hub{
        private readonly RoomService _roomService;

        public GameHub(RoomService roomService){
            _roomService = roomService;
        }
        public override async Task OnConnectedAsync(){
            Console.WriteLine($"Player connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
            // Call the base method to ensure proper setup
        }

        public override async Task OnDisconnectedAsync(Exception? exception){
            Console.WriteLine($"Player disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception); 
        }

        public async Task Ping(){
            var message = _roomService.TestConnection();
            // Log the received message and the connection ID of the sender
            Console.WriteLine($"Received Ping from {Context.ConnectionId}");
            await Clients.Caller.SendAsync("Pong", message);
            // Clients.Caller: 이 메서드를 호출한 클라이언트에게만 응답
        }
        // Leave it empty for now!
        // TODO: Add functions like "create room", "send units" later.
    }
}