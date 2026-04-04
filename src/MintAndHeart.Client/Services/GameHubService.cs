using Microsoft.AspNetCore.SignalR.Client;
using MintAndHeart.Shared.DTOs;

namespace MintAndHeart.Client.Services;

public class GameHubService : IAsyncDisposable
{
    private readonly HubConnection _connection;

    // ── 세션 상태 ─────────────────────────────────────────────────────────────
    public string MyNickname       { get; private set; } = "";
    public string MyPlayerId       { get; private set; } = "";
    public string CurrentRoomId    { get; private set; } = "";
    public string OpponentNickname { get; private set; } = "";

    // 마지막으로 받은 게임 상태/결과 (페이지 전환 후에도 유지)
    public GameStateDto?  LastGameState  { get; private set; }
    public GameResultDto? LastGameResult { get; private set; }

    // ── 이벤트 ────────────────────────────────────────────────────────────────
    public event Action<string, string>?         OnRoomCreated;       // (roomId, playerId)
    public event Action<string, string, string?>? OnRoomJoined;       // (roomId, playerId, opponentNickname)
    public event Action<string>?                 OnPlayerJoined;      // (nickname)
    public event Action<int>?                    OnCountdownStarted;  // (seconds)
    public event Action<GameStateDto, string>?   OnGameStarted;       // (state, myPlayerId)
    public event Action<GameStateDto>?           OnGameStateUpdated;  // (state)
    public event Action<GameResultDto>?          OnGameOver;
    public event Action<string>?                 OnPlayerLeft;
    public event Action<string>?                 OnPlayerDisconnected;
    public event Action<TransferOrderDto>?       OnTransferStarted;
    public event Action<string, string>?         OnTransferStopped;   // (fromNodeId, toNodeId)
    public event Action<string>?                 OnError;

    public GameHubService(string hubUrl)
    {
        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        RegisterHandlers();
    }

    // ── 연결 ─────────────────────────────────────────────────────────────────

    public async Task EnsureConnectedAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            await _connection.StartAsync();
            Console.WriteLine("[Hub] Connected");
        }
    }

    // ── Client → Server ──────────────────────────────────────────────────────

    public async Task CreateRoomAsync(string mapId, string nickname)
    {
        MyNickname = nickname;
        await _connection.InvokeAsync("CreateRoom", mapId, nickname);
    }

    public async Task JoinRoomAsync(string roomId, string nickname)
    {
        MyNickname = nickname;
        await _connection.InvokeAsync("JoinRoom", roomId, nickname);
    }

    public async Task StartTransferAsync(string fromNodeId, string toNodeId) =>
        await _connection.InvokeAsync("StartTransfer", fromNodeId, toNodeId);

    public async Task StopTransferAsync(string fromNodeId, string toNodeId) =>
        await _connection.InvokeAsync("StopTransfer", fromNodeId, toNodeId);

    // ── 핸들러 등록 ──────────────────────────────────────────────────────────

    private void RegisterHandlers()
    {
        _connection.On<string, string>("RoomCreated", (roomId, playerId) =>
        {
            CurrentRoomId = roomId;
            MyPlayerId    = playerId;
            OnRoomCreated?.Invoke(roomId, playerId);
        });

        _connection.On<string, string, string?>("RoomJoined", (roomId, playerId, opp) =>
        {
            CurrentRoomId    = roomId;
            MyPlayerId       = playerId;
            OpponentNickname = opp ?? "";
            OnRoomJoined?.Invoke(roomId, playerId, opp);
        });

        _connection.On<string>("PlayerJoined", nickname =>
        {
            if (MyPlayerId == "player1") OpponentNickname = nickname;
            OnPlayerJoined?.Invoke(nickname);
        });

        _connection.On<int>("CountdownStarted", s => OnCountdownStarted?.Invoke(s));

        _connection.On<GameStateDto, string>("GameStarted", (dto, myPlayerId) =>
        {
            MyPlayerId     = myPlayerId;
            LastGameState  = dto;
            OnGameStarted?.Invoke(dto, myPlayerId);
        });

        _connection.On<GameStateDto>("GameStateUpdated", dto =>
        {
            LastGameState = dto;
            OnGameStateUpdated?.Invoke(dto);
        });

        _connection.On<GameResultDto>("GameOver", dto =>
        {
            LastGameResult = dto;
            OnGameOver?.Invoke(dto);
        });

        _connection.On<string>("PlayerLeft",        n => OnPlayerLeft?.Invoke(n));
        _connection.On<string>("PlayerDisconnected", n => OnPlayerDisconnected?.Invoke(n));

        _connection.On<TransferOrderDto>("TransferStarted", dto => OnTransferStarted?.Invoke(dto));
        _connection.On<string, string>("TransferStopped",   (f, t) => OnTransferStopped?.Invoke(f, t));

        _connection.On<string>("Error", code => OnError?.Invoke(code));
    }

    public async ValueTask DisposeAsync() => await _connection.DisposeAsync();
}
