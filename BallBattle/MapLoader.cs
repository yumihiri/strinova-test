using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BallBattle;

/// <summary>maps.json の読み込みを担当する。</summary>
public static class MapLoader
{
    public static List<MapData> LoadMaps(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            return new List<MapData>();
        }

        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<MapData>>(json, options) ?? new List<MapData>();
    }
}
