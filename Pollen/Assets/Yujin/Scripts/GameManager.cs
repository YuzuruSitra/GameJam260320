using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Attach to a persistent manager object in the Game scene.
/// Counts down 1 minute then loads the Result scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Timer")]
    [Tooltip("Match duration in seconds.")]
    public float gameDuration = 60f;

    [Tooltip("(Optional) TMP label that shows the remaining time.")]
    public TMP_Text timerLabel;

    [Header("Scenes")]
    [Tooltip("Exact name of your Result scene (must match Build Settings).")]
    public string resultSceneName = "Result";

    private float _timeLeft;
    private bool _ended;

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        _timeLeft = gameDuration;
        _ended = false;
    }

    void Update()
    {
        if (_ended) return;

        _timeLeft -= Time.deltaTime;

        if (timerLabel != null)
            timerLabel.text = FormatTime(_timeLeft);

        if (_timeLeft <= 0f)
        {
            _ended = true;
            _timeLeft = 0f;
            SceneManager.LoadScene(resultSceneName);
        }
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