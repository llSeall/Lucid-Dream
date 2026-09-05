using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    /// <summary>
    /// กดปุ่มเล่นใหม่หลังตายตอนกลางคืน -> โหลด Checkpoint กลางคืนของคืนเดิม
    /// </summary>
    public void RestartGame()
    {
        Time.timeScale = 1f;

        if (SaveManager.Instance != null)
        {
            // ✨ โหลดเซฟโดยไม่ผ่าน Main Menu (isFromMainMenu = false) เพื่อให้เริ่มตอนกลางคืนของคืนนั้น
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