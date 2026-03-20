using System;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// 花粉症が発症した（Chase モードに入った）Mob の個体数を追跡するクラス。
    ///
    /// ---- 設計方針 ----
    ///   MobRegistry のイベントを直接購読し、Count を正の値として扱う。
    ///   購読は Awake 後かつ MobRegistry.Instance が確定している Start で行う。
    /// </summary>
    public class PollenSymptomTracker : MonoBehaviour
    {
        // ---- Singleton ----
        public static PollenSymptomTracker Instance { get; private set; }

        // ================================================================
        //  Properties / Events
        // ================================================================

        /// <summary>現在の発症個体数。MobRegistry.Count と常に同期する。</summary>
        public int SymptomCount => MobRegistry.Instance != null ? MobRegistry.Instance.Count : 0;

        /// <summary>発症数が変化したときに発火。引数: 変化後の発症個体数。</summary>
        public event Action<int> OnSymptomCountChanged;

        /// <summary>新たに発症したときに発火。引数: 発症した MobController。</summary>
        public event Action<MobController> OnMobSymptomStarted;

        /// <summary>発症中の Mob が消滅したときに発火。引数: 消滅した MobController。</summary>
        public event Action<MobController> OnMobSymptomEnded;

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
            // Start で購読することで MobRegistry.Instance の確定を保証する
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            if (Instance == this) Instance = null;
        }

        // ================================================================
        //  購読管理
        // ================================================================

        private void Subscribe()
        {
            if (MobRegistry.Instance == null)
            {
                Debug.LogError("[PollenSymptomTracker] MobRegistry.Instance が null です。" +
                               "MobRegistry を先に Awake させてください。");
                return;
            }

            MobRegistry.Instance.OnRegistered   += HandleRegistered;
            MobRegistry.Instance.OnUnregistered += HandleUnregistered;

            Debug.Log($"[PollenSymptomTracker] 購読開始。初期発症数={SymptomCount}");
        }

        private void Unsubscribe()
        {
            if (MobRegistry.Instance == null) return;
            MobRegistry.Instance.OnRegistered   -= HandleRegistered;
            MobRegistry.Instance.OnUnregistered -= HandleUnregistered;
        }

        // ================================================================
        //  イベントハンドラ
        // ================================================================

        private void HandleRegistered(MobController mob)
        {
            OnMobSymptomStarted?.Invoke(mob);
            OnSymptomCountChanged?.Invoke(SymptomCount);
            Debug.Log($"[PollenSymptomTracker] 発症: {mob.name}  発症数={SymptomCount}");
        }

        private void HandleUnregistered(MobController mob)
        {
            OnMobSymptomEnded?.Invoke(mob);
            OnSymptomCountChanged?.Invoke(SymptomCount);
            Debug.Log($"[PollenSymptomTracker] 消滅: {mob.name}  発症数={SymptomCount}");
        }
    }
}
