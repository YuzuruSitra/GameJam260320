namespace Mob
{
    /// <summary>
    /// Mob の行動を定義するインターフェース。
    /// PatrolBehavior / ChaseBehavior が実装する。
    /// </summary>
    public interface IMobBehavior
    {
        /// <summary>毎フレーム呼ばれる行動ロジック。</summary>
        void Execute();

        /// <summary>モード切替時に呼ばれる初期化処理。</summary>
        void OnModeChanged();
    }
}
