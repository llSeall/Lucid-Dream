using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public void RestartGame()
    {
        Time.timeScale = 1f;

        // ✨ เพิ่มค่าความเครียดทุกครั้งที่กด Restart
        if (StressManager.Instance != null)
        {
            StressManager.Instance.IncreaseStressOnGameOver();
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame(SaveManager.Instance.currentSlot, isFromMainMenu: false);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void GoToMainMenu(string mainMenuSceneName = "MainMenuScene")
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}