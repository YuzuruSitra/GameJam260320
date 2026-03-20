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

		/// <summary>このモードに切り替わった直後に呼ばれる初期化処理。</summary>
		void OnEnter();

		/// <summary>このモードから離れる直前に呼ばれる終了処理。</summary>
		void OnExit();
	}
}