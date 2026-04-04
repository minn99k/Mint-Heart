using Microsoft.AspNetCore.SignalR;
using MintAndHeart.Server.Hubs;
using MintAndHeart.Server.Models;
using MintAndHeart.Shared.DTOs;

namespace MintAndHeart.Server.Services;

// 백그라운드 서비스: 서버 시작 시 자동으로 돌기 시작하는 게임 루프
// 100ms마다 Tick()을 실행해 모든 InGame 방의 상태를 업데이트한다
public class GameLoopService : BackgroundService
{
    private const int  TickIntervalMs   = 100;
    private const double CollisionRadius = 10.0;  // px
    private const int  DisconnectTimeoutSec = 30;

    private readonly RoomService         _roomService;
    private readonly GameService         _gameService;
    private readonly IHubContext<GameHub> _hubContext;

    public GameLoopService(RoomService roomService, GameService gameService, IHubContext<GameHub> hubContext)
    {
        _roomService = roomService;
        _gameService = gameService;
        _hubContext  = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("[GameLoop] Started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Tick(); }
            catch (Exception ex) { Console.WriteLine($"[GameLoop] Error: {ex.Message}"); }

            await Task.Delay(TickIntervalMs, stoppingToken);
        }
    }

    private async Task Tick()
    {
        var now   = DateTime.UtcNow;
        var rooms = _roomService.GetAllInGameRooms();

        foreach (var room in rooms)
        {
            lock (room.Lock)
            {
                if (room.GameState == null || room.MapData == null) continue;
                if (room.Status != RoomStatus.InGame)               continue;

                var state = room.GameState;

                // ── 1. 1초 간격: 유닛 생산 + 전송 ────────────────────────────
                if ((now - state.LastProductionTime).TotalSeconds >= 1.0)
                {
                    state.ElapsedSeconds++;
                    state.LastProductionTime = now;

                    // 플레이어 소유 노드마다 유닛 +1
                    foreach (var node in state.Nodes.Values)
                    {
                        if (node.OwnerId != null)
                            node.Units = Math.Min(node.Units + 1, node.MaxUnits);
                    }

                    // 전송 명령 실행 (노드별 라운드 로빈)
                    FireTransfers(state, room.MapData, now);
                }

                // ── 2. 매 틱: 이동 중 상쇄 판정 ─────────────────────────────
                ProcessCollisions(state, now);

                // ── 3. 매 틱: 노드 도착 처리 ─────────────────────────────────
                var arrived = state.MovingUnits
                    .Where(u => u.IsAlive && u.ArrivalTime <= now)
                    .ToList();
                foreach (var unit in arrived)
                {
                    _gameService.ProcessArrival(state, unit);
                    state.MovingUnits.Remove(unit);
                }

                // 소멸된 유닛 제거
                state.MovingUnits.RemoveAll(u => !u.IsAlive);

                // ── 4. 매 틱: 연결 끊김 타임아웃 ────────────────────────────
                foreach (var player in room.Players.Values)
                {
                    if (!player.IsConnected && player.DisconnectedAt.HasValue)
                    {
                        if ((now - player.DisconnectedAt.Value).TotalSeconds >= DisconnectTimeoutSec)
                        {
                            var winnerId = player.PlayerId == "player1" ? "player2" : "player1";
                            state.WinnerId  = winnerId;
                            state.WinReason = "disconnect";
                        }
                    }
                }

                // ── 5. 매 틱: 승리 조건 체크 ─────────────────────────────────
                if (state.WinnerId == null)
                {
                    var (winnerId, reason) = _gameService.CheckWinCondition(state);
                    if (winnerId != null || reason == "draw")
                    {
                        state.WinnerId  = winnerId;
                        state.WinReason = reason;
                    }
                }

                // ── 6. 게임 종료 처리 ─────────────────────────────────────────
                if (state.WinnerId != null || state.WinReason == "draw")
                {
                    room.Status = RoomStatus.Finished;
                }
            }

            // lock 밖에서 비동기 브로드캐스트
            await BroadcastState(room);

            if (room.Status == RoomStatus.Finished)
                await BroadcastGameOver(room);
        }
    }

    // ── 전송 명령 실행 (라운드 로빈) ─────────────────────────────────────────

    private static void FireTransfers(GameState state, MintAndHeart.Shared.Models.MapData map, DateTime now)
    {
        // 노드별로 활성 전송 명령을 CreatedAt 순으로 묶어서 처리
        var byNode = state.ActiveTransfers
            .GroupBy(t => t.FromNodeId)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.CreatedAt).ToList());

        foreach (var (fromNodeId, orders) in byNode)
        {
            if (!state.Nodes.TryGetValue(fromNodeId, out var fromNode)) continue;
            if (fromNode.Units <= 0) continue;

            var fromData = map.Nodes.FirstOrDefault(n => n.Id == fromNodeId);
            if (fromData == null) continue;

            foreach (var order in orders)
            {
                if (fromNode.Units <= 0) break;

                var toData = map.Nodes.FirstOrDefault(n => n.Id == order.ToNodeId);
                if (toData == null) continue;

                double dx       = toData.X - fromData.X;
                double dy       = toData.Y - fromData.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double travelSec = distance / map.UnitSpeed;

                fromNode.Units--;
                state.MovingUnits.Add(new MovingUnit
                {
                    OwnerId       = order.OwnerId,
                    FromNodeId    = fromNodeId,
                    ToNodeId      = order.ToNodeId,
                    StartX        = fromData.X,
                    StartY        = fromData.Y,
                    EndX          = toData.X,
                    EndY          = toData.Y,
                    Speed         = map.UnitSpeed,
                    Distance      = distance,
                    DepartureTime = now,
                    ArrivalTime   = now.AddSeconds(travelSec)
                });
            }
        }
    }

    // ── 이동 중 상쇄 판정 ─────────────────────────────────────────────────────

    private static void ProcessCollisions(GameState state, DateTime now)
    {
        var p1Units = state.MovingUnits.Where(u => u.IsAlive && u.OwnerId == "player1").ToList();
        var p2Units = state.MovingUnits.Where(u => u.IsAlive && u.OwnerId == "player2").ToList();

        foreach (var a in p1Units)
        {
            if (!a.IsAlive) continue;
            double total   = (a.ArrivalTime - a.DepartureTime).TotalSeconds;
            double elapsed = (now - a.DepartureTime).TotalSeconds;
            double prog    = total > 0 ? elapsed / total : 1;
            double ax = a.StartX + (a.EndX - a.StartX) * prog;
            double ay = a.StartY + (a.EndY - a.StartY) * prog;

            foreach (var b in p2Units)
            {
                if (!b.IsAlive) continue;
                double totalB   = (b.ArrivalTime - b.DepartureTime).TotalSeconds;
                double elapsedB = (now - b.DepartureTime).TotalSeconds;
                double progB    = totalB > 0 ? elapsedB / totalB : 1;
                double bx = b.StartX + (b.EndX - b.StartX) * progB;
                double by = b.StartY + (b.EndY - b.StartY) * progB;

                double dist = Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));
                if (dist <= CollisionRadius)
                {
                    a.IsAlive = false;
                    b.IsAlive = false;
                    break;
                }
            }
        }
    }

    // ── 브로드캐스트 ─────────────────────────────────────────────────────────

    private async Task BroadcastState(GameRoom room)
    {
        if (room.GameState == null || room.MapData == null) return;

        GameStateDto dto;
        lock (room.Lock)
        {
            dto = _gameService.ToDto(room.GameState, room.MapData, DateTime.UtcNow);
        }

        await _hubContext.Clients.Group(room.RoomId)
            .SendAsync("GameStateUpdated", dto);
    }

    private async Task BroadcastGameOver(GameRoom room)
    {
        if (room.GameState == null) return;

        var state = room.GameState;
        var winnerNickname = room.Players.Values
            .FirstOrDefault(p => p.PlayerId == state.WinnerId)?.Nickname;

        var finalNodes = state.Nodes.Values.Select(n => new NodeStateDto
        {
            NodeId   = n.NodeId,
            OwnerId  = n.OwnerId,
            Units    = n.Units,
            MaxUnits = n.MaxUnits
        }).ToList();

        var result = new GameResultDto
        {
            WinnerId        = state.WinnerId,
            WinnerNickname  = winnerNickname,
            WinReason       = state.WinReason ?? "draw",
            DurationSeconds = state.ElapsedSeconds,
            Player1Nodes    = state.Nodes.Values.Count(n => n.OwnerId == "player1"),
            Player2Nodes    = state.Nodes.Values.Count(n => n.OwnerId == "player2"),
            Player1Units    = state.Nodes.Values.Where(n => n.OwnerId == "player1").Sum(n => n.Units),
            Player2Units    = state.Nodes.Values.Where(n => n.OwnerId == "player2").Sum(n => n.Units),
            FinalNodes      = finalNodes
        };

        await _hubContext.Clients.Group(room.RoomId).SendAsync("GameOver", result);
    }
}
