using System;
using Microsoft.Xna.Framework;

namespace BallBattle;

/// <summary>対戦キャラクター(玉)。ランダム移動と体当たり衝突のみを扱う。</summary>
public class Ball
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public string Name;
    public float Radius = GameConfig.BallRadius;
    public int MaxHp = GameConfig.InitialHp;
    public int Hp = GameConfig.InitialHp;
    public float Speed = GameConfig.BaseSpeed;
    public int DirectionTimer;
    public int InvincibleTimer;
    public bool Alive = true;

    private readonly Random _random;

    public Ball(Vector2 position, Color color, string name, Random random)
    {
        Position = position;
        Color = color;
        Name = name;
        _random = random;
        PickNewDirection();
    }

    private void PickNewDirection(int? minFrames = null, int? maxFrames = null)
    {
        var angle = (float)(_random.NextDouble() * Math.PI * 2);
        Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Speed;
        var lo = minFrames ?? GameConfig.DirectionChangeMinFrames;
        var hi = maxFrames ?? GameConfig.DirectionChangeMaxFrames;
        DirectionTimer = _random.Next(lo, hi + 1);
    }

    public void Update(Rectangle field)
    {
        if (!Alive) return;

        DirectionTimer -= 1;
        if (DirectionTimer <= 0)
        {
            PickNewDirection();
        }

        if (InvincibleTimer > 0)
        {
            InvincibleTimer -= 1;
        }

        Position += Velocity;

        var minX = field.Left + Radius;
        var maxX = field.Right - Radius;
        var minY = field.Top + Radius;
        var maxY = field.Bottom - Radius;

        if (Position.X < minX)
        {
            Position.X = minX;
            Velocity.X = Math.Abs(Velocity.X);
        }
        else if (Position.X > maxX)
        {
            Position.X = maxX;
            Velocity.X = -Math.Abs(Velocity.X);
        }

        if (Position.Y < minY)
        {
            Position.Y = minY;
            Velocity.Y = Math.Abs(Velocity.Y);
        }
        else if (Position.Y > maxY)
        {
            Position.Y = maxY;
            Velocity.Y = -Math.Abs(Velocity.Y);
        }
    }

    public void TakeDamage(int amount)
    {
        if (!Alive) return;
        Hp -= amount;
        if (Hp <= 0)
        {
            Hp = 0;
            Alive = false;
        }
    }

    public void ClampToField(Rectangle field)
    {
        var minX = field.Left + Radius;
        var maxX = field.Right - Radius;
        var minY = field.Top + Radius;
        var maxY = field.Bottom - Radius;
        Position.X = Math.Clamp(Position.X, minX, maxX);
        Position.Y = Math.Clamp(Position.Y, minY, maxY);
    }

    public void ApplyKnockback(Vector2 direction)
    {
        Velocity = direction * GameConfig.KnockbackSpeed;
        DirectionTimer = GameConfig.KnockbackDirectionLockFrames;
    }
}
