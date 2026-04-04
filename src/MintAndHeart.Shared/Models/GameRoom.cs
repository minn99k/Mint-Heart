namespace MintAndHeart.Shared.Models;

// Room status enum
public enum RoomStatus
{
    Waiting,    // Waiting for another player to join
    Starting,   // 2 players ready, game starting
    InGame,     // Game in progress
    Finished    // Game finished
}

// Player information in a room
public class PlayerInfo
{
    // SignalR connection ID
    public string ConnectionId { get; set; } = "";
    public string PlayerId { get; set; } 
    // Player nickname
    public string Nickname { get; set; } = "";

    public bool IsConnected { get; set; }
    public DateTime? DisconnectedAt { get; set; }
}

// Game room information (shared between server and client)
public class GameRoom
{
    // Unique room ID (room code)
    public string RoomId { get; set; } = "";

    // Current room status
    public RoomStatus Status { get; set; } = RoomStatus.Waiting;

    // Players in the room (Key: ConnectionId, Value: PlayerInfo)
    public Dictionary<string, PlayerInfo> Players { get; set; } = new();

    // Map ID to be played in this room
    public string MapId { get; set; } = "";

    // Room creation time
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Get number of players in room
    public int PlayerCount => Players.Count;

    // Check if room is full (max 2 players)
    public bool IsFull => Players.Count >= 2;
}
