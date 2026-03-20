using UnityEngine;

namespace Mob
{
    /// <summary>
    /// Mob の司令塔。
    /// PatrolBehavior / ChaseBehavior を切り替えながら
    /// MobAnimator・PollenGauge と協調する。
    ///
    /// ---- GameObject 構成 ----
    /// [MobRoot]
    ///   ├─ MobController   (このスクリプト)
    ///   ├─ PatrolBehavior
    ///   ├─ ChaseBehavior
    ///   ├─ MobAnimator
    ///   └─ PollenGauge
    /// </summary>
    [RequireComponent(typeof(PatrolBehavior))]
    [RequireComponent(typeof(ChaseBehavior))]
    [RequireComponent(typeof(MobAnimator))]
    [RequireComponent(typeof(Pollen.PollenGauge))]
    public class MobController : MonoBehaviour
    {
        [Header("Initial Mode")]
        [SerializeField] private MobMode initialMode = MobMode.Patrol;

        // ---- Components ----
        private PatrolBehavior _patrol;
        private ChaseBehavior  _chase;
        private MobAnimator    _mobAnimator;
        private Pollen.PollenGauge    _pollenGauge;

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
            _pollenGauge = GetComponent<Pollen.PollenGauge>();
        }

        private void Start()
        {
            // 花粉ゲージのイベント購読
            _pollenGauge.OnDepleted += HandlePollenDepleted;
            _pollenGauge.OnChanged  += HandlePollenChanged;

            // 初期モード適用
            SwitchMode(initialMode);
        }

        private void Update()
        {
            _currentBehavior?.Execute();
        }

        private void OnDestroy()
        {
            if (_pollenGauge != null)
            {
                _pollenGauge.OnDepleted -= HandlePollenDepleted;
                _pollenGauge.OnChanged  -= HandlePollenChanged;
            }
        }

        // ========== Public API ==========

        /// <summary>Mob の行動モードを切り替える。</summary>
        public void SwitchMode(MobMode newMode)
        {
            if (_currentMode == newMode && _currentBehavior != null) return;

            _currentMode = newMode;
            _currentBehavior = newMode switch
            {
                MobMode.Patrol => _patrol,
                MobMode.Chase  => _chase,
                _              => _patrol
            };

            _currentBehavior.OnModeChanged();
            Debug.Log($"[MobController] Mode → {newMode}");
        }

        // ---- 花粉ゲージ操作（外部から呼ぶ用） ----

        public void AddPollen(float amount)    => _pollenGauge.Add(amount);
        public void ReducePollen(float amount) => _pollenGauge.Reduce(amount);

        // ========== Private Callbacks ==========

        private void HandlePollenDepleted()
        {
            Debug.Log("[MobController] 花粉ゲージが 0 になりました。");
            // 例: 死亡演出・非アクティブ化など
            // gameObject.SetActive(false);
        }

        private void HandlePollenChanged(float current, float max)
        {
            // UI や VFX への通知などを記述
            // Debug.Log($"[MobController] 花粉: {current}/{max} ({_pollenGauge.Ratio:P0})");
            if (Mathf.Approximately(current, max))
            {
                SwitchMode(MobMode.Chase);
            }
        }
    }
}
