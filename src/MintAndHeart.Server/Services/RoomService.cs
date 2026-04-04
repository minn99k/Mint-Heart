using System.Collections.Concurrent;
using MintAndHeart.Server.Models;
using MintAndHeart.Shared.Models;

namespace MintAndHeart.Server.Services;

public class RoomService
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    // ── 방 생성 ─────────────────────────────────────────────────────────────

    public JoinResult CreateRoom(string mapId, string connectionId, string nickname)
    {
        var validationError = ValidateNickname(nickname);
        if (validationError != null)
            return Fail(validationError);

        // 유일한 6자리 코드 생성
        string roomId;
        do { roomId = Random.Shared.Next(100000, 999999).ToString(); }
        while (_rooms.ContainsKey(roomId));

        var room = new GameRoom { RoomId = roomId, MapId = mapId };
        room.Players[connectionId] = new PlayerInfo
        {
            ConnectionId = connectionId,
            PlayerId     = "player1",
            Nickname     = nickname,
            IsConnected  = true
        };
        _rooms[roomId] = room;

        return new JoinResult { IsSuccess = true, RoomId = roomId, PlayerId = "player1" };
    }

    // ── 방 입장 ─────────────────────────────────────────────────────────────

    public JoinResult JoinRoom(string roomId, string connectionId, string nickname)
    {
        var validationError = ValidateNickname(nickname);
        if (validationError != null)
            return Fail(validationError);

        if (!_rooms.TryGetValue(roomId, out var room))
            return Fail("ROOM_NOT_FOUND");

        if (room.Status != RoomStatus.Waiting)
            return Fail("GAME_ALREADY_STARTED");

        if (room.IsFull)
            return Fail("ROOM_FULL");

        var opponentNickname = room.Players.Values.First().Nickname;

        room.Players[connectionId] = new PlayerInfo
        {
            ConnectionId = connectionId,
            PlayerId     = "player2",
            Nickname     = nickname,
            IsConnected  = true
        };
        room.Status = RoomStatus.Starting;

        return new JoinResult
        {
            IsSuccess        = true,
            RoomId           = roomId,
            PlayerId         = "player2",
            IsRoomFull       = true,
            OpponentNickname = opponentNickname
        };
    }

    // ── 연결 끊김 처리 ───────────────────────────────────────────────────────

    // 반환: (room, player) — 둘 다 null이면 해당 연결이 어느 방에도 없음
    public (GameRoom? room, PlayerInfo? player) DisconnectPlayer(string connectionId)
    {
        foreach (var room in _rooms.Values)
        {
            if (!room.Players.TryGetValue(connectionId, out var player))
                continue;

            player.IsConnected   = false;
            player.DisconnectedAt = DateTime.UtcNow;

            // 게임 시작 전이면 방에서 완전히 제거
            if (room.Status == RoomStatus.Waiting || room.Status == RoomStatus.Starting)
            {
                room.Players.Remove(connectionId);
                if (room.Players.Count == 0)
                    _rooms.TryRemove(room.RoomId, out _);
                else
                    room.Status = RoomStatus.Waiting; // 혼자 남으면 다시 Waiting
            }
            // InGame이면 isConnected=false만 유지 → GameLoopService가 30초 타임아웃 처리 예정

            return (room, player);
        }
        return (null, null);
    }

    // ── 조회 ─────────────────────────────────────────────────────────────────

    public GameRoom? GetRoom(string roomId) =>
        _rooms.TryGetValue(roomId, out var room) ? room : null;

    // GameLoopService에서 매 틱마다 호출 — InGame 방 목록 반환
    public IEnumerable<GameRoom> GetAllInGameRooms() =>
        _rooms.Values.Where(r => r.Status == RoomStatus.InGame);

    public GameRoom? GetRoomByConnectionId(string connectionId) =>
        _rooms.Values.FirstOrDefault(r => r.Players.ContainsKey(connectionId));

    public void SetRoomStatus(string roomId, RoomStatus status)
    {
        if (_rooms.TryGetValue(roomId, out var room))
            room.Status = status;
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private static string? ValidateNickname(string nickname)
    {
        if (string.IsNullOrWhiteSpace(nickname)) return "NICKNAME_EMPTY";
        if (nickname.Length > 12)               return "NICKNAME_TOO_LONG";
        return null;
    }

    private static JoinResult Fail(string errorCode) =>
        new() { IsSuccess = false, ErrorCode = errorCode };
}
