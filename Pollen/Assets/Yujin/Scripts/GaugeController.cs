using UnityEngine;
using UnityEngine.UI;
using Pollen;

/// <summary>
/// Half-circle pollen gauge using two sprites:
///   - Dial image  : static background
///   - Needle image: Z rotation changes based on CurrentPollen / MaxPollen
///
/// SETUP:
///   1. Add this script to any GameObject inside a Canvas.
///   2. Assign DialImage and NeedleImage in the Inspector.
///   3. Drag the player's PollenGauge into the Gauge field.
///   4. Tune AngleEmpty and AngleFull to match your needle sprite.
/// </summary>
public class PollenGaugeUI : MonoBehaviour
{
    [Header("References")]
    public PollenGauge gauge;
    public Image dialImage;
    public Image needleImage;

    [Header("Needle Angles")]
    [Tooltip("Needle Z rotation when pollen is empty (left side).")]
    public float angleEmpty = 90f;

    [Tooltip("Needle Z rotation when pollen is full (right side).")]
    public float angleFull = -90f;

    [Header("Smoothing")]
    [Tooltip("How fast the needle follows the actual value. Higher = snappier.")]
    public float smoothSpeed = 6f;

    // ── private ──────────────────────────────────────────────────────────────
    private float _displayRatio;

    // ─────────────────────────────────────────────────────────────────────────
    void Awake()
    {
        if (gauge != null)
            _displayRatio = gauge.Ratio;
    }

    void OnEnable()
    {
        if (gauge != null)
            gauge.OnChanged += OnPollenChanged;
    }

    void OnDisable()
    {
        if (gauge != null)
            gauge.OnChanged -= OnPollenChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    void Update()
    {
        float target = gauge != null ? gauge.Ratio : 0f;
        _displayRatio = Mathf.Lerp(_displayRatio, target, Time.deltaTime * smoothSpeed);
        ApplyNeedleRotation(_displayRatio);
    }

    void OnPollenChanged(float current, float max) { }

    // ─────────────────────────────────────────────────────────────────────────
    void ApplyNeedleRotation(float ratio)
    {
        if (needleImage == null) return;
        float angle = Mathf.Lerp(angleEmpty, angleFull, ratio);
        needleImage.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        float ratio = gauge != null ? gauge.Ratio : 0f;
        ApplyNeedleRotation(ratio);
    }
#endif
}