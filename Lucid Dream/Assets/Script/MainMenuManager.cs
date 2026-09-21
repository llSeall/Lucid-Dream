using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class MainMenuManager : MonoBehaviour
{
    public enum MenuMode { NewGame, Continue, Delete }

    [Header("📂 UI Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject slotSelectionPanel;
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("🎮 Demo UI Elements")]
    [Tooltip("ลาก UI Panel ป๊อปอัพแจ้งเตือนจบ Demo มาวางที่นี่")]
    [SerializeField] private GameObject demoEndPanel;

    [Tooltip("ลาก TextMeshProUGUI แสดงข้อความในป๊อปอัพ Demo มาวางที่นี่")]
    [SerializeField] private TextMeshProUGUI demoEndText;

    [Tooltip("ข้อความแปลภาษาสำหรับตอนเล่นจบ Demo สดๆ ร้อนๆ")]
    [SerializeField] private LocalizedString demoEndLocalizedString;

    [Tooltip("ข้อความแปลภาษาสำหรับตอนพยายามกดโหลดเซฟที่จบ Demo ไปแล้ว")]
    [SerializeField] private LocalizedString demoBlockLoadLocalizedString;

    [Header("📝 Slot Text Elements")]
    [SerializeField] private TextMeshProUGUI slot1Text;
    [SerializeField] private TextMeshProUGUI slot2Text;
    [SerializeField] private TextMeshProUGUI slot3Text;

    [Header("⚠️ Confirmation Popup Elements")]
    [SerializeField] private TextMeshProUGUI confirmationMessageText;

    [Header("🌐 Localization References")]
    [SerializeField] private LocalizedString nightLabelLocalizedString;
    [SerializeField] private LocalizedString dayLabelLocalizedString;
    [SerializeField] private LocalizedString slotFormatLocalizedString;
    [SerializeField] private LocalizedString corruptedSlotLocalizedString;
    [SerializeField] private LocalizedString emptySlotLocalizedString;
    [SerializeField] private LocalizedString overwriteConfirmLocalizedString;
    [SerializeField] private LocalizedString deleteConfirmLocalizedString;

    private MenuMode currentMode;
    private int selectedSlotID;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        RefreshSlotUI();
    }

    private void Start()
    {
        ShowMainMenu();
        CheckAndShowDemoEndPopup();
    }

    private void CheckAndShowDemoEndPopup()
    {
        if (PlayerPrefs.GetInt("ShowDemoEndPopup", 0) == 1)
        {
            ShowDemoPopup(isFromClearedGame: true);

            PlayerPrefs.SetInt("ShowDemoEndPopup", 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// ✨ สั่งเปิด Pop-up Demo พร้อมข้อความตามเหตุการณ์
    /// </summary>
    public void ShowDemoPopup(bool isFromClearedGame)
    {
        if (demoEndPanel != null)
        {
            demoEndPanel.SetActive(true);

            if (demoEndText != null)
            {
                if (isFromClearedGame)
                {
                    demoEndText.text = GetLocalizedString(
                        demoEndLocalizedString,
                        "ขอบคุณที่ทดลองเล่น Demo!\nคุณสามารถติดตามการอัปเดตเพิ่มเติมได้ในเร็วๆ นี้"
                    );
                }
                else
                {
                    demoEndText.text = GetLocalizedString(
                        demoBlockLoadLocalizedString,
                        "คุณเล่นจบ Demo ในสล็อตนี้ไปแล้วนะ!\nเนื้อหาถัดไปยังไม่เปิดให้บริการ รอติดตามในเกมฉบับเต็มนะ"
                    );
                }
            }
        }
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

                string timeLabel = (tempData.currentState == GameState.Nighttime)
                    ? GetLocalizedString(nightLabelLocalizedString, "[คืนที่ {0}]", tempData.currentDay)
                    : GetLocalizedString(dayLabelLocalizedString, "[วันที่ {0}]", tempData.currentDay);

                textComponent.text = GetLocalizedString(
                    slotFormatLocalizedString,
                    "สล็อต {0}\n{1} | ความเครียด: {2}%",
                    slotID, timeLabel, tempData.currentStress
                );
            }
            catch
            {
                textComponent.text = GetLocalizedString(
                    corruptedSlotLocalizedString,
                    "สล็อต {0}\n[ข้อมูลเสียหาย]",
                    slotID
                );
            }
        }
        else
        {
            textComponent.text = GetLocalizedString(
                emptySlotLocalizedString,
                "สล็อต {0}\n[--- เซฟว่าง ---]",
                slotID
            );
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
                confirmationMessageText.text = GetLocalizedString(
                    overwriteConfirmLocalizedString,
                    "มีข้อมูลเก่าอยู่ในสล็อต {0}\nคุณต้องการจะเริ่มเกมใหม่ทับเซฟเดิมใช่หรือไม่?",
                    slotID
                );
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
                // 🛑 ตรวจสอบก่อนว่าเซฟนี้เล่นจบ Demo ไปแล้วหรือไม่ (currentDay >= 2 และเปิด isDemoMode)
                bool isDemo = GameManager.Instance != null ? GameManager.Instance.isDemoMode : true;
                if (isDemo && IsSlotDemoCompleted(slotID))
                {
                    ShowDemoPopup(isFromClearedGame: false);
                    return;
                }

                ExecuteLoadGame(slotID);
            }
        }
    }

    /// <summary>
    /// ✨ เช็กว่าข้อมูลในเซฟสล็อตนั้นเป็น Day 2 ขึ้นไปหรือไม่
    /// </summary>
    private bool IsSlotDemoCompleted(int slotID)
    {
        string path = SaveManager.Instance.GetSaveFilePath(slotID);
        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                GameData tempData = JsonUtility.FromJson<GameData>(json);
                return tempData.currentDay >= 2;
            }
            catch
            {
                return false;
            }
        }
        return false;
    }

    public void OnClickDeleteSlotButton(int slotID)
    {
        selectedSlotID = slotID;
        string path = SaveManager.Instance.GetSaveFilePath(slotID);

        if (File.Exists(path))
        {
            currentMode = MenuMode.Delete;
            confirmationMessageText.text = GetLocalizedString(
                deleteConfirmLocalizedString,
                "คุณแน่ใจหรือไม่ว่าต้องการลบข้อมูลสล็อต {0}?",
                slotID
            );
            if (confirmationPanel != null) confirmationPanel.SetActive(true);
        }
    }

    public void OnConfirmYes()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        if (currentMode == MenuMode.NewGame) ExecuteStartNewGame(selectedSlotID);
        else if (currentMode == MenuMode.Continue) ExecuteLoadGame(selectedSlotID);
        else if (currentMode == MenuMode.Delete)
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.ClearSave(selectedSlotID);
                RefreshSlotUI();
            }
        }
    }

    public void OnConfirmNo()
    {
        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    public void OnClickBackToMainMenu()
    {
        ShowMainMenu();
    }

    public void OnClickSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

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

        SaveManager.Instance.gameData.currentDay = 0;
        SaveManager.Instance.gameData.currentState = GameState.Nighttime;
        SaveManager.Instance.SaveGame();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneForState(GameState.Nighttime);
        }
        else
        {
            SceneManager.LoadScene("TutorialScene");
        }
    }

    private void ExecuteLoadGame(int slotID)
    {
        if (SaveManager.Instance == null) return;
        SaveManager.Instance.LoadGame(slotID);
    }

    public void OnClickQuitGame()
    {
        Application.Quit();
    }

    private string GetLocalizedString(LocalizedString localizedString, string fallbackFormat, params object[] args)
    {
        if (localizedString != null && !localizedString.IsEmpty)
        {
            localizedString.Arguments = args;
            return localizedString.GetLocalizedString();
        }
        return string.Format(fallbackFormat, args);
    }
}