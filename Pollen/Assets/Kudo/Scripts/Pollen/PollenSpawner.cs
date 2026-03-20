using UnityEngine;
using UnityEngine.Pool;

namespace Pollen
{
    /// <summary>
    /// 花粉の玉を画面端からランダムに生成し続けるスポナー。
    ///
    /// ---- 設計概要 ----
    ///   ・ObjectPool&lt;PollenBall&gt; で玉を使い回す
    ///   ・maxPoolSize で同時存在できる上限を設定（上限到達時は生成をスキップ）
    ///   ・spawnRateCurve : 時間軸に対する「1 秒あたり生成数」の倍率
    ///   ・speedCurve     : 時間軸に対する「玉の速度」の倍率
    ///   ・両カーブの横軸は 0〜1（0 = 開始, 1 = cycleDuration 秒後）で繰り返す
    ///   ・生成位置はカメラ外周からランダムに選ばれ、進行方向は画面内へ向かう
    ///
    /// Camera.main が存在することを前提とする。
    /// </summary>
    public class PollenSpawner : MonoBehaviour
    {
        // ================================================================
        //  インスペクタ設定
        // ================================================================

        [Header("Prefab")]
        [Tooltip("PollenBall コンポーネントを持つプレハブ")]
        [SerializeField] private PollenBall pollenBallPrefab;

        // ---- プール設定 ----
        [Header("Object Pool")]
        [Tooltip("起動時にプールへ事前確保する数")]
        [SerializeField] private int initialPoolSize = 10;

        [Tooltip("同時に存在できる玉の上限数（これ以上は生成をスキップ）")]
        [SerializeField] private int maxPoolSize = 50;

        // ---- 生成レート ----
        [Header("Spawn Rate")]
        [Tooltip("基準となる 1 秒あたりの生成数")]
        [SerializeField] private float baseSpawnRate = 3f;

        [Tooltip("生成レートの時間変化カーブ（横軸 0〜1、縦軸 = baseSpawnRate への乗数）")]
        [SerializeField] private AnimationCurve spawnRateCurve = AnimationCurve.Constant(0f, 1f, 1f);

        // ---- 速度 ----
        [Header("Speed")]
        [Tooltip("基準となる玉の移動速度（Units/秒）")]
        [SerializeField] private float baseSpeed = 2f;

        [Tooltip("速度の時間変化カーブ（横軸 0〜1、縦軸 = baseSpeed への乗数）")]
        [SerializeField] private AnimationCurve speedCurve = AnimationCurve.Constant(0f, 1f, 1f);

        // ---- カーブ繰り返し周期 ----
        [Header("Curve Cycle")]
        [Tooltip("spawnRateCurve / speedCurve の 1 サイクル秒数。0 以下で固定値")]
        [SerializeField] private float cycleDuration = 60f;

        // ---- 揺れ ----
        [Header("Sway")]
        [Tooltip("揺れの振れ幅（Units）")]
        [SerializeField] private float swayAmplitude = 0.3f;

        [Tooltip("揺れの周波数（Hz）")]
        [SerializeField] private float swayFrequency = 0.8f;

        [Tooltip("振れ幅のランダム上乗せ範囲（+0〜この値）")]
        [SerializeField] private float swayAmplitudeVariance = 0.2f;

        [Tooltip("周波数のランダム上乗せ範囲（+0〜この値）")]
        [SerializeField] private float swayFrequencyVariance = 0.4f;

        // ---- 花粉ゲージ加算量 ----
        [Header("Pollen Amount on Hit")]
        [Tooltip("Player に当たったときの花粉加算量")]
        [SerializeField] private float pollenForPlayer = 10f;

        [Tooltip("Mob に当たったときの花粉加算量")]
        [SerializeField] private float pollenForMob = 5f;

        // ---- 生成マージン ----
        [Header("Spawn Margin")]
        [Tooltip("カメラ外周からの生成オフセット（画面外に少し出た位置から生成）")]
        [SerializeField] private float spawnMargin = 0.5f;

        // ---- 進行方向ランダム幅 ----
        [Header("Direction Spread")]
        [Tooltip("画面中心方向からの最大ランダム角度ずれ（度）")]
        [SerializeField] [Range(0f, 80f)] private float directionSpread = 30f;

        // ================================================================
        //  内部状態
        // ================================================================

        private IObjectPool<PollenBall> _pool;
        private float _elapsed;
        private float _spawnAccumulator;

        /// <summary>現在アクティブな玉の数（デバッグ用に読み取り可能）</summary>
        public int ActiveCount { get; private set; }

        // ================================================================
        //  Unity
        // ================================================================

        private void Awake()
        {
            // プール生成
            // collectionCheck: true にすると二重 Release 時に例外でデバッグしやすい
            _pool = new ObjectPool<PollenBall>(
                createFunc:     CreateBall,
                actionOnGet:    OnGetBall,
                actionOnRelease: OnReleaseBall,
                actionOnDestroy: OnDestroyBall,
                collectionCheck: true,
                defaultCapacity: initialPoolSize,
                maxSize:         maxPoolSize
            );

            // 事前確保：initialPoolSize 個を一度 Get → 即 Release してキャッシュ
            var prewarm = new PollenBall[initialPoolSize];
            for (int i = 0; i < initialPoolSize; i++)
                prewarm[i] = _pool.Get();
            for (int i = 0; i < initialPoolSize; i++)
                _pool.Release(prewarm[i]);
        }

        private void OnEnable()
        {
            _elapsed          = 0f;
            _spawnAccumulator = 0f;
        }

        private void OnDestroy()
        {
            _pool?.Clear();
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;

            float t    = EvalCurveT();
            float rate = baseSpawnRate * spawnRateCurve.Evaluate(t);
            _spawnAccumulator += rate * Time.deltaTime;

            int spawnCount     = Mathf.FloorToInt(_spawnAccumulator);
            _spawnAccumulator -= spawnCount;

            float speed = baseSpeed * speedCurve.Evaluate(t);

            for (int i = 0; i < spawnCount; i++)
                SpawnOne(speed);
        }

        // ================================================================
        //  プールコールバック
        // ================================================================

        private PollenBall CreateBall()
        {
            var ball = Instantiate(pollenBallPrefab, transform);
            ball.gameObject.SetActive(false);
            return ball;
        }

        private void OnGetBall(PollenBall ball)
        {
            ball.gameObject.SetActive(true);
            ActiveCount++;
        }

        private void OnReleaseBall(PollenBall ball)
        {
            ball.gameObject.SetActive(false);
            ActiveCount--;
        }

        private static void OnDestroyBall(PollenBall ball)
        {
            // maxPoolSize を超えて返却された余剰オブジェクトを破棄
            if (ball != null)
                Destroy(ball.gameObject);
        }

        // ================================================================
        //  生成処理
        // ================================================================

        /// <summary>プールから取り出して画面端に配置する。上限到達時はスキップ。</summary>
        private void SpawnOne(float speed)
        {
            if (pollenBallPrefab == null)
            {
                Debug.LogWarning("[PollenSpawner] pollenBallPrefab が未設定です。");
                return;
            }

            // 上限チェック（CountAll = アクティブ + 非アクティブのプール内総数）
            if (ActiveCount >= maxPoolSize) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float camH        = cam.orthographicSize;
            float camW        = camH * cam.aspect;
            Vector2 camCenter = cam.transform.position;

            Vector2 spawnPos  = PickSpawnPosition(camW, camH, camCenter);
            Vector2 baseDir   = (camCenter - spawnPos).normalized;
            float angleOffset = Random.Range(-directionSpread, directionSpread);
            Vector2 moveDir   = Rotate(baseDir, angleOffset);

            // プールから取り出し → 位置セット → 初期化
            PollenBall ball = _pool.Get();
            ball.transform.position = spawnPos;

            float amp  = swayAmplitude  + Random.Range(0f, swayAmplitudeVariance);
            float freq = swayFrequency  + Random.Range(0f, swayFrequencyVariance);

            ball.Init(_pool, moveDir, speed, amp, freq, pollenForPlayer, pollenForMob);
        }

        // ================================================================
        //  ユーティリティ
        // ================================================================

        private float EvalCurveT()
        {
            if (cycleDuration <= 0f) return 0f;
            return Mathf.Repeat(_elapsed, cycleDuration) / cycleDuration;
        }

        private Vector2 PickSpawnPosition(float camW, float camH, Vector2 center)
        {
            int side = Random.Range(0, 4);
            return side switch
            {
                0 => new Vector2(Random.Range(center.x - camW, center.x + camW), center.y + camH + spawnMargin), // 上
                1 => new Vector2(Random.Range(center.x - camW, center.x + camW), center.y - camH - spawnMargin), // 下
                2 => new Vector2(center.x - camW - spawnMargin, Random.Range(center.y - camH, center.y + camH)), // 左
                _ => new Vector2(center.x + camW + spawnMargin, Random.Range(center.y - camH, center.y + camH)), // 右
            };
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // ================================================================
        //  Gizmo
        // ================================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            float camH = cam.orthographicSize;
            float camW = camH * cam.aspect;
            Vector3 c  = cam.transform.position;

            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.4f);
            Gizmos.DrawWireCube(c, new Vector3(camW * 2f, camH * 2f, 0f));
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
            Gizmos.DrawWireCube(c, new Vector3((camW + spawnMargin) * 2f, (camH + spawnMargin) * 2f, 0f));
        }

        private void OnGUI()
        {
            // 実行中に右上へアクティブ数を表示
            if (!Application.isPlaying) return;
            GUI.Label(new Rect(Screen.width - 200, 10, 190, 24),
                $"PollenBall: {ActiveCount} / {maxPoolSize}");
        }
#endif
    }
}
