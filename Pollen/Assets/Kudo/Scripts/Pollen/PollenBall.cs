using UnityEngine;
using UnityEngine.Pool;

namespace Pollen
{
    /// <summary>
    /// 花粉の玉 1 個の挙動を管理する。
    ///
    /// 動作:
    ///   ・設定速度で直進方向に移動
    ///   ・直進方向に対して垂直方向にサインカーブで揺れる
    ///   ・Mob / Player に接触したら花粉ゲージを加算してプールへ返却
    ///   ・カメラ外に出たらプールへ返却
    ///
    /// PollenSpawner が Get() 後に Init() を呼んで初期化する。
    /// 消滅時は Destroy せず Release() でプールへ戻す。
    ///
    /// ---- 当たり判定の取得方法 ----
    /// Collider2D の GameObject と PollenGauge の GameObject が
    /// 必ずしも同一とは限らないため、GetComponentInParent で親階層まで検索する。
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

        // ---- 内部状態 ----
        private Vector2 _perpendicular;
        private Vector2 _basePosition;
        private float   _elapsed;

        private IObjectPool<PollenBall> _pool;

        // ========== 公開 API ==========

        public void Init(
            IObjectPool<PollenBall> pool,
            Vector2 moveDirection,
            float   moveSpeed,
            float   swayAmplitude,
            float   swayFrequency,
            float   pollenForPlayer,
            float   pollenForMob)
        {
            _pool             = pool;
            _moveDirection    = moveDirection.normalized;
            _moveSpeed        = moveSpeed;
            _swayAmplitude    = swayAmplitude;
            _swayAngularSpeed = 2f * Mathf.PI * swayFrequency;
            _swayPhaseOffset  = Random.Range(0f, 2f * Mathf.PI);
            _pollenForPlayer  = pollenForPlayer;
            _pollenForMob     = pollenForMob;

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
            // コライダーが子 GameObject にある構成に対応するため
            // GetComponentInParent で親階層まで含めて PollenGauge を検索する
            if (other.CompareTag(TagPlayer))
            {
                var gauge = other.GetComponentInParent<PollenGauge>();
                if (gauge != null)
                    gauge.Add(_pollenForPlayer);
                else
                    Debug.LogWarning($"[PollenBall] '{other.name}' およびその親に PollenGauge が見つかりません。");

                ReturnToPool();
                return;
            }

            if (other.CompareTag(TagMob))
            {
                var gauge = other.GetComponentInParent<PollenGauge>();
                if (gauge != null)
                    gauge.Add(_pollenForMob);
                else
                    Debug.LogWarning($"[PollenBall] '{other.name}' およびその親に PollenGauge が見つかりません。");

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
