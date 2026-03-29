namespace MintAndHeart.Shared.Models;

// 맵에 있는 하나의 노드(도시)를 정의
public class NodeData
{
    // 노드 고유 ID (예: "seoul", "busan")
    public string Id { get; set; } = "";

    // 화면에 표시할 이름 (예: "서울", "부산")
    public string Name { get; set; } = "";

    // 맵에서의 X 좌표 (픽셀 단위, 맵 Width 기준)
    public double X { get; set; }

    // 맵에서의 Y 좌표 (픽셀 단위, 맵 Height 기준)
    public double Y { get; set; }

    // 중립 노드의 초기 유닛 수 (플레이어가 점령하려면 이 수만큼 상쇄해야 함)
    public int InitialUnits { get; set; }

    // 이 노드에 쌓일 수 있는 유닛 최대값 (생산 시 이 값을 초과하지 않음)
    public int MaxUnits { get; set; }

    // 수도 여부 (향후 "수도 점령 시 즉시 승리" 등 확장용, 지금은 미사용)
    public bool IsCapital { get; set; }
}
