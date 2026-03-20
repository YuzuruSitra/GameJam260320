using UnityEngine;
using UnityEngine.Pool;

namespace Mob
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
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PollenBall : MonoBehaviour
    {
        // ---- 当たり判定用タグ ----
        private const string TagPlayer = "Player";
        private const string TagMob    = "Mob";

        // ---- 初期化パラメータ（PollenSpawner からセット） ----

        /// <summary>直進方向（正規化済み）</summary>
        private Vector2 _moveDirection;

        /// <summary>直進速度（Units/秒）</summary>
        private float _moveSpeed;

        /// <summary>揺れの振れ幅（Units）</summary>
        private float _swayAmplitude;

        /// <summary>揺れの角速度（rad/秒）。2π × Hz</summary>
        private float _swayAngularSpeed;

        /// <summary>揺れの位相オフセット（生成ごとにランダム化し同期を崩す）</summary>
        private float _swayPhaseOffset;

        /// <summary>Player に当たったときの花粉加算量</summary>
        private float _pollenForPlayer;

        /// <summary>Mob に当たったときの花粉加算量</summary>
        private float _pollenForMob;

        // ---- 内部状態 ----

        /// <summary>直進方向に対して 90° 回転した垂直ベクトル</summary>
        private Vector2 _perpendicular;

        /// <summary>基準位置（揺れの中心線）を毎フレーム直進方向で更新するために使用</summary>
        private Vector2 _basePosition;

        private float _elapsed;

        /// <summary>返却先のプール。PollenSpawner が Init() 前にセットする。</summary>
        private IObjectPool<PollenBall> _pool;

        // ========== 公開 API ==========

        /// <summary>
        /// PollenSpawner から呼ぶ初期化メソッド。
        /// プールから取り出した直後に必ず呼ぶこと。
        /// </summary>
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

            // 直進方向に対して垂直なベクトル（左 90°）
            _perpendicular = new Vector2(-_moveDirection.y, _moveDirection.x);

            _basePosition = transform.position;
            _elapsed      = 0f;
        }

        // ========== Unity ==========

        private void Update()
        {
            _elapsed += Time.deltaTime;

            // 基準位置を直進方向へ進める
            _basePosition += _moveDirection * (_moveSpeed * Time.deltaTime);

            // 垂直方向の揺れオフセット
            float swayOffset = _swayAmplitude * Mathf.Sin(_swayAngularSpeed * _elapsed + _swayPhaseOffset);

            transform.position = (Vector3)(_basePosition + _perpendicular * swayOffset);

            // カメラ外に出たらプールへ返却
            if (IsOutOfCamera())
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(TagPlayer))
            {
                if (other.TryGetComponent<PollenGauge>(out var gauge))
                    gauge.Add(_pollenForPlayer);

                ReturnToPool();
                return;
            }

            if (other.CompareTag(TagMob))
            {
                if (other.TryGetComponent<PollenGauge>(out var gauge))
                    gauge.Add(_pollenForMob);

                ReturnToPool();
            }
        }

        // ========== Private ==========

        /// <summary>カメラのビューポート外（マージン 10%）に出ているか判定する。</summary>
        private bool IsOutOfCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return false;

            Vector3 vp = cam.WorldToViewportPoint(transform.position);
            return vp.x < -0.1f || vp.x > 1.1f || vp.y < -0.1f || vp.y > 1.1f;
        }

        /// <summary>プールへ返却する。二重返却を防ぐため gameObject の状態で判定。</summary>
        private void ReturnToPool()
        {
            if (!gameObject.activeSelf) return;   // すでに非アクティブなら返却済み
            _pool?.Release(this);
        }
    }
}
