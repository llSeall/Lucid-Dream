using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class MainMenuController : MonoBehaviour
{
    [Header("📋 UI Elements")]
    [SerializeField] private Button continueButton;
    [SerializeField] private string firstSceneName = "GameplayScene"; // ชื่อฉากตื่นนอนวันแรกของคุณ

    private void Start()
    {
        CheckSaveFile();
    }

    // 🔍 เช็คว่าในเครื่องมีไฟล์เซฟไหม ถ้าไม่มีให้กดปุ่ม Continue ไม่ได้
    private void CheckSaveFile()
    {
        if (continueButton == null) return;

        // ดึงชื่อไฟล์จาก SaveManager มาเช็คพิกัด
        if (SaveManager.Instance != null)
        {
            string savePath = Path.Combine(Application.persistentDataPath, SaveManager.Instance.saveFileName);

            // ถ้ามีไฟล์เซฟอยู่จริง ให้เปิดปุ่ม Continue แต่ถ้าไม่มีให้ปิดไว้
            continueButton.interactable = File.Exists(savePath);
        }
        else
        {
            continueButton.interactable = false;
        }
    }

    // 🆕 ฟังก์ชันสำหรับผูกกับปุ่ม New Game
    public void OnClickNewGame()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ClearSave(); // ล้างข้อมูลเก่าทิ้งทั้งหมดเพื่อเริ่มใหม่ร้อยเปอร์เซ็นต์
        }

        // โหลดเข้าฉากเริ่มเกมวันแรก
        SceneManager.LoadScene(firstSceneName);
    }

    // 🔄 ฟังก์ชันสำหรับผูกกับปุ่ม Continue
    public void OnClickContinue()
    {
        if (SaveManager.Instance != null)
        {
            // เรียกคำสั่งโหลดเซฟดั้งเดิมที่คุณเขียนไว้ 
            // ตัวมันจะทำหน้าที่ปลุก Manager ทุกตัว และสั่ง GameManager โหลดฉากให้เองอัตโนมัติ!
            SaveManager.Instance.LoadGame();
        }
    }

    // ❌ ฟังก์ชันสำหรับผูกกับปุ่ม Quit Game
    public void OnClickQuitGame()
    {
        Debug.Log("[MainMenu] ผู้เล่นกดปิดเกม");
        Application.Quit();
    }
}