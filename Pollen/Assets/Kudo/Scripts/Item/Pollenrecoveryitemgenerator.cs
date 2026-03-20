using System.Collections.Generic;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// 花粉回復アイテムをランダムな位置に生成するジェネレーター。
    ///
    /// ---- カーブ設定 ----
    ///   横軸 0〜1 = 1 サイクル（cycleDuration 秒）で繰り返す。
    ///   spawnCountCurve  : baseMaxCount への乗数 → 目標アクティブ上限数が変化
    ///   recoveryAmountCurve : baseRecoveryAmount への乗数 → 回復量が変化
    ///
    /// ---- 補充ロジック ----
    ///   毎フレームカーブから「現在の目標上限数」を計算し、
    ///   アクティブ数が上限を下回っていれば非アクティブアイテムを補充する。
    ///   補充チェックは replenishInterval 秒ごとに行う。
    ///
    /// ---- プール設計 ----
    ///   Instantiate / Destroy を繰り返さず SetActive で出し入れする。
    ///   プールサイズは baseMaxCount × maxCountCurvePeak を切り上げた値で事前確保。
    /// </summary>
    public class PollenRecoveryItemGenerator : MonoBehaviour
    {
        // ================================================================
        //  インスペクタ設定
        // ================================================================

        [Header("Prefab")]
        [Tooltip("PollenRecoveryItem コンポーネントを持つプレハブ")]
        [SerializeField] private PollenRecoveryItem itemPrefab;

        // ---- 生成数カーブ ----
        [Header("Spawn Count")]
        [Tooltip("同時に存在させるアイテムの基準上限数")]
        [SerializeField] private int baseMaxCount = 5;

        [Tooltip("上限数の時間変化カーブ（横軸 0〜1、縦軸 = baseMaxCount への乗数）")]
        [SerializeField] private AnimationCurve spawnCountCurve = AnimationCurve.Constant(0f, 1f, 1f);

        // ---- 回復量カーブ ----
        [Header("Recovery Amount")]
        [Tooltip("アイテム 1 個あたりの基準回復量")]
        [SerializeField] private float baseRecoveryAmount = 30f;

        [Tooltip("回復量の時間変化カーブ（横軸 0〜1、縦軸 = baseRecoveryAmount への乗数）")]
        [SerializeField] private AnimationCurve recoveryAmountCurve = AnimationCurve.Constant(0f, 1f, 1f);

        // ---- カーブ周期 ----
        [Header("Curve Cycle")]
        [Tooltip("両カーブの 1 サイクル秒数（0 以下で固定値）")]
        [SerializeField] private float cycleDuration = 60f;

        // ---- 生成エリア ----
        [Header("Spawn Area")]
        [Tooltip("生成基点（未設定時はこの GameObject の位置）")]
        [SerializeField] private Transform spawnCenter;

        [Tooltip("生成基点からの最大半径（World 単位）")]
        [SerializeField] private float spawnRadius = 10f;

        [Tooltip("アイテム同士の最低間隔（マス数）")]
        [SerializeField] private int minSpacing = 2;

        // ---- 補充 ----
        [Header("Replenish")]
        [Tooltip("補充チェックの間隔（秒）")]
        [SerializeField] private float replenishInterval = 1.0f;

        // ---- リトライ ----
        [Header("Retry")]
        [Tooltip("配置候補が minSpacing を満たさない場合の最大リトライ回数")]
        [SerializeField] private int maxRetries = 30;

        // ---- 自動開始 ----
        [Header("Auto Spawn")]
        [Tooltip("true: Start() で自動生成 / false: 外部から SpawnAll() を呼ぶ")]
        [SerializeField] private bool autoSpawnOnStart = true;

        // ================================================================
        //  内部状態
        // ================================================================

        /// <summary>生成済みアイテムの全インスタンス（アクティブ・非アクティブ含む）。</summary>
        private readonly List<PollenRecoveryItem> _items = new();

        private float _elapsed;
        private float _replenishTimer;

        /// <summary>現在アクティブなアイテム数。</summary>
        public int ActiveCount
        {
            get
            {
                int count = 0;
                foreach (var item in _items)
                    if (item != null && item.gameObject.activeSelf) count++;
                return count;
            }
        }

        /// <summary>現在のカーブ評価による目標上限数（外部参照用）。</summary>
        public int CurrentMaxCount { get; private set; }

        /// <summary>現在のカーブ評価による回復量（外部参照用）。</summary>
        public float CurrentRecoveryAmount { get; private set; }

        // ================================================================
        //  Unity
        // ================================================================

        private void Start()
        {
            if (autoSpawnOnStart)
                SpawnAll();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float t = EvalCurveT();
            CurrentMaxCount       = Mathf.Max(0, Mathf.RoundToInt(baseMaxCount * spawnCountCurve.Evaluate(t)));
            CurrentRecoveryAmount = baseRecoveryAmount * recoveryAmountCurve.Evaluate(t);

            // 補充チェック
            _replenishTimer += Time.deltaTime;
            if (_replenishTimer >= replenishInterval)
            {
                _replenishTimer = 0f;
                TryReplenish();
            }

            // 上限を超えているアイテムを非アクティブ化（カーブで上限が下がった場合）
            TrimToMaxCount();
        }

        // ================================================================
        //  Public API
        // ================================================================

        /// <summary>
        /// プールを初期化してアイテムを一括生成する。
        /// カーブの最大値を見越した数だけ事前に Instantiate する。
        /// </summary>
        public void SpawnAll()
        {
            if (itemPrefab == null)
            {
                Debug.LogError("[PollenRecoveryItemGenerator] itemPrefab が未設定です。");
                return;
            }

            DespawnAll();

            // カーブのピーク値からプールサイズを決定
            int poolSize = Mathf.Max(baseMaxCount, EvalMaxCountInCurve());

            Vector2 center    = GetCenter();
            var usedCells     = new List<Vector2Int>();
            float t           = EvalCurveT();
            int initialCount  = Mathf.Max(0, Mathf.RoundToInt(baseMaxCount * spawnCountCurve.Evaluate(t)));

            for (int i = 0; i < poolSize; i++)
            {
                Vector2Int cell = Vector2Int.zero;
                bool activate   = i < initialCount
                    && TryGetCell(center, usedCells, out cell);

                Vector3 pos = activate ? CellToWorld(cell) : Vector3.zero;
                var item    = Instantiate(itemPrefab, pos, Quaternion.identity, transform);
                item.name   = $"RecoveryItem_{i:00}";
                item.gameObject.SetActive(activate);

                if (activate)
                    usedCells.Add(WorldToCell(pos));

                _items.Add(item);
            }

            Debug.Log($"[PollenRecoveryItemGenerator] 生成完了: プール={poolSize} / アクティブ={ActiveCount}");
        }

        /// <summary>全アイテムを非アクティブにする。</summary>
        public void HideAll()
        {
            foreach (var item in _items)
                if (item != null) item.gameObject.SetActive(false);
        }

        /// <summary>全アイテムを破棄してリストをクリアする。</summary>
        public void DespawnAll()
        {
            foreach (var item in _items)
                if (item != null) Destroy(item.gameObject);
            _items.Clear();
        }

        // ================================================================
        //  補充 / トリム処理
        // ================================================================

        /// <summary>
        /// アクティブ数が CurrentMaxCount を下回っていれば
        /// 非アクティブのアイテムを新しい位置に再配置して再表示する。
        /// </summary>
        private void TryReplenish()
        {
            if (ActiveCount >= CurrentMaxCount) return;

            Vector2 center  = GetCenter();
            var activeCells = GetActiveCells();
            float amount    = CurrentRecoveryAmount;

            foreach (var item in _items)
            {
                if (ActiveCount >= CurrentMaxCount) break;
                if (item == null || item.gameObject.activeSelf) continue;

                if (TryGetCell(center, activeCells, out Vector2Int cell))
                {
                    item.transform.position = CellToWorld(cell);
                    item.Init(amount);
                    item.gameObject.SetActive(true);
                    activeCells.Add(cell);
                }
            }
        }

        /// <summary>カーブで上限が下がったとき、超過分を非アクティブ化する。</summary>
        private void TrimToMaxCount()
        {
            if (ActiveCount <= CurrentMaxCount) return;

            int excess = ActiveCount - CurrentMaxCount;
            foreach (var item in _items)
            {
                if (excess <= 0) break;
                if (item == null || !item.gameObject.activeSelf) continue;
                item.gameObject.SetActive(false);
                excess--;
            }
        }

        // ================================================================
        //  ユーティリティ
        // ================================================================

        /// <summary>カーブ評価用の正規化時間 t（0〜1）を返す。</summary>
        private float EvalCurveT()
        {
            if (cycleDuration <= 0f) return 0f;
            return Mathf.Repeat(_elapsed, cycleDuration) / cycleDuration;
        }

        /// <summary>spawnCountCurve 全体のサンプルから最大乗数を求めてプールサイズを算出する。</summary>
        private int EvalMaxCountInCurve()
        {
            float peak = 1f;
            const int samples = 20;
            for (int i = 0; i <= samples; i++)
                peak = Mathf.Max(peak, spawnCountCurve.Evaluate((float)i / samples));
            return Mathf.CeilToInt(baseMaxCount * peak);
        }

        private Vector2 GetCenter() =>
            spawnCenter != null ? (Vector2)spawnCenter.position : (Vector2)transform.position;

        private List<Vector2Int> GetActiveCells()
        {
            var cells = new List<Vector2Int>();
            foreach (var item in _items)
                if (item != null && item.gameObject.activeSelf)
                    cells.Add(WorldToCell(item.transform.position));
            return cells;
        }

        private bool TryGetCell(Vector2 center, List<Vector2Int> usedCells, out Vector2Int cell)
        {
            cell = Vector2Int.zero;
            for (int i = 0; i < maxRetries; i++)
            {
                Vector2Int candidate = WorldToCell(center + Random.insideUnitCircle * spawnRadius);
                if (IsFarEnough(candidate, usedCells))
                {
                    cell = candidate;
                    return true;
                }
            }
            return false;
        }

        private bool IsFarEnough(Vector2Int candidate, List<Vector2Int> usedCells)
        {
            foreach (var used in usedCells)
                if (Mathf.Max(Mathf.Abs(candidate.x - used.x), Mathf.Abs(candidate.y - used.y)) < minSpacing)
                    return false;
            return true;
        }

        private static Vector2Int WorldToCell(Vector3 pos) =>
            new Vector2Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y));

        private static Vector3 CellToWorld(Vector2Int cell) =>
            new Vector3(cell.x, cell.y, 0f);

        // ================================================================
        //  Gizmo
        // ================================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.25f);
            Gizmos.DrawWireSphere(center, spawnRadius);

            Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.15f);
            foreach (var item in _items)
            {
                if (item == null || !item.gameObject.activeSelf) continue;
                Gizmos.DrawWireCube(item.transform.position,
                    new Vector3(minSpacing * 2, minSpacing * 2, 0.1f));
            }
        }
#endif
    }
}
