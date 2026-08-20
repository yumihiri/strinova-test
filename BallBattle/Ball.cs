using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace BallBattle;

/// <summary>対戦キャラクター(玉)。ランダム移動・体当たり衝突・ダッシュ突進スキル・必殺技を扱う。</summary>
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

    // ---- スキル(ダッシュ突進・クールダウン制) ----
    public int SkillCooldownTimer;

    // ---- 必殺技(時間経過制ゲージ) ----
    public float UltimateGauge;

    // ---- ダッシュ/必殺技共通の「突進状態」 ----
    public int BurstTimer;
    public bool IsUltimateBurst;
    private float _speedMultiplier = 1f;

    // ---- スタン(行動不能) ----
    public int StunTimer;

    public bool IsDashing => BurstTimer > 0 && !IsUltimateBurst;
    public bool IsUltimateActive => BurstTimer > 0 && IsUltimateBurst;
    public bool IsStunned => StunTimer > 0;
    public int CurrentBurstBonusDamage => BurstTimer <= 0 ? 0 : IsUltimateBurst ? GameConfig.UltimateBonusDamage : GameConfig.DashBonusDamage;
    public int CurrentBurstStunFrames => BurstTimer <= 0 ? 0 : IsUltimateBurst ? GameConfig.UltimateStunFrames : GameConfig.DashStunFrames;

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
        SkillCooldownTimer = GameConfig.DashCooldownFrames;
        PickNewDirection();
    }

    /// <summary>characters.jsonから読み込んだキャラクターデータで玉を生成する。</summary>
    public Ball(Vector2 position, Color color, CharacterData character, Texture2D? icon, Random random)
        : this(position, color, character.Name, random, character.Hp, character.Speed)
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

        Velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * Speed * _speedMultiplier;
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

        // スタン中は行動不能。クールダウンやゲージの経過も止まる。
        if (StunTimer > 0)
        {
            StunTimer -= 1;
            return;
        }

        UpdateBurst();
        UpdateSkillCooldown();
        UpdateUltimateGauge();

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

    private void UpdateBurst()
    {
        if (BurstTimer <= 0) return;

        BurstTimer -= 1;
        if (BurstTimer == 0)
        {
            EndBurst();
        }
    }

    /// <summary>ダッシュ/必殺技の突進状態を終える。速度の向きは保ったまま通常速度に戻す。</summary>
    private void EndBurst()
    {
        IsUltimateBurst = false;
        _speedMultiplier = 1f;
        if (Velocity.LengthSquared() > 0.0001f)
        {
            Velocity = Vector2.Normalize(Velocity) * Speed;
        }
    }

    private void UpdateSkillCooldown()
    {
        if (BurstTimer > 0) return; // 突進中はクールダウン進行・新規発動しない

        if (SkillCooldownTimer > 0)
        {
            SkillCooldownTimer -= 1;
            return;
        }

        TriggerDash();
    }

    /// <summary>クールダウンが明けたタイミングで自動発動するダッシュ突進。</summary>
    private void TriggerDash()
    {
        IsUltimateBurst = false;
        BurstTimer = GameConfig.DashDurationFrames;
        _speedMultiplier = GameConfig.DashSpeedMultiplier;
        if (Velocity.LengthSquared() > 0.0001f)
        {
            Velocity = Vector2.Normalize(Velocity) * Speed * _speedMultiplier;
        }
        SkillCooldownTimer = GameConfig.DashCooldownFrames;
    }

    /// <summary>ゲージが100%に達したタイミングで自動発動する必殺技。</summary>
    private void TriggerUltimate()
    {
        IsUltimateBurst = true;
        BurstTimer = GameConfig.UltimateDurationFrames;
        _speedMultiplier = GameConfig.UltimateSpeedMultiplier;
        if (Velocity.LengthSquared() > 0.0001f)
        {
            Velocity = Vector2.Normalize(Velocity) * Speed * _speedMultiplier;
        }
        UltimateGauge = 0;
    }

    /// <summary>
    /// 必殺技ゲージは時間経過(UltimateChargeFrames)で満タンになり、満タンで自動発動する。
    /// キャラごとの強い/弱いはチャージ時間の差で表現する想定(現時点では全キャラ共通値)。
    /// </summary>
    private void UpdateUltimateGauge()
    {
        UltimateGauge = Math.Min(GameConfig.UltimateGaugeMax, UltimateGauge + GameConfig.UltimateGaugeMax / (float)GameConfig.UltimateChargeFrames);
        if (UltimateGauge >= GameConfig.UltimateGaugeMax)
        {
            TriggerUltimate();
        }
    }

    /// <summary>ダッシュ/必殺技を相手に当てた際、その突進状態を即座に終える(命中で突進が終わる)。</summary>
    public void EndBurstOnHit()
    {
        if (BurstTimer > 0)
        {
            BurstTimer = 0;
            EndBurst();
        }
    }

    public void ApplyStun(int frames)
    {
        if (!Alive || frames <= 0) return;
        StunTimer = Math.Max(StunTimer, frames);
        // スタンされたら突進状態は強制解除
        if (BurstTimer > 0)
        {
            BurstTimer = 0;
            IsUltimateBurst = false;
            _speedMultiplier = 1f;
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
