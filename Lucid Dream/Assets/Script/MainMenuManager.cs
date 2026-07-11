using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro; // 👈 1. เพิ่มตัวนี้เข้ามาเพื่อเรียกใช้ TextMeshPro

public class MainMenuManager : MonoBehaviour
{
    public enum MenuMode { NewGame, Continue }

    [Header("📂 UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject slotSelectionPanel;
    [SerializeField] private GameObject confirmationPanel;

    [Header("📝 Slot Text Elements (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI slot1Text; // 👈 2. เปลี่ยนจาก Text เป็น TextMeshProUGUI
    [SerializeField] private TextMeshProUGUI slot2Text;
    [SerializeField] private TextMeshProUGUI slot3Text;

    [Header("⚠️ Confirmation Popup Elements (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI confirmationMessageText; // 👈 3. เปลี่ยนตรงนี้ด้วย   

    [Header("⚙️ Scene Configuration")]
    [SerializeField] private string gameplaySceneName = "DaytimeScene";

    private MenuMode currentMode;
    private int selectedSlotID;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        slotSelectionPanel.SetActive(false);
        confirmationPanel.SetActive(false);
    }

    public void OnClickNewGameMode()
    {
        currentMode = MenuMode.NewGame;
        mainMenuPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
        RefreshSlotUI();
    }

    public void OnClickContinueMode()
    {
        currentMode = MenuMode.Continue;
        mainMenuPanel.SetActive(false);
        slotSelectionPanel.SetActive(true);
        RefreshSlotUI();
    }

    public void RefreshSlotUI()
    {
        if (SaveManager.Instance == null) return;
        UpdateSlotDisplay(1, slot1Text);
        UpdateSlotDisplay(2, slot2Text);
        UpdateSlotDisplay(3, slot3Text);
    }

    // สั่งเปลี่ยนข้อความผ่าน .text ได้เหมือนเดิมเป๊ะ ไม่ต้องแก้โค้ดข้างล่างเลยครับ
    private void UpdateSlotDisplay(int slotID, TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;

        string path = SaveManager.Instance.GetSaveFilePath(slotID);

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                GameData tempData = JsonUtility.FromJson<GameData>(json);
                string phaseName = (tempData.currentState == GameState.Daytime) ? "กลางวัน" : "กลางคืน";
                textComponent.text = $"สล็อต {slotID}\n[วันที่ {tempData.currentDay} - {phaseName}]";
            }
            catch
            {
                textComponent.text = $"สล็อต {slotID}\n[ข้อมูลเสียหาย]";
            }
        }
        else
        {
            textComponent.text = $"สล็อต {slotID}\n[--- เซฟว่าง ---]";
        }
    }

    public void OnSelectSlot(int slotID)
    {
        selectedSlotID = slotID;
        string path = SaveManager.Instance.GetSaveFilePath(slotID);
        bool saveExists = File.Exists(path);

        if (currentMode == MenuMode.NewGame)
        {
            if (saveExists)
            {
                confirmationMessageText.text = $" มีข้อมูลเก่าอยู่ในสล็อต {slotID}\nคุณต้องการจะเซฟทับจริงๆ ใช่หรือไม่?\n(ข้อมูลเดิมจะหายไปทั้งหมด)";
                confirmationPanel.SetActive(true);
            }
            else
            {
                ExecuteStartNewGame(slotID);
            }
        }
        else if (currentMode == MenuMode.Continue)
        {
            if (saveExists)
            {
                confirmationMessageText.text = $" คุณต้องการจะโหลดเซฟจาก สล็อต {slotID} ใช่หรือไม่?";
                confirmationPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[Menu] สล็อต {slotID} ไม่มีข้อมูลให้โหลด!");
            }
        }
    }

    public void OnConfirmYes()
    {
        confirmationPanel.SetActive(false);
        if (currentMode == MenuMode.NewGame) ExecuteStartNewGame(selectedSlotID);
        else if (currentMode == MenuMode.Continue) ExecuteLoadGame(selectedSlotID);
    }

    public void OnConfirmNo()
    {
        confirmationPanel.SetActive(false);
    }

    /// <summary>
    /// ✨ ฟังก์ชันใหม่: สำหรับกดปุ่มย้อนกลับจากหน้าเลือกสล็อตเพื่อกลับไปหน้าเมนูหลัก
    /// </summary>
    public void OnClickBackToMainMenu()
    {
        ShowMainMenu();
    }

    private void ExecuteStartNewGame(int slotID)
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.currentSlot = slotID;
        SaveManager.Instance.ClearSave(slotID);
        SceneManager.LoadScene(gameplaySceneName);
    }

    private void ExecuteLoadGame(int slotID)
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.LoadGame(slotID, true);
    }

    public void OnClickQuitGame()
    {
        Application.Quit();
    }
}