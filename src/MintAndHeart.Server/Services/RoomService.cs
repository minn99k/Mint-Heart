using System.Collections.Concurrent;
using MintAndHeart.Shared.Models;

namespace MintAndHeart.Server.Services;

// Manages game rooms and players
public class RoomService
{
    // Thread-safe storage for rooms (RoomId -> GameRoom)
    private readonly ConcurrentDictionary<string, GameRoom> _rooms;

    public RoomService()
    {
        _rooms = new ConcurrentDictionary<string, GameRoom>();
        
        // Add test room for development
        _rooms["1234"] = new GameRoom { RoomId = "1234", MapId = "korea" };
    }

    public string TestConnection()
    {
        return "RoomService is working correctly!";
    }

    // Add player to room
    public JoinResult JoinRoom(string roomId, string connectionId, string nickname)
    {
        // Check if room exists
        if (!_rooms.TryGetValue(roomId, out var room))
        {
            return new JoinResult
            {
                IsSuccess = false,
                ErrorMessage = "Room not found"
            };
        }

        // Check if room is full (max 2 players)
        if (room.IsFull)
        {
            return new JoinResult
            {
                IsSuccess = false,
                ErrorMessage = "Room is full"
            };
        }

        // Create player and add to room
        var player = new PlayerInfo
        {
            ConnectionId = connectionId,
            Nickname = nickname
        };

        room.Players[connectionId] = player;

        // Update room status if now has 2 players
        if (room.Players.Count == 2)
        {
            room.Status = RoomStatus.Starting;
        }

        return new JoinResult
        {
            IsSuccess = true,
            ErrorMessage = ""
        };
    }
}