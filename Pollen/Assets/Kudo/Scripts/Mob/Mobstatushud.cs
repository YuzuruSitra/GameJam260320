using UnityEngine;
using UnityEngine.UI;

namespace Mob
{
    /// <summary>
    /// HUD displayed at the top of the screen showing infected count vs total.
    /// 
    /// Win condition  — infected count reaches InfectedToWin.
    /// Lose condition — game timer (in GameManager) runs out before that.
    /// </summary>
    public class MobStatusHUD : MonoBehaviour
    {
        // ================================================================
        //  Inspector
        // ================================================================

        [Header("Win Condition")]
        [Tooltip("How many mobs must be infected to trigger a win.")]
        public int infectedToWin = 10;

        [Header("Layout")]
        [SerializeField] private float panelWidth = 360f;
        [SerializeField] private float panelHeight = 144f;
        [SerializeField] private float topOffset = 32f;

        [Header("Colors")]
        [SerializeField] private Color panelBg = new Color(0.06f, 0.05f, 0.08f, 0.90f);
        [SerializeField] private Color panelBorder = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] private Color labelColor = new Color(1f, 1f, 1f, 0.38f);
        [SerializeField] private Color valueNormal = new Color(0.91f, 0.79f, 0.48f, 1f);
        [SerializeField] private Color valueAllInfected = new Color(0.88f, 0.38f, 0.38f, 1f);
        [SerializeField] private Color totalColor = new Color(1f, 1f, 1f, 0.30f);
        [SerializeField] private Color barBg = new Color(1f, 1f, 1f, 0.08f);

        [Header("Font Size")]
        [Tooltip("Single font size used for all text. Adjust until it fits perfectly.")]
        [SerializeField] private int fontSize = 22;

        [Header("Alert Pulse")]
        [Tooltip("Pulse speed in Hz when all mobs are infected.")]
        [SerializeField] private float pulseFrequency = 1.4f;

        // ================================================================
        //  Private refs
        // ================================================================

        private Text _infectedText;
        private Text _totalText;
        private Image _barFill;

        private int _lastInfected = -1;
        private int _lastTotal = -1;
        private bool _isAllInfected;
        private float _pulseTimer;
        private bool _ended;

        // ================================================================
        //  Unity
        // ================================================================

        private void Start()
        {
            BuildUI();
        }

        private void Update()
        {
            if (_ended) return;

            int infected = MobRegistry.Instance != null ? MobRegistry.Instance.Count : 0;
            int total = MobGenerator.Instance != null ? MobGenerator.Instance.SpawnedCount : 0;

            if (infected != _lastInfected || total != _lastTotal)
            {
                _lastInfected = infected;
                _lastTotal = total;
                _isAllInfected = total > 0 && infected >= total;

                RefreshDisplay(infected, total);

                // ── Win: infected count reached the target ────────────────
                if (infected >= infectedToWin)
                {
                    _ended = true;
                    GameManager.PlayerWon = true;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        FindObjectOfType<GameManager>()?.resultSceneName ?? "Result");
                    return;
                }
            }

            // Pulse color when all mobs are infected
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
        //  Display refresh
        // ================================================================

        private void RefreshDisplay(int infected, int total)
        {
            _infectedText.text = infected.ToString();
            _totalText.text = $"/ {infectedToWin}";
            _infectedText.color = _isAllInfected ? valueAllInfected : valueNormal;

            float ratio = infectedToWin > 0 ? Mathf.Clamp01((float)infected / infectedToWin) : 0f;
            var barRt = _barFill.rectTransform;
            barRt.anchorMax = new Vector2(ratio, 1f);
        }

        // ================================================================
        //  UI construction
        // ================================================================

        private void BuildUI()
        {
            // Canvas (Screen Space Overlay)
            var canvasGo = new GameObject("MobStatusHUD_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Panel background
            var panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            var panelImage = panelGo.AddComponent<Image>();
            panelImage.color = panelBg;
            var panelRt = panelImage.rectTransform;
            panelRt.anchorMin = new Vector2(0.5f, 1f);
            panelRt.anchorMax = new Vector2(0.5f, 1f);
            panelRt.pivot = new Vector2(0.5f, 1f);
            panelRt.sizeDelta = new Vector2(panelWidth, panelHeight);
            panelRt.anchoredPosition = new Vector2(0f, -topOffset);

            // Border outline
            AddOutline(panelGo.transform, panelWidth, panelHeight, panelBorder);

            // "INFECTED" top label — stretches full width, top quarter of panel
            var labelText = AddText(panelGo.transform, "LabelInfected", "INFECTED");
            labelText.fontSize = fontSize;
            labelText.color = labelColor;
            labelText.alignment = TextAnchor.MiddleCenter;
            LayoutRT(labelText.rectTransform,
                new Vector2(0f, 0.75f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);

            // Infected count — left half, middle section
            _infectedText = AddText(panelGo.transform, "InfectedValue", "0");
            _infectedText.fontSize = fontSize;
            _infectedText.color = valueNormal;
            _infectedText.fontStyle = FontStyle.Bold;
            _infectedText.alignment = TextAnchor.MiddleRight;
            LayoutRT(_infectedText.rectTransform,
                new Vector2(0f, 0.25f), new Vector2(0.5f, 0.75f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-6f, 0f), Vector2.zero);

            // "/ infectedToWin" — right half, middle section
            _totalText = AddText(panelGo.transform, "TotalValue", $"/ {infectedToWin}");
            _totalText.fontSize = fontSize;
            _totalText.color = totalColor;
            _totalText.alignment = TextAnchor.MiddleLeft;
            LayoutRT(_totalText.rectTransform,
                new Vector2(0.5f, 0.25f), new Vector2(1f, 0.75f),
                new Vector2(0.5f, 0.5f),
                new Vector2(6f, 0f), Vector2.zero);

            // Progress bar background
            var barBgGo = new GameObject("BarBg");
            barBgGo.transform.SetParent(panelGo.transform, false);
            var barBgImage = barBgGo.AddComponent<Image>();
            barBgImage.color = barBg;
            LayoutRT(barBgImage.rectTransform,
                new Vector2(0f, 0f), new Vector2(1f, 0.12f),
                Vector2.zero, Vector2.zero, Vector2.zero);

            // Progress bar fill
            var barFillGo = new GameObject("BarFill");
            barFillGo.transform.SetParent(barBgGo.transform, false);
            _barFill = barFillGo.AddComponent<Image>();
            _barFill.color = valueNormal;
            var fillRt = _barFill.rectTransform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f); // width=0 at start, full height
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
        }

        // ================================================================
        //  Utilities
        // ================================================================

        private static void AddOutline(Transform parent, float w, float h, Color color)
        {
            var go = new GameObject("Outline");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-1f, -1f);
            rt.offsetMax = new Vector2(1f, 1f);
            go.AddComponent<Mask>().showMaskGraphic = true;
            var innerGo = new GameObject("OutlineInner");
            innerGo.transform.SetParent(go.transform, false);
            var inner = innerGo.AddComponent<Image>();
            inner.color = Color.clear;
            var innerRt = inner.rectTransform;
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.offsetMin = new Vector2(1f, 1f);
            innerRt.offsetMax = new Vector2(-1f, -1f);
        }

        private static Text AddText(Transform parent, string goName, string content)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return text;
        }

        private static void LayoutRT(
            RectTransform rt,
            Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = sizeDelta;
        }
    }
}