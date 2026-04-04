using MintAndHeart.Server.Models;
using MintAndHeart.Shared.Models;

namespace MintAndHeart.Server.Services;

// 게임 로직 담당: 전송 명령, 상쇄/점령, 승리 판정
// GameLoopService의 Tick에서 호출하거나 GameHub에서 직접 호출함
public class GameService
{
    private const int MaxTransfersPerNode = 3;

    // ── 게임 초기화 ──────────────────────────────────────────────────────────

    // 방이 InGame 상태가 되는 순간 호출. GameState를 초기화한다.
    public GameState InitializeGame(MapData map, Dictionary<string, PlayerInfo> players)
    {
        var state = new GameState
        {
            StartTime          = DateTime.UtcNow,
            LastProductionTime = DateTime.UtcNow,
        };

        // 스폰 포인트 결정
        var spawnNodes = map.SpawnPoints; // { "player1": "seoul", "player2": "busan" }

        foreach (var nodeData in map.Nodes)
        {
            string? ownerId = null;
            int units = nodeData.InitialUnits;

            // 스폰 포인트면 플레이어 소유 노드로 설정
            foreach (var kv in spawnNodes)
            {
                if (kv.Value == nodeData.Id)
                {
                    ownerId = kv.Key; // "player1" or "player2"
                    units   = map.PlayerStartUnits;
                    break;
                }
            }

            state.Nodes[nodeData.Id] = new NodeState
            {
                NodeId   = nodeData.Id,
                OwnerId  = ownerId,
                Units    = units,
                MaxUnits = nodeData.MaxUnits
            };
        }

        return state;
    }

    // ── 전송 명령 ────────────────────────────────────────────────────────────

    public (bool ok, string? error) StartTransfer(GameState state, string playerId, string fromNodeId, string toNodeId)
    {
        if (state.WinnerId != null)          return (false, "GAME_NOT_ACTIVE");
        if (fromNodeId == toNodeId)          return (false, "SELF_TRANSFER");

        if (!state.Nodes.TryGetValue(fromNodeId, out var fromNode))
            return (false, "ROOM_NOT_FOUND");

        if (fromNode.OwnerId != playerId)    return (false, "NOT_YOUR_NODE");

        // 동일 쌍 중복 체크
        if (state.ActiveTransfers.Any(t => t.OwnerId == playerId
                && t.FromNodeId == fromNodeId && t.ToNodeId == toNodeId))
            return (false, "DUPLICATE_TRANSFER");

        // 노드당 최대 3개
        int count = state.ActiveTransfers.Count(t => t.OwnerId == playerId && t.FromNodeId == fromNodeId);
        if (count >= MaxTransfersPerNode)    return (false, "MAX_TRANSFERS");

        state.ActiveTransfers.Add(new TransferOrder
        {
            OwnerId    = playerId,
            FromNodeId = fromNodeId,
            ToNodeId   = toNodeId
        });

        return (true, null);
    }

    public (bool ok, string? error) StopTransfer(GameState state, string playerId, string fromNodeId, string toNodeId)
    {
        var order = state.ActiveTransfers
            .FirstOrDefault(t => t.OwnerId == playerId
                && t.FromNodeId == fromNodeId && t.ToNodeId == toNodeId);

        if (order == null) return (false, "TRANSFER_NOT_FOUND");

        state.ActiveTransfers.Remove(order);
        return (true, null);
    }

    // ── 노드 도착 처리 ────────────────────────────────────────────────────────

    // 이동 유닛이 도착지에 도달했을 때 호출
    public void ProcessArrival(GameState state, MovingUnit unit)
    {
        if (!state.Nodes.TryGetValue(unit.ToNodeId, out var node)) return;

        if (node.OwnerId == unit.OwnerId)
        {
            // 아군 노드 → 합산
            node.Units = Math.Min(node.Units + 1, node.MaxUnits);
        }
        else
        {
            // 적/중립 노드 → 상쇄
            if (node.Units > 0)
            {
                node.Units -= 1;
            }
            else
            {
                // 점령!
                OnNodeCaptured(state, node, unit.OwnerId);
            }
        }
    }

    // 점령 시 처리: 소유권 전환 + 기존 소유자 전송 제거
    private void OnNodeCaptured(GameState state, NodeState node, string newOwnerId)
    {
        var oldOwnerId = node.OwnerId;
        node.OwnerId = newOwnerId;
        node.Units   = 1;

        // 기존 소유자가 이 노드에서 출발하는 전송 명령 전부 제거
        state.ActiveTransfers.RemoveAll(t =>
            t.FromNodeId == node.NodeId && t.OwnerId == oldOwnerId);
    }

    // ── 승리 판정 ────────────────────────────────────────────────────────────

    // 반환: (winnerId, winReason) — null이면 게임 계속
    public (string? winnerId, string? winReason) CheckWinCondition(GameState state)
    {
        int p1Nodes = state.Nodes.Values.Count(n => n.OwnerId == "player1");
        int p2Nodes = state.Nodes.Values.Count(n => n.OwnerId == "player2");

        // 점령 승리
        if (p1Nodes == 0) return ("player2", "conquest");
        if (p2Nodes == 0) return ("player1", "conquest");

        // 시간 승리 (300초 = 5분)
        if (state.ElapsedSeconds >= 300)
        {
            if (p1Nodes != p2Nodes)
                return (p1Nodes > p2Nodes ? "player1" : "player2", "timeout");

            // 노드 수 동률 → 유닛 수 비교
            int p1Units = state.Nodes.Values.Where(n => n.OwnerId == "player1").Sum(n => n.Units);
            int p2Units = state.Nodes.Values.Where(n => n.OwnerId == "player2").Sum(n => n.Units);

            if (p1Units != p2Units)
                return (p1Units > p2Units ? "player1" : "player2", "timeout");

            return (null, "draw");
        }

        return (null, null);
    }

    // ── DTO 변환 헬퍼 ─────────────────────────────────────────────────────────

    // GameState → 클라이언트 브로드캐스트용 DTO 변환
    public MintAndHeart.Shared.DTOs.GameStateDto ToDto(GameState state, MapData map, DateTime now)
    {
        var nodeList = state.Nodes.Values.Select(n => new MintAndHeart.Shared.DTOs.NodeStateDto
        {
            NodeId   = n.NodeId,
            OwnerId  = n.OwnerId,
            Units    = n.Units,
            MaxUnits = n.MaxUnits
        }).ToList();

        var transferList = state.ActiveTransfers.Select(t => new MintAndHeart.Shared.DTOs.TransferOrderDto
        {
            Id         = t.Id,
            OwnerId    = t.OwnerId,
            FromNodeId = t.FromNodeId,
            ToNodeId   = t.ToNodeId
        }).ToList();

        var movingList = state.MovingUnits.Where(u => u.IsAlive).Select(u =>
        {
            double total    = (u.ArrivalTime - u.DepartureTime).TotalSeconds;
            double elapsed  = (now - u.DepartureTime).TotalSeconds;
            double progress = total > 0 ? Math.Clamp(elapsed / total, 0, 1) : 1;
            double cx = u.StartX + (u.EndX - u.StartX) * progress;
            double cy = u.StartY + (u.EndY - u.StartY) * progress;

            return new MintAndHeart.Shared.DTOs.MovingUnitDto
            {
                Id       = u.Id,
                OwnerId  = u.OwnerId,
                CurrentX = cx,
                CurrentY = cy,
                EndX     = u.EndX,
                EndY     = u.EndY,
                Progress = progress
            };
        }).ToList();

        int p1NodeCount  = state.Nodes.Values.Count(n => n.OwnerId == "player1");
        int p2NodeCount  = state.Nodes.Values.Count(n => n.OwnerId == "player2");
        int p1TotalUnits = state.Nodes.Values.Where(n => n.OwnerId == "player1").Sum(n => n.Units)
                         + state.MovingUnits.Count(u => u.IsAlive && u.OwnerId == "player1");
        int p2TotalUnits = state.Nodes.Values.Where(n => n.OwnerId == "player2").Sum(n => n.Units)
                         + state.MovingUnits.Count(u => u.IsAlive && u.OwnerId == "player2");

        return new MintAndHeart.Shared.DTOs.GameStateDto
        {
            Nodes           = nodeList,
            ActiveTransfers = transferList,
            MovingUnits     = movingList,
            Player1TotalUnits = p1TotalUnits,
            Player2TotalUnits = p2TotalUnits,
            Player1NodeCount  = p1NodeCount,
            Player2NodeCount  = p2NodeCount,
            ElapsedSeconds    = state.ElapsedSeconds
        };
    }
}
