using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace BallBattle;

/// <summary>
/// 玉対戦ゲーム - 土台 + 基本UI (要件定義書セクション10の1・2)。
/// 正方形フィールド内で2つの玉がランダム移動し、接触するとダメージを与え合う。
/// HPが0になった方が負け。CPU対CPUの観戦専用で、プレイヤーはリセットボタンのみ操作できる。
/// </summary>
public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Texture2D _pixel = null!;

    private readonly Random _random = new();
    private Ball _ballA = null!;
    private Ball _ballB = null!;
    private string? _resultText;

    private Rectangle _buttonRect;
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
        _buttonRect = new Rectangle(GameConfig.WindowWidth / 2 - 80, GameConfig.FieldTop + GameConfig.FieldSize + 16, 160, 44);
        ResetBalls();
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    private void ResetBalls()
    {
        var margin = GameConfig.FieldSize / 4;
        _ballA = new Ball(new Vector2(GameConfig.FieldLeft + margin, GameConfig.FieldTop + margin), GameConfig.BallRed, "RED", _random);
        _ballB = new Ball(new Vector2(GameConfig.FieldLeft + GameConfig.FieldSize - margin, GameConfig.FieldTop + GameConfig.FieldSize - margin), GameConfig.BallBlue, "BLUE", _random);
        _resultText = null;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        var mouse = Mouse.GetState();
        var mouseDown = mouse.LeftButton == ButtonState.Pressed;
        if (mouseDown && !_prevMouseDown && _buttonRect.Contains(mouse.Position))
        {
            ResetBalls();
        }
        _prevMouseDown = mouseDown;

        if (_resultText is null)
        {
            var field = GameConfig.FieldRect;
            _ballA.Update(field);
            _ballB.Update(field);
            ResolveCollision(_ballA, _ballB, field);
            _resultText = GetResultText(_ballA, _ballB);
        }

        base.Update(gameTime);
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
        if (!a.Alive && !b.Alive) return "DRAW!";
        if (!a.Alive) return $"{b.Name} WINS!";
        if (!b.Alive) return $"{a.Name} WINS!";
        return null;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(GameConfig.WindowBg);

        _spriteBatch.Begin();

        DrawTitle();
        DrawHpBar(_ballA, GameConfig.FieldLeft, 66, alignRight: false);
        DrawHpBar(_ballB, GameConfig.FieldLeft + GameConfig.FieldSize - 240, 66, alignRight: true);

        FillRect(GameConfig.FieldRect, GameConfig.FieldBg);
        DrawRectBorder(GameConfig.FieldRect, GameConfig.FieldBorder, 3);

        DrawBall(_ballA);
        DrawBall(_ballB);

        DrawButton();

        if (_resultText is not null)
        {
            DrawResultOverlay(_resultText);
        }

        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void DrawTitle()
    {
        const string title = "BALL BATTLE";
        var width = PixelFont.MeasureWidth(title, 4);
        PixelFont.DrawText(_spriteBatch, _pixel, title, new Vector2(GameConfig.WindowWidth / 2f - width / 2f, 16), 4, GameConfig.TextDark);
    }

    private void DrawHpBar(Ball ball, int x, int y, bool alignRight)
    {
        const int width = 240;
        const int height = 22;

        var label = $"{ball.Name} HP {ball.Hp}/{ball.MaxHp}";
        var labelWidth = PixelFont.MeasureWidth(label, 2);
        var labelX = alignRight ? x + width - labelWidth : x;
        PixelFont.DrawText(_spriteBatch, _pixel, label, new Vector2(labelX, y - 20), 2, GameConfig.TextDark);

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
    }

    private void DrawButton()
    {
        var mouse = Mouse.GetState();
        var hover = _buttonRect.Contains(mouse.Position);
        FillRect(_buttonRect, hover ? GameConfig.ButtonHoverBg : GameConfig.ButtonBg);
        DrawRectBorder(_buttonRect, GameConfig.TextDark, 2);

        const string label = "RESET";
        var labelWidth = PixelFont.MeasureWidth(label, 2);
        var labelHeight = PixelFont.Height(2);
        var pos = new Vector2(
            _buttonRect.Center.X - labelWidth / 2f,
            _buttonRect.Center.Y - labelHeight / 2f);
        PixelFont.DrawText(_spriteBatch, _pixel, label, pos, 2, Color.White);
    }

    private void DrawResultOverlay(string text)
    {
        var overlay = new Color(0, 0, 0, 120);
        FillRect(GameConfig.FieldRect, overlay);

        var width = PixelFont.MeasureWidth(text, 6);
        var height = PixelFont.Height(6);
        var pos = new Vector2(
            GameConfig.WindowWidth / 2f - width / 2f,
            GameConfig.FieldTop + GameConfig.FieldSize / 2f - height / 2f);
        PixelFont.DrawText(_spriteBatch, _pixel, text, pos, 6, Color.White);
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
