using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach to the Main Menu scene's UI manager object.
/// Hook the PlayButton's OnClick to this component's StartGame().
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Tooltip("Exact name of your Game scene (must match Build Settings).")]
    public string gameSceneName = "Main";

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}