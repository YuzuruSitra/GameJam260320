using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Reads GameManager.PlayerWon and shows the correct image.
/// Attach to the Result scene's UI manager object.
/// </summary>
public class ResultUI : MonoBehaviour
{
    [Header("Result Images")]
    [Tooltip("Image shown when the player wins (timer ran out).")]
    public Image winImage;

    [Tooltip("Image shown when the player loses (max pollen held too long).")]
    public Image loseImage;

    [Header("Scenes")]
    [Tooltip("Exact name of your Main Menu scene (must match Build Settings).")]
    public string mainMenuSceneName = "MainMenu";

    // ─────────────────────────────────────────────────────────────────────────
    void Start()
    {
        bool won = GameManager.PlayerWon;

        if (winImage != null) winImage.gameObject.SetActive(won);
        if (loseImage != null) loseImage.gameObject.SetActive(!won);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}