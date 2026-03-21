using UnityEngine;

namespace Mob
{
    /// <summary>
    /// モード2: MobRegistry に登録された順番で前の Mob（または Player）を追従する。
    ///
    /// ---- 速度同期 ----
    ///   Player の MouseFollowMovement.moveSpeed をそのまま使用する。
    ///   参照は OnEnter 時に MobRegistry.PlayerTransform の GameObject から取得し、
    ///   Execute() で毎フレーム最新値を読み取る。
    ///   取得できない場合は fallbackMoveSpeed にフォールバックする。
    /// </summary>
    public class ChaseBehavior : MonoBehaviour, IMobBehavior
    {
        [Header("Chase Settings")]
        [Tooltip("Player の MouseFollowMovement が取得できない場合の代替速度（Units/秒）")]
        [SerializeField] private float fallbackMoveSpeed = 3.0f;

        [Tooltip("この距離以内に入ったら停止")]
        [SerializeField] private float stopDistance = 0.5f;

        [Tooltip("追従ターゲットを再取得する間隔（秒）")]
        [SerializeField] private float targetUpdateInterval = 0.3f;

        // ---- 内部参照 ----
        private MobController        _mobController;
        private MobAnimator          _mobAnimator;
        private MouseFollowMovement  _playerMovement;   // Player の速度参照元

        // ---- 内部状態 ----
        private Vector2Int _lastAnimDir = Vector2Int.down;
        private int        _myIndex     = -1;
        private Transform  _followTarget;
        private float      _targetTimer;

        /// <summary>現在の移動速度。Player の moveSpeed と同期、取得不可時は fallback。</summary>
        private float CurrentSpeed =>
            _playerMovement != null ? _playerMovement.moveSpeed : fallbackMoveSpeed;

        // ========== Unity ==========

        private void Awake()
        {
            _mobController = GetComponent<MobController>();
            _mobAnimator   = GetComponent<MobAnimator>();
        }

        // ========== IMobBehavior ==========

        public void Execute()
        {
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
            transform.position += (Vector3)(dir * (CurrentSpeed * Time.deltaTime));

            Vector2Int animDir = QuantizeDirection(dir);
            if (animDir != _lastAnimDir)
            {
                _mobAnimator?.PlayWalk(animDir);
                _lastAnimDir = animDir;
            }
        }

        public void OnEnter()
        {
            if (MobRegistry.Instance != null)
            {
                _myIndex = MobRegistry.Instance.Register(_mobController);

                // Player の MouseFollowMovement を取得
                Transform playerTransform = MobRegistry.Instance.PlayerTransform;
                if (playerTransform != null)
                {
                    _playerMovement = playerTransform.GetComponent<MouseFollowMovement>();
                    if (_playerMovement == null)
                        Debug.LogWarning("[ChaseBehavior] Player に MouseFollowMovement が見つかりません。fallbackMoveSpeed を使用します。");
                }
            }
            else
            {
                Debug.LogWarning("[ChaseBehavior] MobRegistry が見つかりません。");
            }

            _lastAnimDir = Vector2Int.down;
            _targetTimer = 0f;
            _mobAnimator?.PlayIdle();
        }

        public void OnExit()
        {
            _mobAnimator?.PlayIdle();
            _followTarget   = null;
            _playerMovement = null;
        }

        public void OnModeChanged() => OnEnter();

        // ========== Private ==========

        private void RefreshTarget()
        {
            if (MobRegistry.Instance == null) return;
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
