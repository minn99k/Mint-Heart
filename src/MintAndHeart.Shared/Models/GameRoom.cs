// GameRoom, PlayerInfo, RoomStatus는 서버 전용이므로
// MintAndHeart.Server.Models.GameModels 에 정의되어 있습니다.
// Shared 프로젝트가 Server를 참조하면 순환 의존성이 발생하기 때문입니다.
//
// 클라이언트가 필요한 방/게임 정보는 DTOs/Dtos.cs 의 RoomInfoDto, GameStateDto 를 사용하세요.
