using Pollen;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// プレイヤーが触れると花粉ゲージを回復するアイテム。
    ///
    /// ---- 仕様 ----
    ///   ・"Player" タグの Collider2D（IsTrigger）と接触したら
    ///     プレイヤーの PollenGauge.Reduce() を呼んで回復し、自身を非アクティブ化する
    ///   ・回復量は recoveryAmount で設定
    ///   ・ジェネレーターによるプール管理を想定し Destroy ではなく SetActive(false) で返却
    ///
    /// ---- セットアップ ----
    ///   プレハブに Collider2D（IsTrigger: ON）をアタッチすること。
    ///   Player 側の Collider2D は IsTrigger: OFF の通常コライダーでよい。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PollenRecoveryItem : MonoBehaviour
    {
        private const string TagPlayer = "Player";

        // ================================================================
        //  インスペクタ設定
        // ================================================================

        [Header("Recovery")]
        [Tooltip("プレイヤーの花粉ゲージを回復する量")]
        [SerializeField] private float recoveryAmount = 30f;

        // ================================================================
        //  Public API
        // ================================================================

        /// <summary>インスペクタで設定した回復量。ジェネレーターから上書き可能。</summary>
        public float RecoveryAmount
        {
            get => recoveryAmount;
            set => recoveryAmount = value;
        }

        /// <summary>
        /// ジェネレーターから再配置するときに呼ぶ初期化メソッド。
        /// </summary>
        public void Init(float amount)
        {
            recoveryAmount = amount;
        }

        // ================================================================
        //  Unity
        // ================================================================

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag(TagPlayer)) return;

            if (other.TryGetComponent<PollenGauge>(out var gauge))
                gauge.Reduce(recoveryAmount);

            // ジェネレーター側で管理するため非アクティブ化して返す
            gameObject.SetActive(false);
        }
    }
}
