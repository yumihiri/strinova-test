using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework.Graphics;

namespace BallBattle;

/// <summary>characters.json とキャラアイコン画像の読み込みを担当する。</summary>
public static class CharacterLoader
{
    public static List<CharacterData> LoadCharacters(string jsonPath)
    {
        if (!File.Exists(jsonPath))
        {
            return new List<CharacterData>();
        }

        var json = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<CharacterData>>(json, options) ?? new List<CharacterData>();
    }

    /// <summary>
    /// アイコン画像を読み込む。ファイルが見つからない・壊れている場合はfallbackを返す
    /// (アイコンがまだ用意されていないキャラのための保険)。
    /// </summary>
    public static Texture2D LoadIcon(GraphicsDevice graphicsDevice, string contentRoot, string iconRelativePath, Texture2D fallback)
    {
        try
        {
            var fullPath = Path.Combine(contentRoot, iconRelativePath);
            using var stream = File.OpenRead(fullPath);
            return Texture2D.FromStream(graphicsDevice, stream);
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
