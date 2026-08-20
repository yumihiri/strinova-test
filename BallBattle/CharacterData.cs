namespace BallBattle;

/// <summary>
/// characters.json 1件分のキャラクターマスタデータ。
/// HP・Speedは現時点ではバランス未調整のため全キャラ共通値。今後個別に調整する際は、
/// 1つのステータスだけが飛び抜けて強いキャラを作らず(例:HPが高いキャラは移動速度を遅くする)、
/// ステータス間にトレードオフを持たせること。
/// </summary>
public class CharacterData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Faction { get; set; } = "";
    public string Weapon { get; set; } = "";
    public int Hp { get; set; } = GameConfig.InitialHp;
    public float Speed { get; set; } = GameConfig.BaseSpeed;
    public string Icon { get; set; } = "";
}
