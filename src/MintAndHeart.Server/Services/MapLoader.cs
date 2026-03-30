using System.Text.Json;
using MintAndHeart.Shared.Models;

namespace MintAndHeart.Server.Services;

// Loads and caches map data at server startup
public class MapLoader
{
    private readonly Dictionary<string, MapData> _maps = new();

    public MapLoader(IWebHostEnvironment env)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var mapsPath = Path.Combine(env.ContentRootPath, "Data", "Maps");

        if (!Directory.Exists(mapsPath))
        {
            Console.WriteLine($"[MapLoader] Warning: Map folder not found at {mapsPath}");
            return;
        }

        foreach (var filePath in Directory.GetFiles(mapsPath, "*.json"))
        {
            var json = File.ReadAllText(filePath);
            var map = JsonSerializer.Deserialize<MapData>(json, options);

            if (map == null)
            {
                Console.WriteLine($"[MapLoader] Warning: Failed to parse {filePath}");
                continue;
            }

            _maps[map.MapId] = map;
            Console.WriteLine($"[MapLoader] Loaded map: {map.MapId} ({map.MapName}, {map.Nodes.Count} nodes)");
        }
    }

    // Get map by ID, returns null if not found
    public MapData? GetMap(string mapId)
    {
        _maps.TryGetValue(mapId, out var map);
        return map;
    }

    // Get list of available map IDs
    public List<string> GetAvailableMaps()
    {
        return _maps.Keys.ToList();
    }
}
