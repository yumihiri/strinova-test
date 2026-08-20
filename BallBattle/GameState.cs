namespace BallBattle;

/// <summary>画面遷移: キャラ選択 → マップ選択 → バトル(→結果表示はバトル画面内のオーバーレイ)。</summary>
public enum GameState
{
    CharacterSelect,
    MapSelect,
    Battle,
}
