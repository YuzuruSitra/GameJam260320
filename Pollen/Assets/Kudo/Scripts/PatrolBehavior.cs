using System.Collections;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// モード1: 初期地点から指定マス範囲内で 4 方向ランダム移動。
    /// 移動ごとにランダムな距離（マス数）を決定し、
    /// 1 マスあたりの速度は一定のまま連続して移動する。
    /// </summary>
    public class PatrolBehavior : MonoBehaviour, IMobBehavior
    {
        [Header("Patrol Settings")]
        [Tooltip("初期地点からの移動可能範囲（マス数）")]
        [SerializeField] private int patrolRange = 3;

        [Tooltip("1 マス移動にかかる秒数（速度の基準）")]
        [SerializeField] private float secondsPerCell = 0.4f;

        [Tooltip("次の移動開始までの待機秒数")]
        [SerializeField] private float waitInterval = 0.6f;

        [Tooltip("1 回の移動で進む最小マス数")]
        [SerializeField] private int minSteps = 1;

        [Tooltip("1 回の移動で進む最大マス数（patrolRange を超えないよう自動クランプ）")]
        [SerializeField] private int maxSteps = 3;

        // 初期地点（グリッド座標）
        private Vector2Int _origin;

        // 現在のグリッド座標
        private Vector2Int _currentCell;

        private MobAnimator _mobAnimator;

        // 4 方向定義
        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private void Awake()
        {
            _mobAnimator = GetComponent<MobAnimator>();
        }

        // ========== IMobBehavior ==========

        public void Execute() { }   // 移動は Coroutine で制御

        public void OnModeChanged()
        {
            StopAllCoroutines();
            _origin      = WorldToCell(transform.position);
            _currentCell = _origin;
            StartCoroutine(PatrolRoutine());
        }

        // ========== Coroutine ==========

        private IEnumerator PatrolRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(waitInterval);

                // 方向と距離をまとめて決定する
                if (!TryPickMove(out Vector2Int direction, out int steps))
                    continue;   // 全方向が範囲外 → 待機してリトライ

                // 決定した方向で steps マス連続移動
                yield return StartCoroutine(MoveSteps(direction, steps));
            }
        }

        /// <summary>
        /// direction × steps マスを一定速度で連続移動する。
        /// アニメは移動開始時に 1 回だけ切り替え、完了後に Idle へ戻す。
        /// </summary>
        private IEnumerator MoveSteps(Vector2Int direction, int steps)
        {
            _mobAnimator?.PlayWalk(direction);

            for (int i = 0; i < steps; i++)
            {
                Vector3 startPos  = transform.position;
                Vector3 targetPos = CellToWorld(_currentCell + direction);

                float elapsed = 0f;
                while (elapsed < secondsPerCell)
                {
                    elapsed += Time.deltaTime;
                    transform.position = Vector3.Lerp(startPos, targetPos, elapsed / secondsPerCell);
                    yield return null;
                }

                transform.position = targetPos;
                _currentCell      += direction;
            }

            _mobAnimator?.PlayIdle();
        }

        // ========== Helpers ==========

        /// <summary>
        /// 移動可能な方向と距離をランダムに決定する。
        /// 方向をシャッフルし、最初に有効だった方向を採用。
        /// 距離は「その方向に実際に進める最大数」と maxSteps の小さい方を上限にランダム決定。
        /// </summary>
        private bool TryPickMove(out Vector2Int direction, out int steps)
        {
            direction = Vector2Int.zero;
            steps     = 0;

            foreach (var d in ShuffledDirections())
            {
                // この方向に何マス進めるか計算（少なくとも 1 マス進める方向のみ対象）
                int reachable = CountReachableSteps(d);
                if (reachable < 1) continue;

                int clampedMax = Mathf.Min(maxSteps, reachable);
                int clampedMin = Mathf.Min(minSteps, clampedMax);

                direction = d;
                steps     = Random.Range(clampedMin, clampedMax + 1);
                return true;
            }

            return false;   // 全方向が範囲外
        }

        /// <summary>指定方向に範囲内で進める最大マス数を返す。</summary>
        private int CountReachableSteps(Vector2Int dir)
        {
            int count = 0;
            Vector2Int cell = _currentCell;
            // patrolRange * 2 を上限に 1 マスずつ確認（実質的に patrolRange が上限になる）
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

        // グリッド ⇔ ワールド変換（1 マス = 1 Unit 想定）
        private static Vector2Int WorldToCell(Vector3 pos) => new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));
        private static Vector3   CellToWorld(Vector2Int c) => new Vector3(c.x, c.y, 0f);

        // ========== Gizmo ==========
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.25f);
            Vector3 center = Application.isPlaying ? CellToWorld(_origin) : transform.position;
            float size     = patrolRange * 2 + 1;
            Gizmos.DrawCube(center, new Vector3(size, size, 0.1f));
        }
#endif
    }
}
