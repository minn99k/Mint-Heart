using System.Text.Json;
using MintAndHeart.Shared.Models;

namespace MintAndHeart.Server.Services;

// MapLoader: 서버가 시작될 때 Data/Maps/ 폴더의 JSON 파일들을 읽어서 메모리에 보관
// 이후 RoomService가 방을 만들 때 "어떤 맵으로 할래?" 하면 여기서 꺼내줌
//
// 왜 Singleton으로 등록하나?
// → 맵 파일은 게임 중 바뀌지 않음. 서버 시작 시 한 번만 읽고 계속 재사용.
//   매 요청마다 파일을 다시 읽으면 느리고 불필요함.
public class MapLoader{
    // 로드된 맵들을 보관하는 딕셔너리
    // 키: mapId ("korea"), 값: 파싱된 MapData 객체
    private readonly Dictionary<string, MapData> _maps = new();

    // IWebHostEnvironment: ASP.NET Core가 주입해주는 환경 정보 객체
    // ContentRootPath → 서버 프로젝트의 루트 경로 (예: D:\House\DEV\MintAndHeart\src\MintAndHeart.Server)
    // 이걸 써서 "실행 환경이 어디든" Data/Maps/ 폴더를 올바르게 찾을 수 있음
    public MapLoader(IWebHostEnvironment env){
        // JSON 직렬화 옵션
        // PropertyNameCaseInsensitive = true → JSON의 "mapId"(camelCase)를 C#의 MapId(PascalCase)에 자동 매핑
        var options = new JsonSerializerOptions{
            PropertyNameCaseInsensitive = true
        };

        // Data/Maps/ 폴더 경로 조합
        var mapsPath = Path.Combine(env.ContentRootPath, "Data", "Maps");

        // 폴더가 없으면 경고만 남기고 종료 (서버는 계속 실행)
        if (!Directory.Exists(mapsPath)){
            Console.WriteLine($"[MapLoader] 경고: 맵 폴더를 찾을 수 없음 → {mapsPath}");
            return;
        }

        // 폴더 안의 모든 .json 파일을 순회하며 로드
        foreach (var filePath in Directory.GetFiles(mapsPath, "*.json")){
            var json = File.ReadAllText(filePath);
            var map = JsonSerializer.Deserialize<MapData>(json, options);

            if (map == null){
                Console.WriteLine($"[MapLoader] 경고: 파싱 실패 → {filePath}");
                continue;
            }

            _maps[map.MapId] = map;
            Console.WriteLine($"[MapLoader] 맵 로드 완료 → {map.MapId} ({map.MapName}, 노드 {map.Nodes.Count}개)");
        }
    }

    // 맵 ID로 MapData를 가져옴
    // 없으면 null 반환 (호출하는 쪽에서 null 체크 필요)
    public MapData? GetMap(string mapId){
        _maps.TryGetValue(mapId, out var map);
        return map;
    }

    // 현재 로드된 모든 맵 ID 목록 반환 (로비에서 "어떤 맵 있어요?" 할 때 사용)
    public List<string> GetAvailableMaps(){
        return _maps.Keys.ToList();
    }
}
