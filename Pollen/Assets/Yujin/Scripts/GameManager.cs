using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Pollen;

/// <summary>
/// Manages game timer, win and lose conditions.
///
/// WIN  — timer reaches 0.
/// LOSE — pollen stays at max for 10 consecutive seconds.
///
/// Passes the result to ResultUI via a static flag before loading the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Timer")]
    [Tooltip("Match duration in seconds.")]
    public float gameDuration = 60f;

    [Tooltip("(Optional) TMP label that shows the remaining time.")]
    public TMP_Text timerLabel;

    [Header("Lose Condition")]
    [Tooltip("How many seconds the pollen must stay at max before triggering a loss.")]
    public float maxPollenLoseTime = 10f;

    [Tooltip("(Optional) TMP label that shows the max-pollen danger timer counting down.")]
    public TMP_Text dangerLabel;

    [Header("Pollen")]
    [Tooltip("The PollenGauge component on the player.")]
    public PollenGauge pollenGauge;

    [Header("Scenes")]
    [Tooltip("Exact name of your Result scene (must match Build Settings).")]
    public string resultSceneName = "Result";

    // ── Static result flag — read by ResultUI after scene load ────────────────
    public static bool PlayerWon = false;

    // ── private ───────────────────────────────────────────────────────────────
    private float _timeLeft;
    private float _maxPollenTimer;   // counts up while pollen is full
    private bool _ended;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _timeLeft = gameDuration;
        _maxPollenTimer = 0f;
        _ended = false;
    }

    void Update()
    {
        if (_ended) return;

        // ── Main countdown ────────────────────────────────────────────────────
        _timeLeft -= Time.deltaTime;

        if (timerLabel != null)
            timerLabel.text = FormatTime(_timeLeft);

        // ── Lose condition: pollen at max ─────────────────────────────────────
        if (pollenGauge != null && pollenGauge.IsFull)
        {
            _maxPollenTimer += Time.deltaTime;

            if (dangerLabel != null)
            {
                float remaining = Mathf.Max(0f, maxPollenLoseTime - _maxPollenTimer);
                dangerLabel.text = $"DANGER: {remaining:F1}s";
            }

            if (_maxPollenTimer >= maxPollenLoseTime)
            {
                EndGame(win: false);
                return;
            }
        }
        else
        {
            // Reset danger timer as soon as pollen drops below max
            _maxPollenTimer = 0f;

            if (dangerLabel != null)
                dangerLabel.text = string.Empty;
        }

        // ── Win condition: timer runs out ─────────────────────────────────────
        if (_timeLeft <= 0f)
        {
            _timeLeft = 0f;
            EndGame(win: true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    void EndGame(bool win)
    {
        _ended = true;
        PlayerWon = win;
        SceneManager.LoadScene(resultSceneName);
    }

    // ─────────────────────────────────────────────────────────────────────────
    static string FormatTime(float seconds)
    {
        seconds = Mathf.Max(0f, seconds);
        int m = (int)(seconds / 60);
        int s = (int)(seconds % 60);
        return $"{m:00}:{s:00}";
    }
}