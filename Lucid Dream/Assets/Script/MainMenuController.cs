using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class MainMenuController : MonoBehaviour
{
    [Header("📋 UI Elements")]
    [SerializeField] private Button continueButton; //[cite: 18]
    [SerializeField] private string firstSceneName = "GameplayScene"; //[cite: 18]

    [Header("สล็อตตั้งต้นสำหรับกด Continue หน้าเมนูหลัก")]
    [Range(1, 3)][SerializeField] private int defaultMenuSlot = 1;

    private void Start()
    {
        CheckSaveFile(); //[cite: 18]
    }

    private void CheckSaveFile()
    {
        if (continueButton == null) return; //[cite: 18]

        if (SaveManager.Instance != null) //[cite: 15, 18]
        {
            // ทำการตรวจเช็คไฟล์ประจำสล็อตที่กำหนดไว้[cite: 15, 18]
            string savePath = Path.Combine(Application.persistentDataPath, $"{SaveManager.Instance.saveFileNamePrefix}{defaultMenuSlot}.json"); //[cite: 15, 18]
            continueButton.interactable = File.Exists(savePath); //[cite: 18]
        }
        else
        {
            continueButton.interactable = false; //[cite: 18]
        }
    }

    public void OnClickNewGame()
    {
        if (SaveManager.Instance != null) //[cite: 15, 18]
        {
            SaveManager.Instance.currentSlot = defaultMenuSlot; //[cite: 15]
            SaveManager.Instance.ClearSave(defaultMenuSlot); // ล้างสล็อตเก่าทิ้งเพื่อเริ่มประวัติศาสตร์ใหม่[cite: 15, 18]
        }
        SceneManager.LoadScene(firstSceneName); //[cite: 18]
    }

    public void OnClickContinue()
    {
        if (SaveManager.Instance != null) //[cite: 15, 18]
        {
            // สั่งโหลดข้อมูลล่าสุดจากสล็อตเมนูหลัก ทะลุมิติกลับไปจุดเซฟดั้งเดิม[cite: 15, 18]
            SaveManager.Instance.LoadGame(defaultMenuSlot, true);
        }
    }

    public void OnClickQuitGame()
    {
        Debug.Log("[MainMenu] ผู้เล่นกดปิดเกม"); //[cite: 18]
        Application.Quit(); //[cite: 18]
    }
}