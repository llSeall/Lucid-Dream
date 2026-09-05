//using UnityEngine;
//using UnityEngine.SceneManagement;

//public class GameOverManager : MonoBehaviour
//{
//    // ฟังก์ชันสำหรับผูกกับปุ่ม Restart ใน UI
//    public void RestartGame()
//    {
//        Time.timeScale = 1f; // คืนค่าเวลาเกมให้กลับมาเดินตามปกติ

//        // โหลดซีนปัจจุบันใหม่
//        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
//    }

//    // (แถม) ฟังก์ชันสำหรับกดกลับหน้าเมนูหลัก
//    public void GoToMainMenu(string mainMenuSceneName)
//    {
//        Time.timeScale = 1f;
//        SceneManager.LoadScene(mainMenuSceneName);
//    }
//}