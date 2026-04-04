using MintAndHeart.Shared.Models;

namespace MintAndHeart.Server.Models;

// ── 방 관리 ──────────────────────────────────────────────────────────────────

public enum RoomStatus
{
    Waiting,   // 1명 대기 중
    Starting,  // 2명 접속, 카운트다운 중
    InGame,    // 게임 진행 중
    Finished   // 게임 종료
}

public class PlayerInfo
{
    public string    ConnectionId  { get; set; } = "";
    public string    PlayerId      { get; set; } = "";  // "player1" / "player2"
    public string    Nickname      { get; set; } = "";
    public bool      IsConnected   { get; set; }
    public DateTime? DisconnectedAt { get; set; }
}

public class GameRoom
{
    public string     RoomId    { get; set; } = "";
    public RoomStatus Status    { get; set; } = RoomStatus.Waiting;
    public string     MapId     { get; set; } = "";
    public DateTime   CreatedAt { get; set; } = DateTime.UtcNow;

    // ConnectionId → PlayerInfo
    public Dictionary<string, PlayerInfo> Players { get; set; } = new();

    // 게임 진행 상태 (InGame 진입 후 초기화)
    public GameState? GameState { get; set; }

    // 맵 데이터 (게임 시작 시 설정)
    public MapData? MapData { get; set; }

    // GameLoopService와 GameHub가 동시에 상태를 건드릴 수 있으므로 lock 객체
    public object Lock { get; } = new();

    public int  PlayerCount => Players.Count;
    public bool IsFull      => Players.Count >= 2;
}

// ── 게임 진행 상태 ────────────────────────────────────────────────────────────

public class GameState
{
    public Dictionary<string, NodeState> Nodes           { get; set; } = new();
    public List<TransferOrder>           ActiveTransfers  { get; set; } = new();
    public List<MovingUnit>              MovingUnits      { get; set; } = new();

    public string?   WinnerId    { get; set; }  // null = 진행 중
    public string?   WinReason   { get; set; }  // conquest / timeout / disconnect / draw
    public DateTime  StartTime   { get; set; }
    public int       ElapsedSeconds { get; set; }
    public DateTime  LastProductionTime { get; set; }  // 1초 생산 간격 체크용
}

public class NodeState
{
    public string  NodeId   { get; set; } = "";
    public string? OwnerId  { get; set; }  // "player1" / "player2" / null
    public int     Units    { get; set; }
    public int     MaxUnits { get; set; }
}

public class TransferOrder
{
    public string   Id         { get; set; } = Guid.NewGuid().ToString();
    public string   OwnerId    { get; set; } = "";
    public string   FromNodeId { get; set; } = "";
    public string   ToNodeId   { get; set; } = "";
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
}

public class MovingUnit
{
    public string   Id         { get; set; } = Guid.NewGuid().ToString();
    public string   OwnerId    { get; set; } = "";
    public string   FromNodeId { get; set; } = "";
    public string   ToNodeId   { get; set; } = "";

    public double StartX { get; set; }
    public double StartY { get; set; }
    public double EndX   { get; set; }
    public double EndY   { get; set; }

    public double   Speed        { get; set; }
    public double   Distance     { get; set; }
    public DateTime DepartureTime { get; set; }
    public DateTime ArrivalTime   { get; set; }

    public bool IsAlive { get; set; } = true;
}
