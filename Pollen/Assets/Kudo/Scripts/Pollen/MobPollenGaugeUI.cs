using UnityEngine;
using UnityEngine.UI;

namespace Pollen
{
    /// <summary>
    /// Mob の頭上に World Space Canvas で花粉ゲージを描画する。
    ///
    /// ---- Hierarchy 構成（手動またはスクリプトで自動生成） ----
    ///
    ///   [MobRoot]
    ///     └─ PollenGaugeUI          Canvas (World Space, このスクリプトをアタッチ)
    ///          └─ Background        Image  背景・枠
    ///               └─ MaskRoot     Image + Mask  ← ゲージ形状で切り抜く
    ///                    └─ Fill    Image  ← RectTransform を左右スライドして増減
    ///
    /// ---- Mask の動作 ----
    ///   MaskRoot の Image がマスク形状を定義する。
    ///   Fill は MaskRoot と同じサイズの矩形で、右端を左方向に縮めることで
    ///   「左から右へ伸びるゲージ」を Mask で切り抜いて表現する。
    ///   → Fill 自体のサイズは変えず offsetMax.x だけを動かすため
    ///      縦方向の歪みが発生しない。
    ///
    /// ---- セットアップ ----
    ///   1. MobRoot に本スクリプトをアタッチ
    ///   2. PollenGauge コンポーネントが同じ GameObject にあること
    ///   3. Start() で Hierarchy を自動生成するため手動配置は不要
    ///      （既存の Canvas を使いたい場合は autoCreateUI = false にして
    ///        各参照を手動でアサイン）
    /// </summary>
    [RequireComponent(typeof(PollenGauge))]
    public class MobPollenGaugeUI : MonoBehaviour
    {
        // ================================================================
        //  インスペクタ設定
        // ================================================================

        [Header("Auto Build UI")]
        [Tooltip("true: Start() で Hierarchy を自動生成 / false: 手動アサイン")]
        [SerializeField] private bool autoCreateUI = true;

        [Header("Manual References (autoCreateUI = false の時のみ使用)")]
        [SerializeField] private RectTransform fillRect;   // Fill の RectTransform

        [Header("Layout")]
        [Tooltip("Mob 頭上へのオフセット（World 座標）")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 0.8f, 0f);

        [Tooltip("ゲージ全体の幅（World 単位）")]
        [SerializeField] private float gaugeWidth = 1.0f;

        [Tooltip("ゲージ全体の高さ（World 単位）")]
        [SerializeField] private float gaugeHeight = 0.15f;

        [Header("Colors")]
        [SerializeField] private Color backgroundColor = new Color(0.15f, 0.10f, 0.05f, 0.85f);
        [SerializeField] private Color fillColorFull   = new Color(1.00f, 0.85f, 0.20f, 1.00f);
        [SerializeField] private Color fillColorEmpty  = new Color(0.90f, 0.30f, 0.10f, 1.00f);

        [Header("Fill Padding (Background 内側の余白)")]
        [SerializeField] private float padX = 0.04f;
        [SerializeField] private float padY = 0.04f;

        [Header("Smooth")]
        [Tooltip("ゲージが目標値へ追従する速度（0 で即時）")]
        [SerializeField] private float smoothSpeed = 8f;

        // ================================================================
        //  内部参照
        // ================================================================

        private PollenGauge _pollenGauge;
        private Image       _fillImage;
        private Canvas      _canvas;

        private float _targetRatio  = 1f;
        private float _currentRatio = 1f;

        // Fill の左右端が MaskRoot に対してどれだけ内側に入るか（px）
        // autoCreateUI では Canvas pixels-per-unit = 100 前提で計算
        private float _fillFullWidth;   // Mask 幅 px（= Fill が 100% のときの右端）

        // ================================================================
        //  Unity
        // ================================================================

        private void Awake()
        {
            _pollenGauge = GetComponent<PollenGauge>();
        }

        private void Start()
        {
            if (autoCreateUI)
                BuildUI();

            // 初期値を即時反映（Smooth なし）
            _targetRatio  = _pollenGauge.Ratio;
            _currentRatio = _targetRatio;
            ApplyRatio(_currentRatio);

            // イベント購読
            _pollenGauge.OnChanged += OnPollenChanged;
        }

        private void OnDestroy()
        {
            if (_pollenGauge != null)
                _pollenGauge.OnChanged -= OnPollenChanged;
        }

        private void LateUpdate()
        {
            // Mob の動きに追従（Canvas は子 GameObject なので自動追従するが
            // World Offset はここで補正）
            if (_canvas != null)
                _canvas.transform.localPosition = worldOffset;

            // Smooth 追従
            if (smoothSpeed > 0f)
                _currentRatio = Mathf.Lerp(_currentRatio, _targetRatio, Time.deltaTime * smoothSpeed);
            else
                _currentRatio = _targetRatio;

            ApplyRatio(_currentRatio);
        }

        // ================================================================
        //  UI 自動生成
        // ================================================================

        /// <summary>
        /// World Space Canvas → Background → MaskRoot → Fill を
        /// コードで生成してアタッチする。
        /// </summary>
        private void BuildUI()
        {
            const float ppu = 100f;   // pixels per unit

            // ---- Canvas ----
            var canvasGo = new GameObject("PollenGaugeUI");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = worldOffset;

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            float w = gaugeWidth  * ppu;
            float h = gaugeHeight * ppu;
            canvasRt.sizeDelta = new Vector2(w, h);
            canvasRt.localScale = Vector3.one / ppu;
            canvasRt.anchoredPosition = Vector2.zero;

            // CanvasScaler は不要（World Space では機能しない）
            // GraphicRaycaster も不要（UI 操作なし）

            // ---- Background ----
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color = backgroundColor;
            StretchFull(bgImage.rectTransform);

            // ---- MaskRoot（Mask の形状 Image + Mask コンポーネント） ----
            var maskGo = new GameObject("MaskRoot");
            maskGo.transform.SetParent(bgGo.transform, false);

            var maskImage = maskGo.AddComponent<Image>();
            maskImage.color = Color.white;   // Mask 自体は描画しない（showMaskGraphic = false）

            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;    // 枠画像を表示しないので false

            // MaskRoot は Background 内側の余白分だけ小さくする
            float pxPadX = padX * ppu;
            float pxPadY = padY * ppu;
            var maskRt = maskImage.rectTransform;
            StretchFull(maskRt);
            maskRt.offsetMin = new Vector2( pxPadX,  pxPadY);
            maskRt.offsetMax = new Vector2(-pxPadX, -pxPadY);

            // ---- Fill ----
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(maskGo.transform, false);

            _fillImage       = fillGo.AddComponent<Image>();
            _fillImage.color = fillColorFull;

            fillRect = _fillImage.rectTransform;
            StretchFull(fillRect);

            // Fill 幅（px）: MaskRoot の幅 = 全体幅 - padX×2
            _fillFullWidth = w - pxPadX * 2f;
        }

        // ================================================================
        //  表示更新
        // ================================================================

        private void OnPollenChanged(float current, float max)
        {
            _targetRatio = max > 0f ? current / max : 0f;
        }

        /// <summary>
        /// ratio (0〜1) に応じて Fill の右端を動かし、色を補間する。
        /// Mask により MaskRoot 外側は自動的に非表示になる。
        /// </summary>
        private void ApplyRatio(float ratio)
        {
            if (fillRect == null) return;

            ratio = Mathf.Clamp01(ratio);

            // Fill の右端を左方向へオフセット
            // offsetMax.x = 0       → 100%（右端が MaskRoot の右端と一致）
            // offsetMax.x = -width  →   0%（Fill が MaskRoot の外に完全に押し出される）
            float hideAmount = _fillFullWidth * (1f - ratio);
            fillRect.offsetMax = new Vector2(-hideAmount, fillRect.offsetMax.y);

            // 色補間（満タン = fillColorFull, 空 = fillColorEmpty）
            if (_fillImage != null)
                _fillImage.color = Color.Lerp(fillColorEmpty, fillColorFull, ratio);
        }

        // ================================================================
        //  ユーティリティ
        // ================================================================

        /// <summary>RectTransform を親に対してフルストレッチに設定する。</summary>
        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;
            rt.localScale   = Vector3.one;
            rt.localPosition = Vector3.zero;
        }

        // ================================================================
        //  Gizmo
        // ================================================================
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(
                transform.position + worldOffset,
                new Vector3(gaugeWidth, gaugeHeight, 0.01f));
        }
#endif
    }
}
