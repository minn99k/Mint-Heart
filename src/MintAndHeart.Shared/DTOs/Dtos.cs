namespace MintAndHeart.Shared.DTOs;

// ── 방 정보 ──────────────────────────────────────────────────────────────────
// RoomCreated / RoomJoined 이벤트 시 사용
public class RoomInfoDto
{
    public string RoomId           { get; set; } = "";
    public string MapId            { get; set; } = "";
    public string MapName          { get; set; } = "";
    public string Player1Nickname  { get; set; } = "";
    public string? Player2Nickname { get; set; }  // null = 아직 대기 중
}

// ── 게임 상태 (브로드캐스트용) ────────────────────────────────────────────────
// GameStarted / GameStateUpdated 이벤트 시 사용
public class GameStateDto
{
    public List<NodeStateDto>     Nodes           { get; set; } = new();
    public List<TransferOrderDto> ActiveTransfers { get; set; } = new();
    public List<MovingUnitDto>    MovingUnits     { get; set; } = new();

    public int Player1TotalUnits { get; set; }  // 노드 + 이동 중 합산
    public int Player2TotalUnits { get; set; }
    public int Player1NodeCount  { get; set; }
    public int Player2NodeCount  { get; set; }
    public int ElapsedSeconds    { get; set; }
}

// 노드 하나의 상태
public class NodeStateDto
{
    public string  NodeId   { get; set; } = "";
    public string? OwnerId  { get; set; }  // "player1" / "player2" / null
    public int     Units    { get; set; }
    public int     MaxUnits { get; set; }
}

// 전송 명령 하나
public class TransferOrderDto
{
    public string Id         { get; set; } = "";
    public string OwnerId    { get; set; } = "";
    public string FromNodeId { get; set; } = "";
    public string ToNodeId   { get; set; } = "";
}

// 이동 중인 유닛 하나 (클라이언트 렌더링용)
public class MovingUnitDto
{
    public string Id       { get; set; } = "";
    public string OwnerId  { get; set; } = "";
    public double CurrentX { get; set; }  // 현재 x 좌표
    public double CurrentY { get; set; }  // 현재 y 좌표
    public double EndX     { get; set; }  // 도착 x 좌표
    public double EndY     { get; set; }  // 도착 y 좌표
    public double Progress { get; set; }  // 0.0 ~ 1.0
}

// ── 게임 결과 ─────────────────────────────────────────────────────────────────
// GameOver 이벤트 시 사용
public class GameResultDto
{
    public string?          WinnerId       { get; set; }  // null = 무승부
    public string?          WinnerNickname { get; set; }
    public string           WinReason      { get; set; } = "";  // conquest/timeout/disconnect/draw
    public int              DurationSeconds { get; set; }
    public int              Player1Nodes   { get; set; }
    public int              Player2Nodes   { get; set; }
    public int              Player1Units   { get; set; }
    public int              Player2Units   { get; set; }
    public List<NodeStateDto> FinalNodes   { get; set; } = new();  // 결과 화면 최종 맵
}
