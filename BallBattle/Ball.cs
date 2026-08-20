using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BallBattle;

/// <summary>対戦キャラクター(玉)。ランダム移動と体当たり衝突のみを扱う。</summary>
public class Ball
{
    public Vector2 Position;
    public Vector2 Velocity;
    public Color Color;
    public string Name;
    public CharacterData? Character;
    public Texture2D? Icon;
    public float Radius = GameConfig.BallRadius;
    public int MaxHp = GameConfig.InitialHp;
    public int Hp = GameConfig.InitialHp;
    public float Speed = GameConfig.BaseSpeed;
    public int InvincibleTimer;
    public bool Alive = true;

    private readonly Random _random;

    public Ball(Vector2 position, Color color, string name, Random random, int maxHp = GameConfig.InitialHp, float speed = GameConfig.BaseSpeed)
    {
        Position = position;
        Color = color;
        Name = name;
        _random = random;
        MaxHp = maxHp;
        Hp = maxHp;
        Speed = speed;
        PickNewDirection();
    }

    /// <summary>
    /// characters.jsonから読み込んだキャラクターデータで玉を生成する。
    /// 画面上の表示名は、現状のPixelFontが日本語グリフを持たないため、
    /// キャラID(ローマ字)を大文字化したものを使う。本来のキャラ名は Character.Name から参照できる。
    /// </summary>
    public Ball(Vector2 position, Color color, CharacterData character, Texture2D? icon, Random random)
        : this(position, color, character.Id.ToUpperInvariant(), random, character.Hp, character.Speed)
    {
        Character = character;
        Icon = icon;
    }

    /// <summary>
    /// 新しいランダムな方向を選ぶ。壁に当たった直後に呼ぶ場合は、
    /// その壁から再び外に出ようとする向きを選ばないよう、フィールド内側を向く角度のみを候補にする。
    /// </summary>
    private void PickNewDirection(bool awayFromLeft = false, bool awayFromRight = false, bool awayFromTop = false, bool awayFromBottom = false)
    {
        float angle;
        var attempts = 0;
        do
        {
            angle = (float)(_random.NextDouble() * Math.PI * 2);
            attempts++;
        } while (attempts < 30 && !IsDirectionValid(angle, awayFromLeft, awayFromRight, awayFromTop, awayFromBottom));

        Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Speed;
    }

    private static bool IsDirectionValid(float angle, bool awayFromLeft, bool awayFromRight, bool awayFromTop, bool awayFromBottom)
    {
        var dx = (float)Math.Cos(angle);
        var dy = (float)Math.Sin(angle);
        if (awayFromLeft && dx <= 0) return false;
        if (awayFromRight && dx >= 0) return false;
        if (awayFromTop && dy <= 0) return false;
        if (awayFromBottom && dy >= 0) return false;
        return true;
    }

    public void Update(Rectangle field)
    {
        if (!Alive) return;

        if (InvincibleTimer > 0)
        {
            InvincibleTimer -= 1;
        }

        Position += Velocity;

        var minX = field.Left + Radius;
        var maxX = field.Right - Radius;
        var minY = field.Top + Radius;
        var maxY = field.Bottom - Radius;

        var hitLeft = false;
        var hitRight = false;
        var hitTop = false;
        var hitBottom = false;

        if (Position.X < minX)
        {
            Position.X = minX;
            hitLeft = true;
        }
        else if (Position.X > maxX)
        {
            Position.X = maxX;
            hitRight = true;
        }

        if (Position.Y < minY)
        {
            Position.Y = minY;
            hitTop = true;
        }
        else if (Position.Y > maxY)
        {
            Position.Y = maxY;
            hitBottom = true;
        }

        // 壁に当たるまで直進し、当たったら新しいランダムな方向へ進み直す
        if (hitLeft || hitRight || hitTop || hitBottom)
        {
            PickNewDirection(hitLeft, hitRight, hitTop, hitBottom);
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
    }
}
