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

    // ---- 角丸の半径(統一感のためUIパーツ種別ごとにまとめる) ----
    public const int CardRadius = 16;
    public const int ButtonRadius = 10;
    public const int IconRadius = 12;
    public const int FieldRadius = 18;
    public const int BarRadius = 8;

    // ---- 色(ダークテーマ) ----
    public static readonly Color WindowBg = new(17, 19, 28);
    public static readonly Color PanelBg = new(28, 31, 44);
    public static readonly Color PanelBorder = new(46, 51, 71);

    public static readonly Color FieldBg = new(24, 27, 38);
    public static readonly Color FieldBorder = new(52, 58, 80);

    public static readonly Color BallRed = new(255, 92, 114);
    public static readonly Color BallBlue = new(79, 195, 255);
    public static readonly Color DeadGray = new(90, 95, 110);

    public static readonly Color HpBarBg = new(38, 42, 56);
    public static readonly Color HpGreen = new(61, 220, 132);
    public static readonly Color HpYellow = new(255, 209, 102);
    public static readonly Color HpRed = new(255, 92, 114);

    public static readonly Color ButtonBg = new(44, 48, 64);
    public static readonly Color ButtonHoverBg = new(60, 66, 88);
    public static readonly Color ButtonDisabledBg = new(30, 32, 42);

    public static readonly Color AccentGold = new(255, 209, 102);
    public static readonly Color AccentPurple = new(150, 130, 255);

    public static readonly Color TextPrimary = new(240, 242, 250);
    public static readonly Color TextSecondary = new(150, 158, 180);
    public static readonly Color TextDisabled = new(100, 106, 126);
    public static readonly Color TextDark = new(20, 20, 20); // 明るい背景(通常攻撃オーバーレイ等)用
    public static readonly Color ShadowColor = new(0, 0, 0, 90);
}
