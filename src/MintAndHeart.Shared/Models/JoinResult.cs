namespace MintAndHeart.Shared.Models;

public class JoinResult
{
    public bool IsSuccess { get; set; }

    // ROOM_NOT_FOUND / ROOM_FULL / NICKNAME_EMPTY / NICKNAME_TOO_LONG / GAME_ALREADY_STARTED
    public string ErrorCode { get; set; } = "";

    // 생성/입장된 방 ID
    public string RoomId { get; set; } = "";

    // "player1" or "player2"
    public string PlayerId { get; set; } = "";

    // 이 입장으로 방이 꽉 찼는지 (카운트다운 트리거 여부)
    public bool IsRoomFull { get; set; }

    // 입장 시 이미 있던 상대 닉네임 (player2에게만 전달)
    public string? OpponentNickname { get; set; }
}
