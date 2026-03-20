using UnityEngine;

namespace Mob
{
    /// <summary>
    /// スプライトアニメーションと歩行方向を管理する。
    /// Animator パラメータ:
    ///   - DirX  (Float) : 水平方向 (-1, 0, 1)
    ///   - DirY  (Float) : 垂直方向 (-1, 0, 1)
    ///   - IsWalking (Bool)
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class MobAnimator : MonoBehaviour
    {
        // Animator パラメータ名（Animator Controller と一致させること）
        private static readonly int ParamDirX      = Animator.StringToHash("DirX");
        private static readonly int ParamDirY      = Animator.StringToHash("DirY");
        private static readonly int ParamIsWalking = Animator.StringToHash("IsWalking");

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        /// <summary>歩行アニメを再生し、方向を設定する。</summary>
        /// <param name="direction">4 方向グリッド単位の移動ベクトル（例: Vector2Int.left）</param>
        public void PlayWalk(Vector2Int direction)
        {
            SetDirection(direction);
            _animator.SetBool(ParamIsWalking, true);
        }

        /// <summary>待機アニメに切り替える（方向は維持）。</summary>
        public void PlayIdle()
        {
            _animator.SetBool(ParamIsWalking, false);
        }

        // ---- Private ----

        /// <summary>Animator の DirX / DirY を更新する。</summary>
        private void SetDirection(Vector2Int dir)
        {
            _animator.SetFloat(ParamDirX, dir.x);
            _animator.SetFloat(ParamDirY, dir.y);
        }
    }
}
