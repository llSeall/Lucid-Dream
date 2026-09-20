using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    /// <summary>
    /// เรียกใช้เมื่อผู้เล่นแพ้/ตายในความฝัน (ตื่นขึ้นใหม่ในฝันคืนเดิมทันที + เพิ่มความเครียด)
    /// </summary>
    public void RespawnInDream()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDiedInDream();
        }
        else
        {
            // Fallback หากไม่มี GameManager ในฉาก
            if (StressManager.Instance != null)
            {
                StressManager.Instance.IncreaseStressOnDeath();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    public void GoToMainMenu(string mainMenuSceneName = "MainMenuScene")
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}