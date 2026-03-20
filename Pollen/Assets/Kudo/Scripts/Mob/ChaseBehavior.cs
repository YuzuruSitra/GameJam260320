using UnityEngine;

namespace Mob
{
    /// <summary>
    /// モード2: MobRegistry に登録された順番で前の Mob（または Player）を追従する。
    ///
    /// ---- 追従ルール ----
    ///   自分の登録番号 = 0  → Player を直接追従
    ///   自分の登録番号 = N  → 番号 N-1 の Mob を追従
    /// </summary>
    public class ChaseBehavior : MonoBehaviour, IMobBehavior
    {
        [Header("Chase Settings")]
        [Tooltip("移動速度（Units/秒）")]
        [SerializeField] private float moveSpeed = 3.0f;

        [Tooltip("この距離以内に入ったら停止")]
        [SerializeField] private float stopDistance = 0.5f;

        [Tooltip("追従ターゲットを再取得する間隔（秒）。Unregister 後の繰り上がりに対応")]
        [SerializeField] private float targetUpdateInterval = 0.3f;

        // ---- 内部状態 ----
        private MobController _mobController;
        private MobAnimator   _mobAnimator;
        private Vector2Int    _lastAnimDir = Vector2Int.down;

        private int       _myIndex      = -1;
        private Transform _followTarget;
        private float     _targetTimer;

        // ========== Unity ==========

        private void Awake()
        {
            _mobController = GetComponent<MobController>();
            _mobAnimator   = GetComponent<MobAnimator>();
        }

        // ========== IMobBehavior ==========

        public void Execute()
        {
            // 一定間隔でターゲットを再取得（Unregister 後の繰り上がり対応）
            _targetTimer -= Time.deltaTime;
            if (_targetTimer <= 0f)
            {
                RefreshTarget();
                _targetTimer = targetUpdateInterval;
            }

            if (_followTarget == null) return;

            Vector2 toTarget = (Vector2)_followTarget.position - (Vector2)transform.position;
            float distance   = toTarget.magnitude;

            if (distance <= stopDistance)
            {
                _mobAnimator?.PlayIdle();
                return;
            }

            Vector2 dir = toTarget.normalized;
            transform.position += (Vector3)(dir * (moveSpeed * Time.deltaTime));

            Vector2Int animDir = QuantizeDirection(dir);
            if (animDir != _lastAnimDir)
            {
                _mobAnimator?.PlayWalk(animDir);
                _lastAnimDir = animDir;
            }
        }

        public void OnEnter()
        {
            // Registry に登録して番号を取得
            if (MobRegistry.Instance != null)
                _myIndex = MobRegistry.Instance.Register(_mobController);
            else
                Debug.LogWarning("[ChaseBehavior] MobRegistry が見つかりません。");

            _lastAnimDir = Vector2Int.down;
            _targetTimer = 0f;   // 即時ターゲット取得
            _mobAnimator?.PlayIdle();
        }

        public void OnExit()
        {
            _mobAnimator?.PlayIdle();
            _followTarget = null;
        }

        public void OnModeChanged() => OnEnter();

        // ========== Private ==========

        /// <summary>Registry から現在のインデックスを再取得してターゲットを更新する。</summary>
        private void RefreshTarget()
        {
            if (MobRegistry.Instance == null) return;

            // Unregister が起きてインデックスがずれている可能性があるため毎回確認
            _myIndex      = MobRegistry.Instance.GetIndex(_mobController);
            _followTarget = MobRegistry.Instance.GetFollowTarget(_myIndex);
        }

        private static Vector2Int QuantizeDirection(Vector2 dir)
        {
            if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
                return dir.x >= 0 ? Vector2Int.right : Vector2Int.left;
            return dir.y >= 0 ? Vector2Int.up : Vector2Int.down;
        }
    }
}
