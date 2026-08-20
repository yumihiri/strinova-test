using Microsoft.Xna.Framework;

namespace BallBattle;

/// <summary>画面・フィールド・玉のパラメータを一箇所にまとめた定数集。</summary>
public static class GameConfig
{
    // ---- 画面・フィールド設定 ----
    // Web公開を見据えてウィンドウ/フィールドを一回り大きくした(旧600→760、約1.267倍)。
    // BallRadius/BaseSpeed/SpeedDamageFactorも同じ倍率で追従させ、旧サイズで検証済みの
    // 衝突頻度・試合時間の統計的な傾向(フレーム基準)がそのまま保たれるようにしている。
    public const int WindowWidth = 900;
    public const int WindowHeight = 1060;
    public const int FieldSize = 760;
    public const int FieldLeft = (WindowWidth - FieldSize) / 2;
    public const int FieldTop = 190;

    // ---- 玉のパラメータ ----
    // BallRadius/BaseDamage/SpeedDamageFactorは、ヘッドレスシミュレーション(1000試合)で
    // 「衝突間隔 約9〜10秒」「試合時間 平均49秒/中央値46秒/90%タイルで77秒」になるよう調整した値
    // (フィールド拡大前の基準値。拡大後も同じ統計になるよう上記の倍率で追従させてある)。
    public const float BallRadius = 48f;
    public const int InitialHp = 100;
    public const float BaseSpeed = 3.8f;
    public const int InvincibleFrames = 30; // 連続ヒット防止の無敵時間
    public const float KnockbackSpeed = 9f;
    public const int BaseDamage = 9;
    public const float SpeedDamageFactor = 2.37f;

    // ---- スキル(クールダウン制・ダッシュ突進) ----
    // ヘッドレスシミュレーション(1000試合)で、通常衝突・スキル・必殺技のバランスを
    // 「1試合あたり平均約36秒(中央値約34秒)、必殺技の命中率は約5割」になるよう調整した値。
    // UltimateDurationFramesは、短すぎると自然な衝突間隔(約9秒)に対してほぼ命中しないことが
    // シミュレーションで判明したため、意図的に長め(2秒)にしてある。
    public const int DashCooldownFrames = 300; // 5秒
    public const float DashSpeedMultiplier = 2.5f;
    public const int DashDurationFrames = 20;
    public const int DashBonusDamage = 8;
    public const int DashStunFrames = 18;

    // ---- 必殺技(ゲージ制) ----
    public const int UltimateGaugeMax = 100;
    public const float UltimateSpeedMultiplier = 3.0f;
    public const int UltimateDurationFrames = 120; // 2秒
    public const int UltimateBonusDamage = 16;
    public const int UltimateStunFrames = 36;

    public static Rectangle FieldRect => new(FieldLeft, FieldTop, FieldSize, FieldSize);

    // ---- 角丸の半径(統一感のためUIパーツ種別ごとにまとめる) ----
    public const int CardRadius = 20;
    public const int ButtonRadius = 13;
    public const int IconRadius = 15;
    public const int FieldRadius = 22;
    public const int BarRadius = 12;
    public const int MiniBarRadius = 5;

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
    public static readonly Color SkillGaugeColor = new(150, 130, 255);
    public static readonly Color UltimateGaugeColor = new(255, 209, 102);
    public static readonly Color StunColor = new(255, 230, 120);

    public static readonly Color TextPrimary = new(240, 242, 250);
    public static readonly Color TextSecondary = new(150, 158, 180);
    public static readonly Color TextDisabled = new(100, 106, 126);
    public static readonly Color TextDark = new(20, 20, 20); // 明るい背景(通常攻撃オーバーレイ等)用
    public static readonly Color ShadowColor = new(0, 0, 0, 90);
}
