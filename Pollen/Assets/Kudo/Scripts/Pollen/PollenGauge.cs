using System;
using UnityEngine;

namespace Pollen
{
    /// <summary>
    /// 花粉ゲージ（体力ゲージ）の増減を管理する。
    /// 値は [0, MaxPollen] にクランプされる。
    /// </summary>
    public class PollenGauge : MonoBehaviour
    {
        [Header("Pollen Gauge Settings")]
        [SerializeField] private float maxPollen     = 100f;
        [SerializeField] private float initialPollen = 100f;

        // ---- Properties ----

        public float MaxPollen     => maxPollen;
        public float CurrentPollen { get; private set; }

        /// <summary>現在値を 0〜1 に正規化した比率。</summary>
        public float Ratio => maxPollen > 0f ? CurrentPollen / maxPollen : 0f;

        public bool IsEmpty => CurrentPollen <= 0f;
        public bool IsFull  => CurrentPollen >= maxPollen;

        // ---- Events ----

        /// <summary>値が変化したときに発火。引数: (current, max)</summary>
        public event Action<float, float> OnChanged;

        /// <summary>ゲージが 0 になったときに発火。</summary>
        public event Action OnDepleted;

        // ========== Unity ==========

        private void Awake()
        {
            CurrentPollen = Mathf.Clamp(initialPollen, 0f, maxPollen);
        }

        // ========== Public API ==========

        /// <summary>花粉ゲージを増加させる。</summary>
        public void Add(float amount)
        {
            if (amount <= 0f) return;
            ApplyDelta(amount);
        }

        /// <summary>花粉ゲージを減少させる。</summary>
        public void Reduce(float amount)
        {
            if (amount <= 0f) return;
            ApplyDelta(-amount);
        }

        /// <summary>ゲージを満タンにリセットする。</summary>
        public void Restore()
        {
            CurrentPollen = maxPollen;
            OnChanged?.Invoke(CurrentPollen, maxPollen);
        }

        // ========== Private ==========

        private void ApplyDelta(float delta)
        {
            float prev    = CurrentPollen;
            CurrentPollen = Mathf.Clamp(CurrentPollen + delta, 0f, maxPollen);

            if (Mathf.Approximately(prev, CurrentPollen)) return;

            OnChanged?.Invoke(CurrentPollen, maxPollen);

            if (IsEmpty)
                OnDepleted?.Invoke();
        }
    }
}
