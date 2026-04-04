using Microsoft.AspNetCore.SignalR;
using MintAndHeart.Server.Models;
using MintAndHeart.Server.Services;

namespace MintAndHeart.Server.Hubs;

public class GameHub : Hub
{
    private readonly RoomService          _roomService;
    private readonly GameService          _gameService;
    private readonly MapLoader            _mapLoader;
    private readonly IHubContext<GameHub> _hubContext;

    public GameHub(RoomService roomService, GameService gameService,
                   MapLoader mapLoader, IHubContext<GameHub> hubContext)
    {
        _roomService = roomService;
        _gameService = gameService;
        _mapLoader   = mapLoader;
        _hubContext  = hubContext;
    }

    // ── 연결 / 해제 ──────────────────────────────────────────────────────────

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] Connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] Disconnected: {Context.ConnectionId}");

        var (room, player) = _roomService.DisconnectPlayer(Context.ConnectionId);

        if (room != null && player != null)
        {
            if (room.Status is RoomStatus.Waiting or RoomStatus.Starting)
                await Clients.Group(room.RoomId).SendAsync("PlayerLeft", player.Nickname);
            else if (room.Status == RoomStatus.InGame)
                await Clients.Group(room.RoomId).SendAsync("PlayerDisconnected", player.Nickname);
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ── 방 생성 ──────────────────────────────────────────────────────────────

    public async Task CreateRoom(string mapId, string nickname)
    {
        var result = _roomService.CreateRoom(mapId, Context.ConnectionId, nickname);
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.ErrorCode);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, result.RoomId);
        await Clients.Caller.SendAsync("RoomCreated", result.RoomId, result.PlayerId);
        Console.WriteLine($"[Hub] Room created: {result.RoomId} by '{nickname}'");
    }

    // ── 방 입장 ──────────────────────────────────────────────────────────────

    public async Task JoinRoom(string roomId, string nickname)
    {
        var result = _roomService.JoinRoom(roomId, Context.ConnectionId, nickname);
        if (!result.IsSuccess)
        {
            await Clients.Caller.SendAsync("Error", result.ErrorCode);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
        await Clients.Caller.SendAsync("RoomJoined", roomId, result.PlayerId, result.OpponentNickname);
        await Clients.Group(roomId).SendAsync("PlayerJoined", nickname);

        Console.WriteLine($"[Hub] '{nickname}' joined room '{roomId}'");

        if (result.IsRoomFull)
            _ = StartCountdownAsync(roomId, 3);
    }

    // ── 카운트다운 → 게임 시작 ───────────────────────────────────────────────

    private async Task StartCountdownAsync(string roomId, int seconds)
    {
        await _hubContext.Clients.Group(roomId).SendAsync("CountdownStarted", seconds);
        await Task.Delay(seconds * 1000);

        // 게임 상태 초기화
        var room = _roomService.GetRoom(roomId);
        if (room == null) return;

        var map = _mapLoader.GetMap(room.MapId);
        if (map == null)
        {
            await _hubContext.Clients.Group(roomId).SendAsync("Error", "MAP_NOT_FOUND");
            return;
        }

        lock (room.Lock)
        {
            room.MapData   = map;
            room.GameState = _gameService.InitializeGame(map, room.Players);
            room.Status    = RoomStatus.InGame;
        }

        // 초기 GameStateDto 생성
        var dto = _gameService.ToDto(room.GameState, map, DateTime.UtcNow);

        // 각 플레이어에게 자신의 playerId와 함께 GameStarted 전송
        foreach (var player in room.Players.Values)
        {
            await _hubContext.Clients.Client(player.ConnectionId)
                .SendAsync("GameStarted", dto, player.PlayerId);
        }

        Console.WriteLine($"[Hub] Game started in room '{roomId}'");
    }

    // ── 전송 시작/중단 ───────────────────────────────────────────────────────

    public async Task StartTransfer(string fromNodeId, string toNodeId)
    {
        var room = FindRoomByConnection();
        if (room?.GameState == null) { await Clients.Caller.SendAsync("Error", "GAME_NOT_ACTIVE"); return; }

        var player = room.Players.GetValueOrDefault(Context.ConnectionId);
        if (player == null) return;

        bool ok; string? error;
        lock (room.Lock)
        {
            (ok, error) = _gameService.StartTransfer(room.GameState, player.PlayerId, fromNodeId, toNodeId);
        }

        if (!ok) { await Clients.Caller.SendAsync("Error", error); return; }

        var orderDto = room.GameState.ActiveTransfers
            .LastOrDefault(t => t.OwnerId == player.PlayerId
                && t.FromNodeId == fromNodeId && t.ToNodeId == toNodeId);

        await Clients.Group(room.RoomId).SendAsync("TransferStarted", new MintAndHeart.Shared.DTOs.TransferOrderDto
        {
            Id         = orderDto?.Id ?? "",
            OwnerId    = player.PlayerId,
            FromNodeId = fromNodeId,
            ToNodeId   = toNodeId
        });
    }

    public async Task StopTransfer(string fromNodeId, string toNodeId)
    {
        var room = FindRoomByConnection();
        if (room?.GameState == null) { await Clients.Caller.SendAsync("Error", "GAME_NOT_ACTIVE"); return; }

        var player = room.Players.GetValueOrDefault(Context.ConnectionId);
        if (player == null) return;

        bool ok; string? error;
        lock (room.Lock)
        {
            (ok, error) = _gameService.StopTransfer(room.GameState, player.PlayerId, fromNodeId, toNodeId);
        }

        if (!ok) { await Clients.Caller.SendAsync("Error", error); return; }

        await Clients.Group(room.RoomId).SendAsync("TransferStopped", fromNodeId, toNodeId);
    }

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────────

    private GameRoom? FindRoomByConnection() =>
        _roomService.GetRoomByConnectionId(Context.ConnectionId);
}
