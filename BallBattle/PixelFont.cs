using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BallBattle;

/// <summary>
/// コンテンツパイプライン(SpriteFont)を使わずに文字を描画するための、
/// 5x7ドットの簡易ビットマップフォント。UIで使う英数字のみ収録。
/// </summary>
public static class PixelFont
{
    private const int GlyphWidth = 5;
    private const int GlyphHeight = 7;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['0'] = new[] { "#####", "#...#", "#...#", "#...#", "#...#", "#...#", "#####" },
        ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
        ['2'] = new[] { "#####", "....#", "....#", "#####", "#....", "#....", "#####" },
        ['3'] = new[] { "#####", "....#", "....#", "#####", "....#", "....#", "#####" },
        ['4'] = new[] { "#...#", "#...#", "#...#", "#####", "....#", "....#", "....#" },
        ['5'] = new[] { "#####", "#....", "#....", "#####", "....#", "....#", "#####" },
        ['6'] = new[] { "#####", "#....", "#....", "#####", "#...#", "#...#", "#####" },
        ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
        ['8'] = new[] { "#####", "#...#", "#...#", "#####", "#...#", "#...#", "#####" },
        ['9'] = new[] { "#####", "#...#", "#...#", "#####", "....#", "....#", "#####" },
        ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
        ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
        ['E'] = new[] { "#####", "#....", "#....", "###..", "#....", "#....", "#####" },
        ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['I'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
        ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
        ['N'] = new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" },
        ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
        ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
        ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
        ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
        ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
        [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
        ['/'] = new[] { "....#", "....#", "...#.", "..#..", ".#...", "#....", "#...." },
        ['!'] = new[] { "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.." },
    };

    public static void DrawText(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, int pixelSize, Color color)
    {
        var cursor = position;
        foreach (var ch in text.ToUpperInvariant())
        {
            if (Glyphs.TryGetValue(ch, out var rows))
            {
                for (var row = 0; row < GlyphHeight; row++)
                {
                    for (var col = 0; col < GlyphWidth; col++)
                    {
                        if (rows[row][col] == '#')
                        {
                            var dest = new Rectangle(
                                (int)(cursor.X + col * pixelSize),
                                (int)(cursor.Y + row * pixelSize),
                                pixelSize,
                                pixelSize);
                            spriteBatch.Draw(pixel, dest, color);
                        }
                    }
                }
            }
            cursor.X += (GlyphWidth + 1) * pixelSize;
        }
    }

    public static int MeasureWidth(string text, int pixelSize)
    {
        return text.Length * (GlyphWidth + 1) * pixelSize;
    }

    public static int Height(int pixelSize) => GlyphHeight * pixelSize;
}
