using Pollen;
using UnityEngine;
using UnityEngine.Pool;

namespace Mob
{
    /// <summary>
    /// 周囲に一定間隔で花粉を放射し、一定時間後に消滅する樹。
    ///
    /// ---- 動作仕様 ----
    ///   ・shotInterval 秒ごとに shotCount 方向へ均等に PollenBall を発射
    ///   ・lifetime 秒後に自身を Destroy
    ///   ・発射する PollenBall は内部 ObjectPool で管理
    ///     （樹が破棄されるとプールも破棄されカメラ外の玉も自動回収）
    ///
    /// ---- Prefab 構成 ----
    ///   [PollenTree]
    ///     ├─ PollenTree.cs    ← このスクリプト
    ///     └─ SpriteRenderer  ← 見た目（任意）
    /// </summary>
    public class PollenTree : MonoBehaviour
    {
        // ================================================================
        //  インスペクタ設定
        // ================================================================

        [Header("Pollen Ball Prefab")]
        [Tooltip("発射する PollenBall プレハブ")]
        [SerializeField] private PollenBall pollenBallPrefab;

        [Header("Shot Settings")]
        [Tooltip("発射間隔（秒）")]
        [SerializeField] private float shotInterval = 2.0f;

        [Tooltip("1 回の発射で放つ方向数（均等分割）")]
        [SerializeField] private int shotCount = 8;

        [Tooltip("発射開始角度のオフセット（度）。0 = 上方向から開始")]
        [SerializeField] private float angleOffset = 0f;

        [Header("Pollen Ball Settings")]
        [Tooltip("花粉の飛翔速度（Units/秒）")]
        [SerializeField] private float ballSpeed = 2.5f;

        [Tooltip("揺れの振れ幅（Units）")]
        [SerializeField] private float swayAmplitude = 0.2f;

        [Tooltip("揺れの周波数（Hz）")]
        [SerializeField] private float swayFrequency = 0.8f;

        [Tooltip("Player に当たったときの花粉ゲージ加算量")]
        [SerializeField] private float pollenForPlayer = 10f;

        [Tooltip("Mob に当たったときの花粉ゲージ加算量")]
        [SerializeField] private float pollenForMob = 5f;

        [Header("Pool")]
        [Tooltip("プールの初期確保数")]
        [SerializeField] private int initialPoolSize = 8;

        [Tooltip("プールの最大サイズ")]
        [SerializeField] private int maxPoolSize = 32;

        [Header("Lifetime")]
        [Tooltip("樹が消滅するまでの秒数")]
        [SerializeField] private float lifetime = 10.0f;

        // ================================================================
        //  内部状態
        // ================================================================

        private IObjectPool<PollenBall> _pool;
        private float _shotTimer;
        private float _lifetimeTimer;

        // ================================================================
        //  Unity
        // ================================================================

        private void Awake()
        {
            _pool = new ObjectPool<PollenBall>(
                createFunc:      CreateBall,
                actionOnGet:     OnGetBall,
                actionOnRelease: OnReleaseBall,
                actionOnDestroy: OnDestroyBall,
                collectionCheck: true,
                defaultCapacity: initialPoolSize,
                maxSize:         maxPoolSize
            );

            // 事前確保
            var prewarm = new PollenBall[initialPoolSize];
            for (int i = 0; i < initialPoolSize; i++) prewarm[i] = _pool.Get();
            for (int i = 0; i < initialPoolSize; i++) _pool.Release(prewarm[i]);
        }

        private void OnDestroy()
        {
            _pool?.Clear();
        }

        private void Update()
        {
            // ---- 寿命チェック ----
            _lifetimeTimer += Time.deltaTime;
            if (_lifetimeTimer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // ---- 発射チェック ----
            _shotTimer += Time.deltaTime;
            if (_shotTimer >= shotInterval)
            {
                _shotTimer = 0f;
                FireShot();
            }
        }

        // ================================================================
        //  発射処理
        // ================================================================

        /// <summary>shotCount 方向へ均等に PollenBall を発射する。</summary>
        private void FireShot()
        {
            if (pollenBallPrefab == null)
            {
                Debug.LogWarning("[PollenTree] pollenBallPrefab が未設定です。");
                return;
            }

            float step = 360f / shotCount;

            for (int i = 0; i < shotCount; i++)
            {
                float   angleDeg = angleOffset + step * i;
                float   angleRad = angleDeg * Mathf.Deg2Rad;
                Vector2 dir      = new Vector2(Mathf.Sin(angleRad), Mathf.Cos(angleRad));

                PollenBall ball          = _pool.Get();
                ball.transform.position  = transform.position;
                ball.Init(_pool, dir, ballSpeed, swayAmplitude, swayFrequency,
                          pollenForPlayer, pollenForMob);
            }
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

        private static void OnGetBall(PollenBall ball)     => ball.gameObject.SetActive(true);
        private static void OnReleaseBall(PollenBall ball) => ball.gameObject.SetActive(false);
        private static void OnDestroyBall(PollenBall ball)
        {
            if (ball != null) Destroy(ball.gameObject);
        }

        // ================================================================
        //  Gizmo
        // ================================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 発射方向を可視化
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
            float step = 360f / Mathf.Max(1, shotCount);
            for (int i = 0; i < shotCount; i++)
            {
                float   angleDeg = angleOffset + step * i;
                float   angleRad = angleDeg * Mathf.Deg2Rad;
                Vector3 dir      = new Vector3(Mathf.Sin(angleRad), Mathf.Cos(angleRad), 0f);
                Gizmos.DrawRay(transform.position, dir * 1.2f);
            }

            // 寿命の残り時間をラベル表示
            if (Application.isPlaying)
            {
                float remaining = lifetime - _lifetimeTimer;
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * 0.6f,
                    $"{remaining:F1}s");
            }
        }
#endif
    }
}
