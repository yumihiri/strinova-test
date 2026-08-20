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
    private const int CellSize = 156;
    private const int IconSize = 96;
    private const int GridStartY = 100;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;
    private Texture2D _fallbackIcon = null!;

    private FontSystem _fontSystem = null!;
    private SpriteFontBase _titleFont = null!;
    private SpriteFontBase _labelFont = null!;
    private SpriteFontBase _smallLabelFont = null!;
    private SpriteFontBase _tinyLabelFont = null!;
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
        _charNextButtonRect = new Rectangle(GameConfig.WindowWidth / 2 - 110, GridStartY + gridRows * CellSize + 34, 220, 58);
        _mapBackButtonRect = new Rectangle(GameConfig.WindowWidth / 2 - 210, 500, 200, 58);
        _mapNextButtonRect = new Rectangle(GameConfig.WindowWidth / 2 + 10, 500, 200, 58);
        _backToSelectButtonRect = new Rectangle(20, 20, 120, 46);
        _retryButtonRect = new Rectangle(GameConfig.WindowWidth / 2 - 110, GameConfig.FieldTop + GameConfig.FieldSize + 20, 220, 58);

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
        _titleFont = _fontSystem.GetFont(38);
        _labelFont = _fontSystem.GetFont(20);
        _smallLabelFont = _fontSystem.GetFont(16);
        _tinyLabelFont = _fontSystem.GetFont(14);
        _buttonFont = _fontSystem.GetFont(26);
        _resultFont = _fontSystem.GetFont(48);
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

        // ヒット時の勢い(相対速度)が大きいほどダメージが大きい(通常攻撃分・両者対称)
        var relSpeed = (a.Velocity - b.Velocity).Length();
        var baseDamage = (int)(GameConfig.BaseDamage + relSpeed * GameConfig.SpeedDamageFactor);

        // ダッシュ突進/必殺技で突進中だった側は、相手に追加ダメージ+スタンを与える(一方向)
        var aWasBursting = a.BurstTimer > 0;
        var bWasBursting = b.BurstTimer > 0;

        var damageToA = baseDamage + (bWasBursting ? b.CurrentBurstBonusDamage : 0);
        var damageToB = baseDamage + (aWasBursting ? a.CurrentBurstBonusDamage : 0);

        a.TakeDamage(damageToA);
        b.TakeDamage(damageToB);

        if (bWasBursting) a.ApplyStun(b.CurrentBurstStunFrames);
        if (aWasBursting) b.ApplyStun(a.CurrentBurstStunFrames);

        // 突進は命中した時点で終了する
        if (aWasBursting) a.EndBurstOnHit();
        if (bWasBursting) b.EndBurstOnHit();

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
        return new Rectangle(gridStartX + col * CellSize + 5, GridStartY + row * CellSize + 5, CellSize - 10, CellSize - 10);
    }

    private void DrawCharacterSelect()
    {
        var gridRows = (int)Math.Ceiling(_characters.Count / (double)GridCols);
        var gridWidth = GridCols * CellSize;
        var gridStartX = (GameConfig.WindowWidth - gridWidth) / 2;
        var panelRect = new Rectangle(gridStartX - 14, GridStartY - 14, gridWidth + 28, gridRows * CellSize + 28);

        DrawTitleWithAccent("対戦させる2体を選択", GameConfig.AccentPurple);

        DrawPanel(panelRect);

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
                _ => GameConfig.PanelBorder,
            };
            var ringThickness = pickIndex >= 0 ? 3 : 1;

            if (pickIndex >= 0)
            {
                // 選択中は淡いグロー(一回り大きい角丸を薄い色で敷く)を足して目立たせる
                FillRoundedRect(Inflate(iconRect, 5), GameConfig.IconRadius + 4, new Color((int)ringColor.R, (int)ringColor.G, (int)ringColor.B, 60));
            }

            FillRoundedRect(iconRect, GameConfig.IconRadius, GameConfig.PanelBorder);
            DrawRoundedTexture(GetIcon(character), iconRect, GameConfig.IconRadius, Color.White);
            DrawRoundedRectBorder(iconRect, GameConfig.IconRadius, ringColor, ringThickness);

            var nameColor = pickIndex >= 0 ? GameConfig.TextPrimary : GameConfig.TextSecondary;
            var nameSize = _smallLabelFont.MeasureString(character.Name);
            var nameX = cellRect.X + cellRect.Width / 2f - nameSize.X / 2f;
            _smallLabelFont.DrawText(_spriteBatch, character.Name, new Vector2(nameX, iconRect.Bottom + 5), nameColor);
        }

        var status = $"{_selectedCharacters.Count} / 2 体選択中";
        var statusSize = _smallLabelFont.MeasureString(status);
        _smallLabelFont.DrawText(_spriteBatch, status, new Vector2(GameConfig.WindowWidth / 2f - statusSize.X / 2f, _charNextButtonRect.Top - 24), GameConfig.TextSecondary);

        DrawActionButton(_charNextButtonRect, "つぎへ", _selectedCharacters.Count == 2);
    }

    private Rectangle GetMapCardRect(int index)
    {
        const int cardWidth = 460;
        const int cardHeight = 150;
        var x = (GameConfig.WindowWidth - cardWidth) / 2;
        var y = 210 + index * (cardHeight + 28);
        return new Rectangle(x, y, cardWidth, cardHeight);
    }

    private void DrawMapSelect()
    {
        DrawTitleWithAccent("マップを選択", GameConfig.AccentGold);

        for (var i = 0; i < _maps.Count; i++)
        {
            var map = _maps[i];
            var rect = GetMapCardRect(i);
            var selected = ReferenceEquals(_selectedMap, map);

            DrawShadow(rect, GameConfig.CardRadius);
            FillRoundedRect(rect, GameConfig.CardRadius, GameConfig.PanelBg);
            DrawRoundedRectBorder(rect, GameConfig.CardRadius, selected ? GameConfig.BallBlue : GameConfig.PanelBorder, selected ? 3 : 2);

            var nameSize = _labelFont.MeasureString(map.Name);
            _labelFont.DrawText(_spriteBatch, map.Name, new Vector2(rect.Center.X - nameSize.X / 2f, rect.Center.Y - nameSize.Y - 2), GameConfig.TextPrimary);

            var sub = $"{map.Size} × {map.Size}";
            var subSize = _tinyLabelFont.MeasureString(sub);
            _tinyLabelFont.DrawText(_spriteBatch, sub, new Vector2(rect.Center.X - subSize.X / 2f, rect.Center.Y + 6), GameConfig.TextSecondary);
        }

        DrawActionButton(_mapBackButtonRect, "もどる", true, primary: false);
        DrawActionButton(_mapNextButtonRect, "バトル開始", _selectedMap is not null);
    }

    private void DrawBattle()
    {
        var field = CurrentFieldRect();

        DrawTitle();
        DrawHpBar(_ballA, field.Left, 110, alignRight: false);
        DrawHpBar(_ballB, field.Left + field.Width - HpBarWidth, 110, alignRight: true);

        DrawShadow(field, GameConfig.FieldRadius);
        FillRoundedRect(field, GameConfig.FieldRadius, GameConfig.FieldBg);
        DrawRoundedRectBorder(field, GameConfig.FieldRadius, GameConfig.FieldBorder, 3);

        DrawBall(_ballA);
        DrawBall(_ballB);

        DrawActionButton(_backToSelectButtonRect, "もどる", true, primary: false, small: true);
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
        var pos = new Vector2(GameConfig.WindowWidth / 2f - size.X / 2f, 10);
        DrawTextWithShadow(_titleFont, title, pos, GameConfig.TextPrimary);
    }

    /// <summary>タイトルの下にアクセントカラーの短い下線を添えて画面の見出しにする。</summary>
    private void DrawTitleWithAccent(string title, Color accent)
    {
        var size = _titleFont.MeasureString(title);
        var pos = new Vector2(GameConfig.WindowWidth / 2f - size.X / 2f, 10);
        DrawTextWithShadow(_titleFont, title, pos, GameConfig.TextPrimary);

        const int barWidth = 56;
        const int barHeight = 4;
        var barRect = new Rectangle((int)(GameConfig.WindowWidth / 2f - barWidth / 2f), (int)(pos.Y + size.Y + 4), barWidth, barHeight);
        FillRoundedRect(barRect, barHeight / 2, accent);
    }

    private const int HpBarWidth = 320;
    private const int HpBarHeight = 28;
    private const int MiniBarHeight = 11;
    private const int MiniBarGap = 7;

    private void DrawHpBar(Ball ball, int x, int y, bool alignRight)
    {
        var teamColor = ball.Color;
        var label = $"{ball.Name}  HP {ball.Hp}/{ball.MaxHp}";
        var labelSize = _labelFont.MeasureString(label);
        var labelX = alignRight ? x + HpBarWidth - labelSize.X : x;
        _labelFont.DrawText(_spriteBatch, label, new Vector2(labelX, y - labelSize.Y - 4), GameConfig.TextPrimary);

        var barRect = new Rectangle(x, y, HpBarWidth, HpBarHeight);
        FillRoundedRect(barRect, GameConfig.BarRadius, GameConfig.HpBarBg);

        var ratio = ball.MaxHp > 0 ? (float)ball.Hp / ball.MaxHp : 0f;
        var fillWidth = (int)(HpBarWidth * ratio);
        if (fillWidth > 0)
        {
            var fillX = alignRight ? x + HpBarWidth - fillWidth : x;
            FillRoundedRect(new Rectangle(fillX, y, fillWidth, HpBarHeight), GameConfig.BarRadius, HpBarColor(ratio));
        }
        DrawRoundedRectBorder(barRect, GameConfig.BarRadius, new Color((int)teamColor.R, (int)teamColor.G, (int)teamColor.B, 130), 2);

        // スキルクールダウン(紫): クールダウンが明けて発動可能に近いほど満ちていく。発動可能/発動中は白枠で強調。
        var skillReady = ball.SkillCooldownTimer <= 0 || ball.IsDashing;
        var skillRatio = 1f - ball.SkillCooldownTimer / (float)GameConfig.DashCooldownFrames;
        var skillY = y + HpBarHeight + MiniBarGap;
        DrawMiniBar(x, skillY, alignRight, skillRatio, GameConfig.SkillGaugeColor, skillReady);

        // 必殺技ゲージ(金): 0〜100%。満タン(=発動中)は白枠で強調。
        var ultimateRatio = ball.UltimateGauge / (float)GameConfig.UltimateGaugeMax;
        var ultimateY = skillY + MiniBarHeight + MiniBarGap;
        DrawMiniBar(x, ultimateY, alignRight, ultimateRatio, GameConfig.UltimateGaugeColor, ball.IsUltimateActive);
    }

    private void DrawMiniBar(int x, int y, bool alignRight, float ratio, Color color, bool highlight)
    {
        ratio = MathHelper.Clamp(ratio, 0f, 1f);
        var rect = new Rectangle(x, y, HpBarWidth, MiniBarHeight);
        FillRoundedRect(rect, GameConfig.MiniBarRadius, GameConfig.HpBarBg);

        var fillWidth = (int)(HpBarWidth * ratio);
        if (fillWidth > 0)
        {
            var fillX = alignRight ? x + HpBarWidth - fillWidth : x;
            FillRoundedRect(new Rectangle(fillX, y, fillWidth, MiniBarHeight), GameConfig.MiniBarRadius, color);
        }

        if (highlight)
        {
            DrawRoundedRectBorder(rect, GameConfig.MiniBarRadius, GameConfig.TextPrimary, 2);
        }
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
        var burstColor = ball.IsUltimateActive ? GameConfig.UltimateGaugeColor : color;

        // ダッシュ/必殺技で突進中は、進行方向の逆側に残像(だんだん薄く小さくなる円)を残す
        if (ball.Alive && ball.BurstTimer > 0 && ball.Velocity.LengthSquared() > 0.0001f)
        {
            var dir = Vector2.Normalize(ball.Velocity);
            for (var i = 1; i <= 3; i++)
            {
                var trailPos = ball.Position - dir * (ball.Radius * 0.55f * i);
                var trailAlpha = 100 / (i + 1);
                var trailRadius = ball.Radius * (1f - i * 0.15f);
                FillCircle(trailPos, trailRadius, new Color((int)burstColor.R, (int)burstColor.G, (int)burstColor.B, trailAlpha));
            }
        }

        // 淡いグロー(一回り大きい半透明の円)でネオンっぽい浮遊感を出す。突進中はグローを強調。
        if (ball.Alive)
        {
            var glowScale = ball.BurstTimer > 0 ? 1.7f : 1.35f;
            var glowAlpha = ball.BurstTimer > 0 ? 90 : 45;
            FillCircle(ball.Position, ball.Radius * glowScale, new Color((int)burstColor.R, (int)burstColor.G, (int)burstColor.B, glowAlpha));
        }

        if (ball.Alive && ball.InvincibleTimer > 0 && (ball.InvincibleTimer / 3) % 2 == 0)
        {
            color = new Color(Math.Min(255, color.R + 70), Math.Min(255, color.G + 70), Math.Min(255, color.B + 70));
        }
        FillCircle(ball.Position, ball.Radius, color);
        var outlineColor = ball.IsStunned ? GameConfig.StunColor : GameConfig.TextPrimary;
        DrawCircleOutline(ball.Position, ball.Radius, outlineColor, ball.BurstTimer > 0 ? 3 : 2);

        if (ball.Icon is not null)
        {
            var iconSize = (int)(ball.Radius * 1.5f);
            var dest = new Rectangle((int)ball.Position.X - iconSize / 2, (int)ball.Position.Y - iconSize / 2, iconSize, iconSize);
            var tint = ball.Alive ? Color.White : new Color(190, 190, 190);
            _spriteBatch.Draw(ball.Icon, dest, tint);
        }

        // スタン中は頭上に3つの星をくるくる回して行動不能を表す
        if (ball.Alive && ball.IsStunned)
        {
            var angleBase = ball.StunTimer * 0.3f;
            var starCenter = ball.Position + new Vector2(0, -ball.Radius * 1.25f);
            for (var i = 0; i < 3; i++)
            {
                var angle = angleBase + i * MathHelper.TwoPi / 3f;
                var orbit = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle) * 0.5f) * (ball.Radius * 0.4f);
                FillCircle(starCenter + orbit, 4.5f, GameConfig.StunColor);
            }
        }
    }

    /// <summary>有効/無効を切り替えられる汎用の画面遷移ボタン。primary=falseで控えめな配色になる。</summary>
    private void DrawActionButton(Rectangle rect, string label, bool enabled, bool primary = true, bool small = false)
    {
        var mouse = Mouse.GetState();
        var hover = enabled && rect.Contains(mouse.Position);

        Color bg;
        Color border;
        Color text;
        if (!enabled)
        {
            bg = GameConfig.ButtonDisabledBg;
            border = GameConfig.PanelBorder;
            text = GameConfig.TextDisabled;
        }
        else if (primary)
        {
            bg = hover ? GameConfig.AccentPurple : GameConfig.ButtonBg;
            border = GameConfig.AccentPurple;
            text = GameConfig.TextPrimary;
        }
        else
        {
            bg = hover ? GameConfig.ButtonHoverBg : GameConfig.ButtonBg;
            border = GameConfig.PanelBorder;
            text = GameConfig.TextSecondary;
        }

        var radius = small ? GameConfig.ButtonRadius - 2 : GameConfig.ButtonRadius;
        DrawShadow(rect, radius);
        FillRoundedRect(rect, radius, bg);
        DrawRoundedRectBorder(rect, radius, border, 2);

        var font = small ? _smallLabelFont : _buttonFont;
        var size = font.MeasureString(label);
        var pos = new Vector2(rect.Center.X - size.X / 2f, rect.Center.Y - size.Y / 2f);
        font.DrawText(_spriteBatch, label, pos, text);
    }

    private void DrawResultOverlay(string text, Rectangle field)
    {
        var overlay = new Color(8, 9, 14, 150);
        FillRoundedRect(field, GameConfig.FieldRadius, overlay);

        var size = _resultFont.MeasureString(text);
        var cardWidth = size.X + 64;
        var cardHeight = size.Y + 36;
        var cardRect = new Rectangle(
            (int)(GameConfig.WindowWidth / 2f - cardWidth / 2f),
            (int)(field.Top + field.Height / 2f - cardHeight / 2f),
            (int)cardWidth,
            (int)cardHeight);

        var accent = text.Contains(_ballA.Name) ? _ballA.Color : text.Contains(_ballB.Name) ? _ballB.Color : GameConfig.AccentGold;

        DrawShadow(cardRect, GameConfig.CardRadius);
        FillRoundedRect(cardRect, GameConfig.CardRadius, GameConfig.PanelBg);
        DrawRoundedRectBorder(cardRect, GameConfig.CardRadius, accent, 3);

        var pos = new Vector2(cardRect.Center.X - size.X / 2f, cardRect.Center.Y - size.Y / 2f);
        DrawTextWithShadow(_resultFont, text, pos, GameConfig.TextPrimary);
    }

    /// <summary>キャラ選択グリッドの背景に敷く、角丸のパネル。</summary>
    private void DrawPanel(Rectangle rect)
    {
        DrawShadow(rect, GameConfig.CardRadius);
        FillRoundedRect(rect, GameConfig.CardRadius, GameConfig.PanelBg);
        DrawRoundedRectBorder(rect, GameConfig.CardRadius, GameConfig.PanelBorder, 2);
    }

    /// <summary>矩形の少し下にずらした半透明の角丸を敷いて、浮いているような影を出す。</summary>
    private void DrawShadow(Rectangle rect, int radius)
    {
        var shadowRect = new Rectangle(rect.X + 3, rect.Y + 5, rect.Width, rect.Height);
        FillRoundedRect(shadowRect, radius, GameConfig.ShadowColor);
    }

    private void DrawTextWithShadow(SpriteFontBase font, string text, Vector2 pos, Color color)
    {
        font.DrawText(_spriteBatch, text, pos + new Vector2(2, 2), new Color(0, 0, 0, 120));
        font.DrawText(_spriteBatch, text, pos, color);
    }

    private static Rectangle Inflate(Rectangle rect, int amount)
    {
        var r = rect;
        r.Inflate(amount, amount);
        return r;
    }

    private void FillRect(Rectangle rect, Color color) => _spriteBatch.Draw(_pixel, rect, color);

    /// <summary>角丸の矩形を塗りつぶす(中央の十字+四隅の1/4円)。</summary>
    private void FillRoundedRect(Rectangle rect, int radius, Color color)
    {
        radius = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        if (radius == 0)
        {
            FillRect(rect, color);
            return;
        }

        FillRect(new Rectangle(rect.X + radius, rect.Y, rect.Width - radius * 2, rect.Height), color);
        FillRect(new Rectangle(rect.X, rect.Y + radius, radius, rect.Height - radius * 2), color);
        FillRect(new Rectangle(rect.Right - radius, rect.Y + radius, radius, rect.Height - radius * 2), color);

        FillCircleQuadrant(new Vector2(rect.X + radius, rect.Y + radius), radius, color, -1, -1);
        FillCircleQuadrant(new Vector2(rect.Right - radius, rect.Y + radius), radius, color, 1, -1);
        FillCircleQuadrant(new Vector2(rect.X + radius, rect.Bottom - radius), radius, color, -1, 1);
        FillCircleQuadrant(new Vector2(rect.Right - radius, rect.Bottom - radius), radius, color, 1, 1);
    }

    /// <summary>角丸の矩形の輪郭線だけを描く(内側は上書きしないので、下地の絵柄を消さない)。</summary>
    private void DrawRoundedRectBorder(Rectangle rect, int radius, Color color, int thickness)
    {
        radius = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        thickness = Math.Max(1, thickness);

        FillRect(new Rectangle(rect.X + radius, rect.Y, rect.Width - radius * 2, thickness), color);
        FillRect(new Rectangle(rect.X + radius, rect.Bottom - thickness, rect.Width - radius * 2, thickness), color);
        FillRect(new Rectangle(rect.X, rect.Y + radius, thickness, rect.Height - radius * 2), color);
        FillRect(new Rectangle(rect.Right - thickness, rect.Y + radius, thickness, rect.Height - radius * 2), color);

        if (radius > 0)
        {
            DrawCircleQuadrantRing(new Vector2(rect.X + radius, rect.Y + radius), radius, thickness, color, -1, -1);
            DrawCircleQuadrantRing(new Vector2(rect.Right - radius, rect.Y + radius), radius, thickness, color, 1, -1);
            DrawCircleQuadrantRing(new Vector2(rect.X + radius, rect.Bottom - radius), radius, thickness, color, -1, 1);
            DrawCircleQuadrantRing(new Vector2(rect.Right - radius, rect.Bottom - radius), radius, thickness, color, 1, 1);
        }
    }

    /// <summary>円の1/4の輪(外径radius・内径radius-thickness)を塗る。角丸の枠線の四隅に使う。</summary>
    private void DrawCircleQuadrantRing(Vector2 center, int radius, int thickness, Color color, int signX, int signY)
    {
        var inner = Math.Max(0, radius - thickness);
        for (var dy = 0; dy <= radius; dy++)
        {
            var outerDx = (int)Math.Sqrt(Math.Max(0, radius * radius - dy * dy));
            var innerDx = dy <= inner ? (int)Math.Sqrt(Math.Max(0, inner * inner - dy * dy)) : 0;
            var width = outerDx - innerDx;
            if (width <= 0) continue;

            var x = signX > 0 ? (int)center.X + innerDx : (int)center.X - innerDx - width;
            var y = signY > 0 ? (int)center.Y + dy : (int)center.Y - dy;
            FillRect(new Rectangle(x, y, width, 1), color);
        }
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

    /// <summary>円の1/4だけを塗りつぶす(角丸矩形の四隅を描くのに使う)。signX/signYで象限を指定。</summary>
    private void FillCircleQuadrant(Vector2 center, int radius, Color color, int signX, int signY)
    {
        for (var dy = 0; dy <= radius; dy++)
        {
            var maxDx = (int)Math.Sqrt(Math.Max(0, radius * radius - dy * dy));
            if (maxDx == 0) continue;
            var x = signX > 0 ? (int)center.X : (int)center.X - maxDx;
            var y = signY > 0 ? (int)center.Y + dy : (int)center.Y - dy;
            FillRect(new Rectangle(x, y, maxDx, 1), color);
        }
    }

    /// <summary>テクスチャを角丸クリップ風に描く(四隅だけ背景色相当を上から乗せてマスクする簡易実装)。</summary>
    private void DrawRoundedTexture(Texture2D texture, Rectangle rect, int radius, Color tint)
    {
        _spriteBatch.Draw(texture, rect, tint);
        radius = Math.Max(0, Math.Min(radius, Math.Min(rect.Width, rect.Height) / 2));
        if (radius == 0) return;

        var maskColor = GameConfig.PanelBg;
        MaskCircleQuadrant(new Vector2(rect.X + radius, rect.Y + radius), radius, maskColor, -1, -1);
        MaskCircleQuadrant(new Vector2(rect.Right - radius, rect.Y + radius), radius, maskColor, 1, -1);
        MaskCircleQuadrant(new Vector2(rect.X + radius, rect.Bottom - radius), radius, maskColor, -1, 1);
        MaskCircleQuadrant(new Vector2(rect.Right - radius, rect.Bottom - radius), radius, maskColor, 1, 1);
    }

    /// <summary>角の外側(円の外)だけをmaskColorで塗って四角い角を丸く見せる。</summary>
    private void MaskCircleQuadrant(Vector2 cornerCenter, int radius, Color maskColor, int signX, int signY)
    {
        for (var dy = 0; dy <= radius; dy++)
        {
            var circleDx = (int)Math.Sqrt(Math.Max(0, radius * radius - dy * dy));
            var outside = radius - circleDx;
            if (outside <= 0) continue;
            var x = signX > 0 ? (int)cornerCenter.X + circleDx : (int)cornerCenter.X - radius;
            var y = signY > 0 ? (int)cornerCenter.Y + dy : (int)cornerCenter.Y - dy;
            FillRect(new Rectangle(x, y, outside, 1), maskColor);
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
