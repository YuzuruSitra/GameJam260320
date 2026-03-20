using UnityEngine;

namespace Mob
{
    /// <summary>
    /// モード2: プレイヤーをリアルタイムで追従する。
    /// 移動方向を毎フレーム計算し、MobAnimator へ通知する。
    /// </summary>
    public class ChaseBehavior : MonoBehaviour, IMobBehavior
    {
        [Header("Chase Settings")]
        [Tooltip("追従対象（未設定時は 'Player' タグから自動取得）")]
        [SerializeField] private Transform target;

        [Tooltip("移動速度（Units/秒）")]
        [SerializeField] private float moveSpeed = 3.0f;

        [Tooltip("この距離以内に入ったら停止")]
        [SerializeField] private float stopDistance = 0.5f;

        private MobAnimator _mobAnimator;

        // 前フレームのアニメ方向（変化時のみ更新）
        private Vector2Int _lastAnimDir = Vector2Int.down;

        private void Awake()
        {
            _mobAnimator = GetComponent<MobAnimator>();
        }

        // ========== IMobBehavior ==========

        public void Execute()
        {
            if (target == null) return;

            Vector2 toTarget = (Vector2)target.position - (Vector2)transform.position;
            float distance   = toTarget.magnitude;

            if (distance <= stopDistance)
            {
                _mobAnimator?.PlayIdle();
                return;
            }

            // 移動
            Vector2 dir = toTarget.normalized;
            transform.position += (Vector3)(dir * (moveSpeed * Time.deltaTime));

            // アニメ方向: 4 方向に量子化して変化した時のみ更新
            Vector2Int animDir = QuantizeDirection(dir);
            if (animDir != _lastAnimDir)
            {
                _mobAnimator?.PlayWalk(animDir);
                _lastAnimDir = animDir;
            }
        }

        public void OnModeChanged()
        {
            // ターゲットを自動取得
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
                else
                    Debug.LogWarning("[ChaseBehavior] Player タグのオブジェクトが見つかりません。");
            }

            _lastAnimDir = Vector2Int.down;
            _mobAnimator?.PlayIdle();
        }

        // ========== Helpers ==========

        /// <summary>連続ベクトルを上下左右の 4 方向に量子化する。</summary>
        private static Vector2Int QuantizeDirection(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                return dir.x >= 0 ? Vector2Int.right : Vector2Int.left;
            return dir.y >= 0 ? Vector2Int.up : Vector2Int.down;
        }
    }
}
