using Pollen;
using UnityEngine;
using UnityEngine.Pool;

namespace Pollen
{
    /// <summary>
    /// 花粉の玉 1 個の挙動を管理する。
    ///
    /// ---- 当たり判定モード ----
    ///   HitTarget フラグで Player / Mob それぞれへの当たり判定を個別に制御できる。
    ///   PollenTree から発射する場合は hitMob = false にすることで
    ///   Mob には当たらず Player にのみ作用する友好弾として機能する。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PollenBall : MonoBehaviour
    {
        // ---- 当たり判定用タグ ----
        private const string TagPlayer = "Player";
        private const string TagMob    = "Mob";

        // ---- 移動パラメータ ----
        private Vector2 _moveDirection;
        private float   _moveSpeed;
        private float   _swayAmplitude;
        private float   _swayAngularSpeed;
        private float   _swayPhaseOffset;

        // ---- 花粉加算量 ----
        private float _pollenForPlayer;
        private float _pollenForMob;

        // ---- 当たり判定フラグ ----
        private bool _hitPlayer;
        private bool _hitMob;

        // ---- 内部状態 ----
        private Vector2 _perpendicular;
        private Vector2 _basePosition;
        private float   _elapsed;

        private IObjectPool<PollenBall> _pool;

        // ========== 公開 API ==========

        /// <summary>
        /// 初期化メソッド。
        /// hitPlayer / hitMob で当たり判定の対象を個別に制御できる。
        /// デフォルトは両方 true（従来と同じ挙動）。
        /// </summary>
        public void Init(
            IObjectPool<PollenBall> pool,
            Vector2 moveDirection,
            float   moveSpeed,
            float   swayAmplitude,
            float   swayFrequency,
            float   pollenForPlayer,
            float   pollenForMob,
            bool    hitPlayer = true,
            bool    hitMob    = true)
        {
            _pool             = pool;
            _moveDirection    = moveDirection.normalized;
            _moveSpeed        = moveSpeed;
            _swayAmplitude    = swayAmplitude;
            _swayAngularSpeed = 2f * Mathf.PI * swayFrequency;
            _swayPhaseOffset  = Random.Range(0f, 2f * Mathf.PI);
            _pollenForPlayer  = pollenForPlayer;
            _pollenForMob     = pollenForMob;
            _hitPlayer        = hitPlayer;
            _hitMob           = hitMob;

            _perpendicular = new Vector2(-_moveDirection.y, _moveDirection.x);
            _basePosition  = transform.position;
            _elapsed       = 0f;
        }

        // ========== Unity ==========

        private void Update()
        {
            _elapsed      += Time.deltaTime;
            _basePosition += _moveDirection * (_moveSpeed * Time.deltaTime);

            float swayOffset   = _swayAmplitude * Mathf.Sin(_swayAngularSpeed * _elapsed + _swayPhaseOffset);
            transform.position = (Vector3)(_basePosition + _perpendicular * swayOffset);

            if (IsOutOfCamera())
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hitPlayer && other.CompareTag(TagPlayer))
            {
                var gauge = other.GetComponentInParent<PollenGauge>();
                if (gauge != null)
                    gauge.Add(_pollenForPlayer);
                else
                    Debug.LogWarning($"[PollenBall] '{other.name}' に PollenGauge が見つかりません。");

                ReturnToPool();
                return;
            }

            if (_hitMob && other.CompareTag(TagMob))
            {
                var gauge = other.GetComponentInParent<PollenGauge>();
                if (gauge != null)
                    gauge.Add(_pollenForMob);
                else
                    Debug.LogWarning($"[PollenBall] '{other.name}' に PollenGauge が見つかりません。");

                ReturnToPool();
            }
        }

        // ========== Private ==========

        private bool IsOutOfCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;
            Vector3 vp = cam.WorldToViewportPoint(transform.position);
            return vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f;
        }

        private void ReturnToPool()
        {
            if (!gameObject.activeSelf) return;
            _pool?.Release(this);
        }
    }
}
