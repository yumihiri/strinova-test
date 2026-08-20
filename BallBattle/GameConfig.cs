using Microsoft.Xna.Framework;

namespace BallBattle;

/// <summary>画面・フィールド・玉のパラメータを一箇所にまとめた定数集。</summary>
public static class GameConfig
{
    // ---- 画面・フィールド設定 ----
    public const int WindowWidth = 640;
    public const int WindowHeight = 760;
    public const int FieldSize = 600;
    public const int FieldLeft = (WindowWidth - FieldSize) / 2;
    public const int FieldTop = 100;

    // ---- 玉のパラメータ ----
    // BallRadius/BaseDamage/SpeedDamageFactorは、ヘッドレスシミュレーション(1000試合)で
    // 「衝突間隔 約9〜10秒」「試合時間 平均49秒/中央値46秒/90%タイルで77秒」になるよう調整した値。
    public const float BallRadius = 38f;
    public const int InitialHp = 100;
    public const float BaseSpeed = 3f;
    public const int InvincibleFrames = 30; // 連続ヒット防止の無敵時間
    public const float KnockbackSpeed = 7f;
    public const int BaseDamage = 9;
    public const float SpeedDamageFactor = 3.0f;

    public static Rectangle FieldRect => new(FieldLeft, FieldTop, FieldSize, FieldSize);

    // ---- 色 ----
    public static readonly Color WindowBg = new(245, 245, 245);
    public static readonly Color FieldBg = new(235, 235, 225);
    public static readonly Color FieldBorder = new(60, 60, 60);
    public static readonly Color BallRed = new(220, 70, 70);
    public static readonly Color BallBlue = new(70, 120, 220);
    public static readonly Color DeadGray = new(120, 120, 120);
    public static readonly Color HpBarBg = new(60, 60, 60);
    public static readonly Color HpGreen = new(80, 190, 90);
    public static readonly Color HpYellow = new(230, 200, 60);
    public static readonly Color HpRed = new(220, 70, 70);
    public static readonly Color ButtonBg = new(90, 90, 100);
    public static readonly Color ButtonHoverBg = new(120, 120, 135);
    public static readonly Color TextDark = new(20, 20, 20);
}
