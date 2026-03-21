using System.Collections;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// Mode 1: random 4-directional movement within a set range from the spawn point.
    /// Idle animation when standing still, Walk animation when moving.
    /// Sprite flips based on horizontal direction.
    /// </summary>
    public class PatrolBehavior : MonoBehaviour, IMobBehavior
    {
        [Header("Patrol Settings")]
        [Tooltip("Movement range in cells from the origin.")]
        [SerializeField] private int patrolRange = 3;

        [Tooltip("Seconds to move one cell.")]
        [SerializeField] private float secondsPerCell = 0.4f;

        [Tooltip("Wait time between moves (seconds).")]
        [SerializeField] private float waitInterval = 0.6f;

        [Tooltip("Minimum steps per move.")]
        [SerializeField] private int minSteps = 1;

        [Tooltip("Maximum steps per move (auto-clamped to patrolRange).")]
        [SerializeField] private int maxSteps = 3;

        // ── refs ──────────────────────────────────────────────────────────────
        private MobAnimator _mobAnimator;
        private Animator _animator;
        private SpriteRenderer _sr;

        // ── state ─────────────────────────────────────────────────────────────
        private Vector2Int _origin;
        private Vector2Int _currentCell;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        // ========== Unity ==========

        private void Awake()
        {
            _mobAnimator = GetComponent<MobAnimator>();
            _animator = GetComponent<Animator>();
            _sr = GetComponent<SpriteRenderer>();
        }

        // ========== IMobBehavior ==========

        public void Execute() { }   // movement is coroutine-driven

        public void OnEnter()
        {
            StopAllCoroutines();
            _origin = WorldToCell(transform.position);
            _currentCell = _origin;
            SetWalking(false);
            StartCoroutine(PatrolRoutine());
        }

        public void OnExit()
        {
            StopAllCoroutines();
            SetWalking(false);
            _mobAnimator?.PlayIdle();
        }

        public void OnModeChanged() => OnEnter();

        // ========== Coroutine ==========

        private IEnumerator PatrolRoutine()
        {
            while (true)
            {
                SetWalking(false);
                _mobAnimator?.PlayIdle();

                yield return new WaitForSeconds(waitInterval);

                if (!TryPickMove(out Vector2Int direction, out int steps))
                    continue;

                yield return StartCoroutine(MoveSteps(direction, steps));
            }
        }

        private IEnumerator MoveSteps(Vector2Int direction, int steps)
        {
            SetWalking(true);
            _mobAnimator?.PlayWalk(direction);
            Flip(direction);

            for (int i = 0; i < steps; i++)
            {
                Vector3 startPos = transform.position;
                Vector3 targetPos = CellToWorld(_currentCell + direction);

                float elapsed = 0f;
                while (elapsed < secondsPerCell)
                {
                    elapsed += Time.deltaTime;
                    transform.position = Vector3.Lerp(startPos, targetPos, elapsed / secondsPerCell);
                    yield return null;
                }

                transform.position = targetPos;
                _currentCell += direction;
            }
        }

        // ========== Helpers ==========

        private void SetWalking(bool walking)
        {
            if (_animator != null)
                _animator.SetBool("IsWalking", walking);
        }

        private void Flip(Vector2Int direction)
        {
            if (_sr == null || direction.x == 0) return;
            _sr.flipX = direction.x < 0;
        }

        private bool TryPickMove(out Vector2Int direction, out int steps)
        {
            direction = Vector2Int.zero;
            steps = 0;

            foreach (var d in ShuffledDirections())
            {
                int reachable = CountReachableSteps(d);
                if (reachable < 1) continue;

                int clampedMax = Mathf.Min(maxSteps, reachable);
                int clampedMin = Mathf.Min(minSteps, clampedMax);

                direction = d;
                steps = Random.Range(clampedMin, clampedMax + 1);
                return true;
            }

            return false;
        }

        private int CountReachableSteps(Vector2Int dir)
        {
            int count = 0;
            Vector2Int cell = _currentCell;
            for (int i = 0; i < patrolRange * 2; i++)
            {
                cell += dir;
                if (!IsWithinRange(cell)) break;
                count++;
            }
            return count;
        }

        private bool IsWithinRange(Vector2Int cell)
        {
            int dx = Mathf.Abs(cell.x - _origin.x);
            int dy = Mathf.Abs(cell.y - _origin.y);
            return dx <= patrolRange && dy <= patrolRange;
        }

        private static Vector2Int[] ShuffledDirections()
        {
            Vector2Int[] arr = (Vector2Int[])Directions.Clone();
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }

        private static Vector2Int WorldToCell(Vector3 pos) => new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        private static Vector3 CellToWorld(Vector2Int c) => new Vector3(c.x, c.y, 0f);

        // ========== Gizmo ==========
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Vector3 center = Application.isPlaying ? CellToWorld(_origin) : transform.position;
            float size = patrolRange * 2 + 1;
            Gizmos.DrawCube(center, new Vector3(size, size, 0.1f));
        }
#endif
    }
}