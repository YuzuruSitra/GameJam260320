using UnityEngine;

namespace Mob
{
    /// <summary>
    /// Mode 2: follows the previous Mob (or Player) in MobRegistry order.
    /// Also spawns a sneeze prefab in a random direction at a fixed interval.
    /// </summary>
    public class ChaseBehavior : MonoBehaviour, IMobBehavior
    {
        [Header("Chase Settings")]
        [Tooltip("Fallback speed if MouseFollowMovement cannot be found.")]
        [SerializeField] private float fallbackMoveSpeed = 3.0f;

        [Tooltip("Stop when within this distance of the target.")]
        [SerializeField] private float stopDistance = 0.5f;

        [Tooltip("How often to re-fetch the follow target (seconds).")]
        [SerializeField] private float targetUpdateInterval = 0.3f;

        [Header("Sneeze")]
        [Tooltip("Same prefab assigned on the player's ProjectileSpawner.")]
        [SerializeField] private GameObject sneezePrefab;

        [Tooltip("Seconds between each sneeze spawn.")]
        [SerializeField] private float sneezeInterval = 1.5f;

        [Tooltip("How far from the mob center the prefab appears.")]
        [SerializeField] private float sneezeSpawnDistance = 0.6f;

        [Tooltip("Random variation added to each interval (±seconds). 0 = perfectly regular.")]
        [SerializeField] private float sneezeIntervalVariance = 0.4f;

        [Tooltip("Random scale range for each spawned prefab.")]
        [SerializeField] private float sneezeMinScale = 0.6f;
        [SerializeField] private float sneezeMaxScale = 1.4f;

        [Tooltip("Spawn this many prefabs per sneeze burst (randomly chosen in range).")]
        [SerializeField] private int sneezeMinCount = 1;
        [SerializeField] private int sneezeMaxCount = 3;

        // ── internal refs ─────────────────────────────────────────────────────
        private MobController _mobController;
        private MobAnimator _mobAnimator;
        private MouseFollowMovement _playerMovement;
        private Animator _animator;
        private SpriteRenderer _sr;

        // ── internal state ────────────────────────────────────────────────────
        private Vector2Int _lastAnimDir = Vector2Int.down;
        private int _myIndex = -1;
        private Transform _followTarget;
        private float _targetTimer;
        private float _sneezeTimer;

        private float CurrentSpeed =>
            _playerMovement != null ? _playerMovement.moveSpeed : fallbackMoveSpeed;

        // ========== Unity ==========

        private void Awake()
        {
            _mobController = GetComponent<MobController>();
            _mobAnimator = GetComponent<MobAnimator>();
            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
        }

        // ========== IMobBehavior ==========

        public void Execute()
        {
            // ── follow target ─────────────────────────────────────────────────
            _targetTimer -= Time.deltaTime;
            if (_targetTimer <= 0f)
            {
                RefreshTarget();
                _targetTimer = targetUpdateInterval;
            }

            bool isMoving = false;

            if (_followTarget != null)
            {
                Vector2 toTarget = (Vector2)_followTarget.position - (Vector2)transform.position;
                float distance = toTarget.magnitude;

                if (distance > stopDistance)
                {
                    isMoving = true;
                    Vector2 dir = toTarget.normalized;
                    transform.position += (Vector3)(dir * (CurrentSpeed * Time.deltaTime));

                    // Flip sprite — face right = no flip, face left = flip
                    if (_sr != null && Mathf.Abs(dir.x) > 0.01f)
                        _sr.flipX = dir.x < 0f;

                    Vector2Int animDir = QuantizeDirection(dir);
                    if (animDir != _lastAnimDir)
                    {
                        _mobAnimator?.PlayWalk(animDir);
                        _lastAnimDir = animDir;
                    }
                }
            }

            // Drive Animator states
            if (_animator != null)
                _animator.SetBool("IsWalking", isMoving);

            if (!isMoving)
                _mobAnimator?.PlayIdle();

            // ── sneeze ────────────────────────────────────────────────────────
            _sneezeTimer -= Time.deltaTime;
            if (_sneezeTimer <= 0f)
            {
                SpawnSneeze(); // timer reset is handled inside SpawnSneeze
            }
        }

        public void OnEnter()
        {
            if (MobRegistry.Instance != null)
            {
                _myIndex = MobRegistry.Instance.Register(_mobController);

                Transform playerTransform = MobRegistry.Instance.PlayerTransform;
                if (playerTransform != null)
                {
                    _playerMovement = playerTransform.GetComponent<MouseFollowMovement>();
                    if (_playerMovement == null)
                        Debug.LogWarning("[ChaseBehavior] MouseFollowMovement not found on Player. Using fallback speed.");
                }
            }
            else
            {
                Debug.LogWarning("[ChaseBehavior] MobRegistry not found.");
            }

            _lastAnimDir = Vector2Int.down;
            _targetTimer = 0f;
            _sneezeTimer = sneezeInterval;
            _mobAnimator?.PlayIdle();
        }

        public void OnExit()
        {
            _mobAnimator?.PlayIdle();
            _followTarget = null;
            _playerMovement = null;
        }

        public void OnModeChanged() => OnEnter();

        // ========== Private ==========

        private void SpawnSneeze()
        {
            if (sneezePrefab == null) return;

            int count = Random.Range(sneezeMinCount, sneezeMaxCount + 1);
            for (int i = 0; i < count; i++)
            {
                float angle = Random.Range(0f, 360f);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector3 spawnPos = transform.position + (Vector3)(dir * sneezeSpawnDistance);
                Quaternion rotation = Quaternion.Euler(0f, 0f, angle - 90f);

                GameObject spawned = Instantiate(sneezePrefab, spawnPos, rotation);

                // Random scale per projectile
                float s = Random.Range(sneezeMinScale, sneezeMaxScale);
                spawned.transform.localScale = Vector3.one * s;
            }

            // Randomise next interval
            _sneezeTimer = sneezeInterval + Random.Range(-sneezeIntervalVariance, sneezeIntervalVariance);
            _sneezeTimer = Mathf.Max(0.1f, _sneezeTimer); // never negative
        }

        private void RefreshTarget()
        {
            if (MobRegistry.Instance == null) return;
            _myIndex = MobRegistry.Instance.GetIndex(_mobController);
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