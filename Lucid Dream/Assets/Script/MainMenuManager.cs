using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    public enum MenuMode { NewGame, Continue }

    [Header("📂 UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject slotSelectionPanel;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("📝 Slot Text Elements")]
    [SerializeField] private TextMeshProUGUI slot1Text;
    [SerializeField] private TextMeshProUGUI slot2Text;
    [SerializeField] private TextMeshProUGUI slot3Text;

    [Header("⚠️ Confirmation Popup Elements")]
    [SerializeField] private TextMeshProUGUI confirmationMessageText;

    [Header("⚙️ Scene Configuration")]
    [SerializeField] private string daytimeSceneName = "DaytimeScene";

    private MenuMode currentMode;
    private int selectedSlotID;

    private void Start()
    {
        ShowMainMenu();
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (slotSelectionPanel != null) slotSelectionPanel.SetActive(false);
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    public void OnClickNewGameMode()
    {
        currentMode = MenuMode.NewGame;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (slotSelectionPanel != null) slotSelectionPanel.SetActive(true);
        RefreshSlotUI();
    }

    public void OnClickContinueMode()
    {
        currentMode = MenuMode.Continue;
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (slotSelectionPanel != null) slotSelectionPanel.SetActive(true);
        RefreshSlotUI();
    }

    public void RefreshSlotUI()
    {
        if (SaveManager.Instance == null) return;
        UpdateSlotDisplay(1, slot1Text);
        UpdateSlotDisplay(2, slot2Text);
        UpdateSlotDisplay(3, slot3Text);
    }

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
                textComponent.text = $"สล็อต {slotID}\n[วันที่ {tempData.currentDay}]";
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
                confirmationMessageText.text = $"มีข้อมูลเก่าอยู่ในสล็อต {slotID}\nคุณต้องการจะเซฟทับจริงๆ ใช่หรือไม่?";
                if (confirmationPanel != null) confirmationPanel.SetActive(true);
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
                confirmationMessageText.text = $"คุณต้องการจะโหลดเซฟจาก สล็อต {slotID} ใช่หรือไม่?";
                if (confirmationPanel != null) confirmationPanel.SetActive(true);
            }
        }
    }

    public void OnConfirmYes()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
        if (currentMode == MenuMode.NewGame) ExecuteStartNewGame(selectedSlotID);
        else if (currentMode == MenuMode.Continue) ExecuteLoadGame(selectedSlotID);
    }

    public void OnConfirmNo()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    public void OnClickBackToMainMenu()
    {
        ShowMainMenu();
    }

    /// <summary>
    /// ผูกกับปุ่ม "Settings" บนหน้า Main Menu
    /// </summary>
    public void OnClickSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    /// <summary>
    /// ผูกกับปุ่ม "Back" ในหน้า Settings ของ Main Menu
    /// </summary>
    public void OnClickBackFromSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    private void ExecuteStartNewGame(int slotID)
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.currentSlot = slotID;
        SaveManager.Instance.ClearSave(slotID);
        SceneManager.LoadScene(daytimeSceneName);
    }

    private void ExecuteLoadGame(int slotID)
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.LoadGame(slotID, isFromMainMenu: true);
    }

    public void OnClickQuitGame()
    {
        Application.Quit();
    }
}