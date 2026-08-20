namespace BallBattle;

/// <summary>
/// maps.json 1件分のマップ(フィールド)マスタデータ。
/// 現時点では正方形フィールド1種類のみだが、後からサイズ違い・形状違いを
/// 追加しやすいようキャラクターと同様にJSONから読み込む構造にしている。
/// </summary>
public class MapData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Size { get; set; } = GameConfig.FieldSize;
}
