using Microsoft.AspNetCore.SignalR;

namespace MintAndHeart.Server.Hubs
{
    // This class is the 'channel' that the client and server can communicate through
    public class GameHub : Hub
    {
        public async Task Ping(){
            Console.WriteLine($"Ping received from: {Context.ConnectionId}");
            await Clients.Caller.SendAsync("Pong", "Responed Messeage from Server!");
        }
        // Leave it empty for now!
        // TODO: Add functions like "create room", "send units" later.
    }
}