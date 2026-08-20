using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BallBattle;

/// <summary>
/// 玉対戦ゲーム - 土台 + 基本UI + キャラJSON化 + キャラ選択/マップ選択画面(要件定義書セクション10の1〜4)。
/// 画面遷移: キャラ選択 → マップ選択 → バトル(結果表示はバトル画面内のオーバーレイ) → (リトライ/キャラ選択に戻る)。
/// バトル自体はCPU対CPUの自動対戦を観戦するのみで、プレイヤーは画面遷移のボタン操作のみ行う。
/// </summary>
public class Game1 : Game
{
    private const int GridCols = 5;
    private const int CellSize = 110;
    private const int IconSize = 64;
    private const int GridStartY = 70;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _fallbackIcon = null!;

    private FontSystem _fontSystem = null!;
    private SpriteFontBase _titleFont = null!;
    private SpriteFontBase _labelFont = null!;
    private SpriteFontBase _smallLabelFont = null!;
    private SpriteFontBase _buttonFont = null!;
    private SpriteFontBase _resultFont = null!;

    private readonly Random _random = new();
    private Ball _ballA = null!;
    private Ball _ballB = null!;
    private string? _resultText;

    private string _contentRoot = "";
    private List<CharacterData> _characters = new();
    private readonly Dictionary<string, Texture2D> _iconCache = new();
    private readonly List<CharacterData> _selectedCharacters = new();

    private List<MapData> _maps = new();
    private MapData? _selectedMap;

    private GameState _state = GameState.CharacterSelect;

    private Rectangle _charNextButtonRect;
    private Rectangle _mapBackButtonRect;
    private Rectangle _mapNextButtonRect;
    private Rectangle _backToSelectButtonRect;
    private Rectangle _retryButtonRect;

    private bool _prevMouseDown;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GameConfig.WindowWidth,
            PreferredBackBufferHeight = GameConfig.WindowHeight,
        };
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Window.Title = "玉対戦ゲーム (Ball Battle)";
    }

    protected override void Initialize()
    {
        var gridRows = (int)Math.Ceiling(23 / (double)GridCols);
        _charNextButtonRect = new Rectangle(GameConfig.WindowWidth / 2 - 80, GridStartY + gridRows * CellSize + 24, 160, 44);
        _mapBackButtonRect = new Rectangle(GameConfig.WindowWidth / 2 - 170, 600, 150, 44);
        _mapNextButtonRect = new Rectangle(GameConfig.WindowWidth / 2 + 20, 600, 150, 44);
        _backToSelectButtonRect = new Rectangle(16, 14, 90, 34);
        _retryButtonRect = new Rectangle(GameConfig.WindowWidth / 2 - 80, GameConfig.FieldTop + GameConfig.FieldSize + 16, 160, 44);

        // base.Initialize()がLoadContent()を呼ぶので、キャラ/マップJSON読み込みが終わってから続きを行う
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });

        _fallbackIcon = new Texture2D(GraphicsDevice, 1, 1);
        _fallbackIcon.SetData(new[] { new Color(160, 160, 160) });

        _contentRoot = Path.Combine(AppContext.BaseDirectory, "Content");
        _characters = CharacterLoader.LoadCharacters(Path.Combine(_contentRoot, "Characters", "characters.json"));
        _maps = MapLoader.LoadMaps(Path.Combine(_contentRoot, "Maps", "maps.json"));
        _selectedMap = _maps.FirstOrDefault();

        // 日本語を含む文字描画用に、同梱のIPAゴシックフォントを読み込む
        _fontSystem = new FontSystem();
        _fontSystem.AddFont(File.ReadAllBytes(Path.Combine(_contentRoot, "Fonts", "ipag.ttf")));
        _titleFont = _fontSystem.GetFont(28);
        _labelFont = _fontSystem.GetFont(16);
        _smallLabelFont = _fontSystem.GetFont(13);
        _buttonFont = _fontSystem.GetFont(20);
        _resultFont = _fontSystem.GetFont(40);
    }

    private Texture2D GetIcon(CharacterData character)
    {
        if (string.IsNullOrEmpty(character.Icon)) return _fallbackIcon;
        if (_iconCache.TryGetValue(character.Icon, out var cached)) return cached;

        var texture = CharacterLoader.LoadIcon(GraphicsDevice, _contentRoot, character.Icon, _fallbackIcon);
        _iconCache[character.Icon] = texture;
        return texture;
    }

    private Rectangle CurrentFieldRect()
    {
        var size = _selectedMap?.Size ?? GameConfig.FieldSize;
        var left = (GameConfig.WindowWidth - size) / 2;
        return new Rectangle(left, GameConfig.FieldTop, size, size);
    }

    /// <summary>キャラ選択・マップ選択で決めた組み合わせでバトルを(再)開始する。</summary>
    private void StartBattle()
    {
        var field = CurrentFieldRect();
        var margin = field.Width / 4;
        var posA = new Vector2(field.Left + margin, field.Top + margin);
        var posB = new Vector2(field.Left + field.Width - margin, field.Top + field.Height - margin);

        var charA = _selectedCharacters.ElementAtOrDefault(0);
        var charB = _selectedCharacters.ElementAtOrDefault(1);

        if (charA is not null && charB is not null)
        {
            _ballA = new Ball(posA, GameConfig.BallRed, charA, GetIcon(charA), _random);
            _ballB = new Ball(posB, GameConfig.BallBlue, charB, GetIcon(charB), _random);
        }
        else
        {
            // 選択が不正な場合のフォールバック(通常は到達しない)
            _ballA = new Ball(posA, GameConfig.BallRed, "RED", _random);
            _ballB = new Ball(posB, GameConfig.BallBlue, "BLUE", _random);
        }

        _resultText = null;
        _state = GameState.Battle;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        var mouse = Mouse.GetState();
        var mouseDown = mouse.LeftButton == ButtonState.Pressed;
        var clicked = mouseDown && !_prevMouseDown;
        _prevMouseDown = mouseDown;

        switch (_state)
        {
            case GameState.CharacterSelect:
                UpdateCharacterSelect(mouse.Position, clicked);
                break;
            case GameState.MapSelect:
                UpdateMapSelect(mouse.Position, clicked);
                break;
            case GameState.Battle:
                UpdateBattle(mouse.Position, clicked);
                break;
        }

        base.Update(gameTime);
    }

    private void UpdateCharacterSelect(Point mousePos, bool clicked)
    {
        if (!clicked) return;

        for (var i = 0; i < _characters.Count; i++)
        {
            if (!GetCharacterCellRect(i).Contains(mousePos)) continue;

            var character = _characters[i];
            if (_selectedCharacters.Contains(character))
            {
                _selectedCharacters.Remove(character);
            }
            else if (_selectedCharacters.Count < 2)
            {
                _selectedCharacters.Add(character);
            }
            return;
        }

        if (_selectedCharacters.Count == 2 && _charNextButtonRect.Contains(mousePos))
        {
            _state = GameState.MapSelect;
        }
    }

    private void UpdateMapSelect(Point mousePos, bool clicked)
    {
        if (!clicked) return;

        for (var i = 0; i < _maps.Count; i++)
        {
            if (GetMapCardRect(i).Contains(mousePos))
            {
                _selectedMap = _maps[i];
                return;
            }
        }

        if (_mapBackButtonRect.Contains(mousePos))
        {
            _state = GameState.CharacterSelect;
            return;
        }

        if (_selectedMap is not null && _mapNextButtonRect.Contains(mousePos))
        {
            StartBattle();
        }
    }

    private void UpdateBattle(Point mousePos, bool clicked)
    {
        if (clicked && _backToSelectButtonRect.Contains(mousePos))
        {
            _selectedCharacters.Clear();
            _state = GameState.CharacterSelect;
            return;
        }

        if (clicked && _retryButtonRect.Contains(mousePos))
        {
            StartBattle();
            return;
        }

        if (_resultText is null)
        {
            var field = CurrentFieldRect();
            _ballA.Update(field);
            _ballB.Update(field);
            ResolveCollision(_ballA, _ballB, field);
            _resultText = GetResultText(_ballA, _ballB);
        }
    }

    private static void ResolveCollision(Ball a, Ball b, Rectangle field)
    {
        if (!a.Alive || !b.Alive) return;
        if (a.InvincibleTimer > 0 || b.InvincibleTimer > 0) return;

        var delta = b.Position - a.Position;
        var dist = delta.Length();
        if (dist >= a.Radius + b.Radius) return;

        var normal = dist > 0.0001f ? delta / dist : new Vector2(1f, 0f);
        if (dist <= 0.0001f) dist = 1f;

        // ヒット時の勢い(相対速度)が大きいほどダメージが大きい
        var relSpeed = (a.Velocity - b.Velocity).Length();
        var damage = (int)(GameConfig.BaseDamage + relSpeed * GameConfig.SpeedDamageFactor);
        a.TakeDamage(damage);
        b.TakeDamage(damage);

        a.InvincibleTimer = GameConfig.InvincibleFrames;
        b.InvincibleTimer = GameConfig.InvincibleFrames;

        // めり込み解消
        var overlap = (a.Radius + b.Radius) - dist;
        var push = overlap / 2f + 1f;
        a.Position -= normal * push;
        b.Position += normal * push;
        a.ClampToField(field);
        b.ClampToField(field);

        // ノックバック(押し出し)
        a.ApplyKnockback(-normal);
        b.ApplyKnockback(normal);
    }

    private static string? GetResultText(Ball a, Ball b)
    {
        if (!a.Alive && !b.Alive) return "引き分け!";
        if (!a.Alive) return $"{b.Name} の勝ち!";
        if (!b.Alive) return $"{a.Name} の勝ち!";
        return null;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GameConfig.WindowBg);

        _spriteBatch.Begin();

        switch (_state)
        {
            case GameState.CharacterSelect:
                DrawCharacterSelect();
                break;
            case GameState.MapSelect:
                DrawMapSelect();
                break;
            case GameState.Battle:
                DrawBattle();
                break;
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private Rectangle GetCharacterCellRect(int index)
    {
        var col = index % GridCols;
        var row = index / GridCols;
        var gridWidth = GridCols * CellSize;
        var gridStartX = (GameConfig.WindowWidth - gridWidth) / 2;
        return new Rectangle(gridStartX + col * CellSize + 4, GridStartY + row * CellSize + 4, CellSize - 8, CellSize - 8);
    }

    private void DrawCharacterSelect()
    {
        const string title = "対戦させる2体を選択";
        var titleSize = _titleFont.MeasureString(title);
        _titleFont.DrawText(_spriteBatch, title, new Vector2(GameConfig.WindowWidth / 2f - titleSize.X / 2f, 16), GameConfig.TextDark);

        for (var i = 0; i < _characters.Count; i++)
        {
            var character = _characters[i];
            var cellRect = GetCharacterCellRect(i);
            var iconRect = new Rectangle(cellRect.X + (cellRect.Width - IconSize) / 2, cellRect.Y + 2, IconSize, IconSize);

            var pickIndex = _selectedCharacters.IndexOf(character);
            var ringColor = pickIndex switch
            {
                0 => GameConfig.BallRed,
                1 => GameConfig.BallBlue,
                _ => GameConfig.FieldBorder,
            };
            var ringThickness = pickIndex >= 0 ? 4 : 1;

            _spriteBatch.Draw(GetIcon(character), iconRect, Color.White);
            DrawRectBorder(iconRect, ringColor, ringThickness);

            var nameSize = _smallLabelFont.MeasureString(character.Name);
            var nameX = cellRect.X + cellRect.Width / 2f - nameSize.X / 2f;
            _smallLabelFont.DrawText(_spriteBatch, character.Name, new Vector2(nameX, iconRect.Bottom + 4), GameConfig.TextDark);
        }

        var status = $"{_selectedCharacters.Count} / 2 体選択中";
        var statusSize = _smallLabelFont.MeasureString(status);
        _smallLabelFont.DrawText(_spriteBatch, status, new Vector2(GameConfig.WindowWidth / 2f - statusSize.X / 2f, _charNextButtonRect.Top - 22), GameConfig.TextDark);

        DrawActionButton(_charNextButtonRect, "つぎへ", _selectedCharacters.Count == 2);
    }

    private Rectangle GetMapCardRect(int index)
    {
        const int cardWidth = 320;
        const int cardHeight = 100;
        var x = (GameConfig.WindowWidth - cardWidth) / 2;
        var y = 140 + index * (cardHeight + 20);
        return new Rectangle(x, y, cardWidth, cardHeight);
    }

    private void DrawMapSelect()
    {
        const string title = "マップを選択";
        var titleSize = _titleFont.MeasureString(title);
        _titleFont.DrawText(_spriteBatch, title, new Vector2(GameConfig.WindowWidth / 2f - titleSize.X / 2f, 16), GameConfig.TextDark);

        for (var i = 0; i < _maps.Count; i++)
        {
            var map = _maps[i];
            var rect = GetMapCardRect(i);
            var selected = ReferenceEquals(_selectedMap, map);

            FillRect(rect, GameConfig.FieldBg);
            DrawRectBorder(rect, selected ? GameConfig.BallBlue : GameConfig.FieldBorder, selected ? 4 : 2);

            var nameSize = _labelFont.MeasureString(map.Name);
            _labelFont.DrawText(_spriteBatch, map.Name, new Vector2(rect.Center.X - nameSize.X / 2f, rect.Center.Y - nameSize.Y / 2f), GameConfig.TextDark);
        }

        DrawActionButton(_mapBackButtonRect, "もどる", true);
        DrawActionButton(_mapNextButtonRect, "バトル開始", _selectedMap is not null);
    }

    private void DrawBattle()
    {
        var field = CurrentFieldRect();

        DrawTitle();
        DrawHpBar(_ballA, field.Left, 66, alignRight: false);
        DrawHpBar(_ballB, field.Left + field.Width - 240, 66, alignRight: true);

        FillRect(field, GameConfig.FieldBg);
        DrawRectBorder(field, GameConfig.FieldBorder, 3);

        DrawBall(_ballA);
        DrawBall(_ballB);

        DrawActionButton(_backToSelectButtonRect, "もどる", true);
        DrawActionButton(_retryButtonRect, "リセット", true);

        if (_resultText is not null)
        {
            DrawResultOverlay(_resultText, field);
        }
    }

    private void DrawTitle()
    {
        const string title = "玉対戦ゲーム";
        var size = _titleFont.MeasureString(title);
        _titleFont.DrawText(_spriteBatch, title, new Vector2(GameConfig.WindowWidth / 2f - size.X / 2f, 12), GameConfig.TextDark);
    }

    private void DrawHpBar(Ball ball, int x, int y, bool alignRight)
    {
        const int width = 240;
        const int height = 22;

        var label = $"{ball.Name}  HP {ball.Hp}/{ball.MaxHp}";
        var labelSize = _labelFont.MeasureString(label);
        var labelX = alignRight ? x + width - labelSize.X : x;
        _labelFont.DrawText(_spriteBatch, label, new Vector2(labelX, y - labelSize.Y - 2), GameConfig.TextDark);

        FillRect(new Rectangle(x, y, width, height), GameConfig.HpBarBg);
        var ratio = ball.MaxHp > 0 ? (float)ball.Hp / ball.MaxHp : 0f;
        var fillWidth = (int)(width * ratio);
        if (fillWidth > 0)
        {
            var fillX = alignRight ? x + width - fillWidth : x;
            FillRect(new Rectangle(fillX, y, fillWidth, height), HpBarColor(ratio));
        }
        DrawRectBorder(new Rectangle(x, y, width, height), GameConfig.TextDark, 2);
    }

    private static Color HpBarColor(float ratio)
    {
        if (ratio > 0.5f) return GameConfig.HpGreen;
        if (ratio > 0.2f) return GameConfig.HpYellow;
        return GameConfig.HpRed;
    }

    private void DrawBall(Ball ball)
    {
        var color = ball.Alive ? ball.Color : GameConfig.DeadGray;
        if (ball.Alive && ball.InvincibleTimer > 0 && (ball.InvincibleTimer / 3) % 2 == 0)
        {
            color = new Color(Math.Min(255, color.R + 70), Math.Min(255, color.G + 70), Math.Min(255, color.B + 70));
        }
        FillCircle(ball.Position, ball.Radius, color);
        DrawCircleOutline(ball.Position, ball.Radius, GameConfig.TextDark, 2);

        if (ball.Icon is not null)
        {
            var iconSize = (int)(ball.Radius * 1.5f);
            var dest = new Rectangle((int)ball.Position.X - iconSize / 2, (int)ball.Position.Y - iconSize / 2, iconSize, iconSize);
            var tint = ball.Alive ? Color.White : new Color(190, 190, 190);
            _spriteBatch.Draw(ball.Icon, dest, tint);
        }
    }

    /// <summary>有効/無効を切り替えられる汎用の画面遷移ボタン。</summary>
    private void DrawActionButton(Rectangle rect, string label, bool enabled)
    {
        var mouse = Mouse.GetState();
        var hover = enabled && rect.Contains(mouse.Position);
        var bg = !enabled ? new Color(190, 190, 190) : hover ? GameConfig.ButtonHoverBg : GameConfig.ButtonBg;
        FillRect(rect, bg);
        DrawRectBorder(rect, GameConfig.TextDark, 2);

        var size = _buttonFont.MeasureString(label);
        var pos = new Vector2(rect.Center.X - size.X / 2f, rect.Center.Y - size.Y / 2f);
        _buttonFont.DrawText(_spriteBatch, label, pos, enabled ? Color.White : new Color(235, 235, 235));
    }

    private void DrawResultOverlay(string text, Rectangle field)
    {
        var overlay = new Color(0, 0, 0, 120);
        FillRect(field, overlay);

        var size = _resultFont.MeasureString(text);
        var pos = new Vector2(
            GameConfig.WindowWidth / 2f - size.X / 2f,
            field.Top + field.Height / 2f - size.Y / 2f);
        _resultFont.DrawText(_spriteBatch, text, pos, Color.White);
    }

    private void FillRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

    private void DrawRectBorder(Rectangle rect, Color color, int thickness)
    {
        FillRect(new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
        FillRect(new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
        FillRect(new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
    }

    private void FillCircle(Vector2 center, float radius, Color color)
    {
        var r = (int)radius;
        for (var y = -r; y <= r; y++)
        {
            var halfWidth = (int)Math.Sqrt(Math.Max(0, r * r - y * y));
            var rect = new Rectangle((int)center.X - halfWidth, (int)center.Y + y, halfWidth * 2, 1);
            FillRect(rect, color);
        }
    }

    private void DrawCircleOutline(Vector2 center, float radius, Color color, int thickness)
    {
        const int segments = 48;
        for (var i = 0; i < segments; i++)
        {
            var angle = MathHelper.TwoPi * i / segments;
            var point = center + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * radius;
            FillRect(new Rectangle((int)point.X - thickness / 2, (int)point.Y - thickness / 2, thickness, thickness), color);
        }
    }
}
