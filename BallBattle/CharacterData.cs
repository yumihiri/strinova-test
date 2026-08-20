namespace BallBattle;

/// <summary>
/// characters.json 1件分のキャラクターマスタデータ。
/// HP・Speedは現時点ではバランス未調整のため全キャラ共通値。今後の段階で個別調整する。
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
