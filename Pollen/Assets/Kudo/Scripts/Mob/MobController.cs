using Pollen;
using UnityEngine;

namespace Mob
{
    /// <summary>
    /// Mob の司令塔。
    /// モード切替時に前の Behavior の OnExit() → 次の Behavior の OnEnter() を
    /// 確実に呼ぶことでコルーチンの停止漏れを防ぐ。
    ///
    /// ---- モード自動切替ルール ----
    ///   Patrol 中に花粉ゲージが満タン → Chase へ一方的に遷移（以降は戻らない）
    ///
    /// ---- GameObject 構成 ----
    /// [MobRoot]
    ///   ├─ MobController
    ///   ├─ PatrolBehavior
    ///   ├─ ChaseBehavior
    ///   ├─ MobAnimator
    ///   ├─ PollenGauge
    ///   └─ MobPollenGaugeUI
    /// </summary>
    [RequireComponent(typeof(PatrolBehavior))]
    [RequireComponent(typeof(ChaseBehavior))]
    [RequireComponent(typeof(MobAnimator))]
    [RequireComponent(typeof(PollenGauge))]
    public class MobController : MonoBehaviour
    {
        [Header("Initial Mode")]
        [SerializeField] private MobMode initialMode = MobMode.Patrol;

        // ---- Components ----
        private PatrolBehavior   _patrol;
        private ChaseBehavior    _chase;
        private MobAnimator      _mobAnimator;
        private PollenGauge      _pollenGauge;
        private MobPollenGaugeUI _gaugeUI;

        // ---- State ----
        private IMobBehavior _currentBehavior;
        private MobMode      _currentMode;

        public MobMode CurrentMode => _currentMode;

        // ========== Unity ==========

        private void Awake()
        {
            _patrol      = GetComponent<PatrolBehavior>();
            _chase       = GetComponent<ChaseBehavior>();
            _mobAnimator = GetComponent<MobAnimator>();
            _pollenGauge = GetComponent<PollenGauge>();
            _gaugeUI     = GetComponent<MobPollenGaugeUI>();
        }

        private void Start()
        {
            _pollenGauge.OnChanged += HandlePollenChanged;
            SwitchMode(initialMode);
        }

        private void Update()
        {
            _currentBehavior?.Execute();
        }

        private void OnDestroy()
        {
            if (_pollenGauge != null)
                _pollenGauge.OnChanged -= HandlePollenChanged;
        }

        // ========== Public API ==========

        /// <summary>
        /// Mob の行動モードを切り替える。
        /// 1. 現在の Behavior の OnExit() を呼んでコルーチン等を停止
        /// 2. 次の Behavior の OnEnter() を呼んで初期化
        /// </summary>
        public void SwitchMode(MobMode newMode)
        {
            if (_currentMode == newMode && _currentBehavior != null) return;

            // --- 現在の Behavior を終了 ---
            _currentBehavior?.OnExit();

            // --- 次の Behavior を開始 ---
            _currentMode     = newMode;
            _currentBehavior = newMode switch
            {
                MobMode.Patrol => _patrol,
                MobMode.Chase  => _chase,
                _              => _patrol
            };

            _currentBehavior.OnEnter();
            Debug.Log($"[MobController] Mode → {newMode}");
        }

        public void AddPollen(float amount)    => _pollenGauge.Add(amount);
        public void ReducePollen(float amount) => _pollenGauge.Reduce(amount);

        // ========== Private Callbacks ==========

        private void HandlePollenChanged(float current, float max)
        {
            if (!_pollenGauge.IsFull) return;

            Debug.Log("[MobController] 花粉ゲージ満タン → Chase 開始（永続）");

            // 以降のゲージ変動を関知しない
            _pollenGauge.OnChanged -= HandlePollenChanged;

            // UI を非表示
            if (_gaugeUI != null)
                _gaugeUI.AutoCreateUI = false;

            SwitchMode(MobMode.Chase);
        }
    }
}
