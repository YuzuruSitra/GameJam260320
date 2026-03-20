using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// 指定数の Mob をランダムな位置に生成するジェネレーター。
    ///
    /// ---- 生成ルール ----
    ///   ・spawnCenter を基点に spawnRadius 以内のランダム位置に生成
    ///   ・生成済み座標と minSpacing マス以上離れた位置のみ採用
    ///   ・配置できない場合は maxRetries 回リトライして諦める
    ///   ・生成はシーン開始時（autoSpawnOnStart）または SpawnAll() を外部から呼ぶことで実行
    /// </summary>
    public class MobGenerator : MonoBehaviour
    {
        // ---- Singleton ----
        public static MobGenerator Instance { get; private set; }

        // ================================================================
        //  インスペクタ設定
        // ================================================================

        [Header("Prefab")]
        [Tooltip("生成する Mob プレハブ（MobController を持つこと）")]
        [SerializeField] private MobController mobPrefab;

        [Header("Spawn Count")]
        [Tooltip("生成する Mob の数")]
        [SerializeField] private int spawnCount = 5;

        [Header("Spawn Area")]
        [Tooltip("生成基点（未設定時はこの GameObject の位置）")]
        [SerializeField] private Transform spawnCenter;

        [Tooltip("生成基点からの最大半径（World 単位）")]
        [SerializeField] private float spawnRadius = 10f;

        [Tooltip("Mob 同士の最低間隔（マス数 = World 単位）")]
        [SerializeField] private int minSpacing = 3;

        [Header("Retry")]
        [Tooltip("配置候補が minSpacing を満たさない場合の最大リトライ回数")]
        [SerializeField] private int maxRetries = 30;

        [Header("Auto Spawn")]
        [Tooltip("true: Start() で自動生成 / false: 外部から SpawnAll() を呼ぶ")]
        [SerializeField] private bool autoSpawnOnStart = true;

        // ================================================================
        //  内部状態
        // ================================================================

        /// <summary>生成済み Mob の一覧（外部参照用）。</summary>
        public IReadOnlyList<MobController> SpawnedMobs => _spawnedMobs;
        private readonly List<MobController> _spawnedMobs = new();

        /// <summary>インスペクタで設定した生成数。</summary>
        public int SpawnCount => spawnCount;

        // / <summary>実際に生成に成功した数。</summary>
        public int SpawnedCount => _spawnedMobs.Count;

        /// <summary>現在シーンに存在する Mob の総数（破棄されたものは除外）。</summary>
        public int TotalMobCount { get; private set; }

        /// <summary>生成済み座標のキャッシュ（間隔チェック用）。</summary>
        private readonly List<Vector2Int> _usedCells = new();

        // ---- Events ----

        /// <summary>総 Mob 数が変化したときに発火。引数: 変化後の総数。</summary>
        public event Action<int> OnTotalCountChanged;

        // ================================================================
        //  Unity
        // ================================================================

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (autoSpawnOnStart)
                SpawnAll();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ================================================================
        //  Public API
        // ================================================================

        /// <summary>spawnCount 体の Mob を一括生成する。</summary>
        public void SpawnAll()
        {
            if (mobPrefab == null)
            {
                Debug.LogError("[MobGenerator] mobPrefab が未設定です。");
                return;
            }

            _spawnedMobs.Clear();
            _usedCells.Clear();

            Vector2 center = spawnCenter != null
                ? (Vector2)spawnCenter.position
                : (Vector2)transform.position;

            for (int i = 0; i < spawnCount; i++)
            {
                if (TryGetSpawnCell(center, out Vector2Int cell))
                {
                    Vector3 worldPos  = new Vector3(cell.x, cell.y, 0f);
                    MobController mob = Instantiate(mobPrefab, worldPos, Quaternion.identity);
                    mob.name          = $"Mob_{i:00}";

                    _spawnedMobs.Add(mob);
                    _usedCells.Add(cell);

                    // Mob 破棄時に総数を更新するため OnDestroy フックを登録
                    mob.OnMobDestroyed += HandleMobDestroyed;
                }
                else
                {
                    Debug.LogWarning($"[MobGenerator] Mob_{i:00} の配置場所が見つかりませんでした（maxRetries={maxRetries}）。");
                }
            }

            NotifyTotalCount();
            Debug.Log($"[MobGenerator] 生成完了: {_spawnedMobs.Count} / {spawnCount} 体");
        }

        /// <summary>生成した全 Mob を破棄してリストをクリアする。</summary>
        public void DespawnAll()
        {
            foreach (var mob in _spawnedMobs)
            {
                if (mob != null)
                {
                    mob.OnMobDestroyed -= HandleMobDestroyed;
                    Destroy(mob.gameObject);
                }
            }
            _spawnedMobs.Clear();
            _usedCells.Clear();
            NotifyTotalCount();
        }

        // ================================================================
        //  Internal
        // ================================================================

        private void HandleMobDestroyed(MobController mob)
        {
            mob.OnMobDestroyed -= HandleMobDestroyed;
            _spawnedMobs.Remove(mob);
            NotifyTotalCount();
        }

        private void NotifyTotalCount()
        {
            // null になった（破棄済み）エントリを除いた実数を計算
            TotalMobCount = 0;
            foreach (var mob in _spawnedMobs)
                if (mob != null) TotalMobCount++;

            OnTotalCountChanged?.Invoke(TotalMobCount);
        }

        private bool TryGetSpawnCell(Vector2 center, out Vector2Int cell)
        {
            cell = Vector2Int.zero;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                Vector2 randomPoint = center + UnityEngine.Random.insideUnitCircle * spawnRadius;
                Vector2Int candidate = new Vector2Int(
                    Mathf.RoundToInt(randomPoint.x),
                    Mathf.RoundToInt(randomPoint.y));

                if (IsFarEnough(candidate))
                {
                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        private bool IsFarEnough(Vector2Int candidate)
        {
            foreach (var used in _usedCells)
            {
                int dx = Mathf.Abs(candidate.x - used.x);
                int dy = Mathf.Abs(candidate.y - used.y);
                if (Mathf.Max(dx, dy) < minSpacing)
                    return false;
            }
            return true;
        }

        // ================================================================
        //  Gizmo
        // ================================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.3f);
            Gizmos.DrawWireSphere(center, spawnRadius);

            Gizmos.color = new Color(1f, 0.5f, 0.2f, 0.2f);
            foreach (var c in _usedCells)
            {
                Vector3 p = new Vector3(c.x, c.y, 0f);
                Gizmos.DrawWireCube(p, new Vector3(minSpacing * 2, minSpacing * 2, 0.1f));
            }
        }
#endif
    }
}
