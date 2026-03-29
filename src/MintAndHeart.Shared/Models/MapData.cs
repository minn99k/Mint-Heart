namespace MintAndHeart.Shared.Models;

// 맵 전체를 정의하는 클래스 (JSON 파일에서 읽어오는 구조와 1:1 대응)
public class MapData
{
    // 맵 고유 ID (파일명과 일치, 예: "korea")
    // 왜 필요? → 방 생성 시 "어떤 맵으로 플레이할지" 지정할 때 이 ID를 씀
    public string MapId { get; set; } = "";

    // 화면에 표시할 맵 이름 (예: "대한민국")
    public string MapName { get; set; } = "";

    // 맵 설명 (로비 화면 등에서 표시용)
    public string Description { get; set; } = "";

    // SVG/Canvas에서 맵을 그릴 때 사용하는 가상 너비 (픽셀)
    // 왜 고정값? → 노드 좌표가 이 크기 기준의 픽셀값이라 크기가 바뀌면 좌표도 같이 스케일해야 함
    public int Width { get; set; }

    // SVG/Canvas 가상 높이 (픽셀)
    public int Height { get; set; }

    // 유닛 이동 속도 (픽셀/초)
    // 예: 150이면 1초에 150px 이동. 거리/속도 = 이동 소요 시간
    public double UnitSpeed { get; set; }

    // 이 맵에 포함된 모든 노드 목록
    public List<NodeData> Nodes { get; set; } = new();

    // 각 플레이어의 시작 노드 ID
    // 예: { "player1": "seoul", "player2": "busan" }
    // 왜 Dictionary? → player1/player2 키로 쉽게 접근 가능
    public Dictionary<string, string> SpawnPoints { get; set; } = new();

    // 플레이어가 시작 노드에서 갖는 초기 유닛 수
    public int PlayerStartUnits { get; set; }
}
