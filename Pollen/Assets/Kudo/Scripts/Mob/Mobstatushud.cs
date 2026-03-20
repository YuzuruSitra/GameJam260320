using UnityEngine;
using UnityEngine.UI;

namespace Mob
{
    /// <summary>
    /// 画面上部中央に感染数 / 生成数をリアルタイム表示するシックな HUD。
    ///
    /// ---- ビジュアル仕様 ----
    ///   半透明の暗いパネル上に
    ///     ラベル（小文字）
    ///     数値（大）/ 合計（小）
    ///     感染率バー（パネル下部）
    ///   を表示。
    ///   全員感染時は数値が点滅してアラート色に変わる。
    ///
    /// ---- 設計方針 ----
    ///   UnityEngine.UI のみ使用（TextMeshPro 不使用）。
    ///   Update で毎フレーム値を取得し、変化があればテキストとバーだけ更新する。
    ///   Canvas / Image / Text をコードで自動生成するため手動配置は不要。
    /// </summary>
    public class MobStatusHUD : MonoBehaviour
    {
        // ================================================================
        //  インスペクタ設定
        // ================================================================

        [Header("Layout")]
        [Tooltip("パネル幅（px）")]
        [SerializeField] private float panelWidth  = 180f;
        [Tooltip("パネル高さ（px）")]
        [SerializeField] private float panelHeight = 72f;
        [Tooltip("画面上端からのオフセット（px）")]
        [SerializeField] private float topOffset   = 16f;

        [Header("Colors")]
        [SerializeField] private Color panelBg        = new Color(0.06f, 0.05f, 0.08f, 0.90f);
        [SerializeField] private Color panelBorder     = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] private Color labelColor      = new Color(1f, 1f, 1f, 0.38f);
        [SerializeField] private Color valueNormal     = new Color(0.91f, 0.79f, 0.48f, 1f);   // ゴールド
        [SerializeField] private Color valueAllInfected = new Color(0.88f, 0.38f, 0.38f, 1f);  // 赤
        [SerializeField] private Color totalColor      = new Color(1f, 1f, 1f, 0.30f);
        [SerializeField] private Color barBg           = new Color(1f, 1f, 1f, 0.08f);

        [Header("Font Size")]
        [Tooltip("ラベルのフォントサイズ（px）")]
        [SerializeField] private int labelFontSize = 10;
        [Tooltip("感染数（大）のフォントサイズ（px）")]
        [SerializeField] private int valueFontSize = 28;
        [Tooltip("合計数（小）のフォントサイズ（px）")]
        [SerializeField] private int totalFontSize = 13;

        [Header("Alert Pulse")]
        [Tooltip("全員感染時に点滅させる速度（Hz）")]
        [SerializeField] private float pulseFrequency = 1.4f;

        // ================================================================
        //  内部参照
        // ================================================================

        private Text  _infectedText;
        private Text  _totalText;
        private Image _barFill;

        private int   _lastInfected = -1;
        private int   _lastTotal    = -1;
        private bool  _isAllInfected;
        private float _pulseTimer;

        // ================================================================
        //  Unity
        // ================================================================

        private void Start()
        {
            BuildUI();
        }

        private void Update()
        {
            int infected = MobRegistry.Instance != null ? MobRegistry.Instance.Count         : 0;
            int total    = MobGenerator.Instance != null ? MobGenerator.Instance.SpawnedCount : 0;

            // 値が変化したときだけ UI を更新
            if (infected != _lastInfected || total != _lastTotal)
            {
                _lastInfected  = infected;
                _lastTotal     = total;
                _isAllInfected = total > 0 && infected >= total;

                RefreshDisplay(infected, total);
            }

            // 全員感染時：数値を点滅
            if (_isAllInfected)
            {
                _pulseTimer += Time.deltaTime * pulseFrequency * 2f * Mathf.PI;
                float alpha = Mathf.Lerp(0.5f, 1f, (Mathf.Sin(_pulseTimer) + 1f) * 0.5f);
                _infectedText.color = new Color(
                    valueAllInfected.r,
                    valueAllInfected.g,
                    valueAllInfected.b,
                    alpha);
            }
        }

        // ================================================================
        //  表示更新
        // ================================================================

        private void RefreshDisplay(int infected, int total)
        {
            _infectedText.text  = infected.ToString();
            _totalText.text     = $"/ {total}";
            _infectedText.color = _isAllInfected ? valueAllInfected : valueNormal;

            // バーの幅を比率で更新
            float ratio = total > 0 ? Mathf.Clamp01((float)infected / total) : 0f;
            var barRt   = _barFill.rectTransform;
            barRt.anchorMax = new Vector2(ratio, 1f);
        }

        // ================================================================
        //  UI 自動生成
        // ================================================================

        private void BuildUI()
        {
            // ---- Canvas（Screen Space Overlay） ----
            var canvasGo = new GameObject("MobStatusHUD_Canvas");
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // ---- パネル（背景） ----
            var panelGo    = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = panelBg;

            var panelRt         = panelImage.rectTransform;
            panelRt.anchorMin   = new Vector2(0.5f, 1f);
            panelRt.anchorMax   = new Vector2(0.5f, 1f);
            panelRt.pivot       = new Vector2(0.5f, 1f);
            panelRt.sizeDelta   = new Vector2(panelWidth, panelHeight);
            panelRt.anchoredPosition = new Vector2(0f, -topOffset);

            // ---- 枠線（アウトライン用の細い Image） ----
            AddOutline(panelGo.transform, panelWidth, panelHeight, panelBorder);

            // ---- ラベル「感染数」----
            var labelText        = AddText(panelGo.transform, "LabelInfected", "感染数");
            labelText.fontSize   = labelFontSize;
            labelText.color      = labelColor;
            labelText.alignment  = TextAnchor.MiddleCenter;
            labelText.fontStyle  = FontStyle.Normal;
            LayoutRT(labelText.rectTransform,
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -8f),
                new Vector2(panelWidth, 16f));

            // ---- 感染数（大） ----
            _infectedText        = AddText(panelGo.transform, "InfectedValue", "0");
            _infectedText.fontSize  = valueFontSize;
            _infectedText.color     = valueNormal;
            _infectedText.fontStyle = FontStyle.Bold;
            _infectedText.alignment = TextAnchor.MiddleRight;
            LayoutRT(_infectedText.rectTransform,
                new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-4f, -4f),
                new Vector2(0f, 32f));

            // ---- 合計数（小） ----
            _totalText          = AddText(panelGo.transform, "TotalValue", "/ 0");
            _totalText.fontSize = totalFontSize;
            _totalText.color    = totalColor;
            _totalText.alignment = TextAnchor.MiddleLeft;
            LayoutRT(_totalText.rectTransform,
                new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(4f, -4f),
                new Vector2(0f, 32f));

            // ---- 感染率バー（背景） ----
            var barBgGo    = new GameObject("BarBg");
            barBgGo.transform.SetParent(panelGo.transform, false);
            var barBgImage = barBgGo.AddComponent<Image>();
            barBgImage.color = barBg;
            LayoutRT(barBgImage.rectTransform,
                Vector2.zero, new Vector2(1f, 0f),
                Vector2.zero,
                Vector2.zero,
                new Vector2(0f, 3f));

            // ---- 感染率バー（Fill） ----
            var barFillGo   = new GameObject("BarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            _barFill        = barFillGo.AddComponent<Image>();
            _barFill.color  = valueNormal;

            var fillRt      = _barFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.zero;   // 初期は 0%
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }

        // ================================================================
        //  ユーティリティ
        // ================================================================

        /// <summary>パネル外枠を細い Image で描画する。</summary>
        private static void AddOutline(Transform parent, float w, float h, Color color)
        {
            var go    = new GameObject("Outline");
            go.transform.SetParent(parent, false);
            var img   = go.AddComponent<Image>();
            img.color = color;

            // Outline は 1px の細線なので、Mask で内側を抜く
            var rt          = img.rectTransform;
            rt.anchorMin    = Vector2.zero;
            rt.anchorMax    = Vector2.one;
            rt.offsetMin    = new Vector2(-1f, -1f);
            rt.offsetMax    = new Vector2(1f, 1f);

            // 内側を透明にするため Mask + 内側 Image を置く
            go.AddComponent<Mask>().showMaskGraphic = true;
            var innerGo     = new GameObject("OutlineInner");
            innerGo.transform.SetParent(go.transform, false);
            var inner       = innerGo.AddComponent<Image>();
            inner.color     = Color.clear;
            var innerRt     = inner.rectTransform;
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(1f, 1f);
            innerRt.offsetMax = new Vector2(-1f, -1f);
        }

        private static Text AddText(Transform parent, string goName, string content)
        {
            var go   = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static void LayoutRT(
            RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPos,
            Vector2 sizeDelta)
        {
            rt.anchorMin        = anchorMin;
            rt.anchorMax        = anchorMax;
            rt.pivot            = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta        = sizeDelta;
        }
    }
}
