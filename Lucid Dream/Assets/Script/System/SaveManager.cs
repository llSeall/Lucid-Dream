using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;

[System.Serializable]
public class GameData
{
    public int currentDay = 0;
    public GameState currentState = GameState.Nighttime;
    public float currentStress = 0f;
    public string mapSeed = "";
    public string selectedLanguage = "th";
    public List<string> collectedItems = new List<string>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("💾 Multi-Slot Config")]
    [Range(1, 3)] public int currentSlot = 1;
    public string saveFileNamePrefix = "Nightmare_Slot_";

    [Header("Current RAM Data")]
    public GameData gameData = new GameData();

    public string GetSaveFilePath(int slot) => Path.Combine(Application.persistentDataPath, $"{saveFileNamePrefix}{slot}.json");

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ResetData();

        string savedLanguage = PlayerPrefs.GetString("SelectedLanguageCode", "");
        if (!string.IsNullOrEmpty(savedLanguage))
        {
            SetLanguageByCode(savedLanguage);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene") return;

        NotifyAllManagersToSync();
        Debug.Log("<color=cyan><b>[SaveManager] ซิงค์ข้อมูลเซฟเข้าสู่ระบบเรียบร้อย!</b></color>");

        if (PlayerWakeUpEffect.Instance != null)
        {
            PlayerWakeUpEffect.Instance.PlayWakeUpAnimation();
        }
    }

    public void SaveGame()
    {
        try
        {
            if (TimeManager.Instance != null)
            {
                gameData.currentDay = TimeManager.Instance.currentDay;
                gameData.currentState = TimeManager.Instance.currentState;
            }

            if (StressManager.Instance != null)
            {
                gameData.currentStress = StressManager.Instance.CurrentStress;
            }

            if (InventoryManager.Instance != null) InventoryManager.Instance.PackageDataForSave(ref gameData);
            if (LevelGenerator.Instance != null) gameData.mapSeed = LevelGenerator.Instance.GetMapSeed();

            if (LocalizationSettings.SelectedLocale != null)
            {
                string langCode = LocalizationSettings.SelectedLocale.Identifier.Code;
                gameData.selectedLanguage = langCode;
                PlayerPrefs.SetString("SelectedLanguageCode", langCode);
                PlayerPrefs.Save();
            }

            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(GetSaveFilePath(currentSlot), json);

            Debug.Log($"<color=green><b>[Slot {currentSlot}] บันทึกสำเร็จ! ภาษา: {gameData.selectedLanguage} | วันที่/คืนที่ {gameData.currentDay}</b></color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"บันทึกเซฟล้มเหลว: {e.Message}");
        }
    }

    public void LoadGame(int slot)
    {
        currentSlot = slot;
        string path = GetSaveFilePath(slot);

        if (!File.Exists(path))
        {
            ResetData();
            if (GameManager.Instance != null)
                GameManager.Instance.LoadSceneForState(GameState.Nighttime);
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            gameData = JsonUtility.FromJson<GameData>(json);

            if (!string.IsNullOrEmpty(gameData.selectedLanguage))
            {
                SetLanguageByCode(gameData.selectedLanguage);
            }

            // ✨ ถ้าผู้เล่นกดโหลดเกมมาจาก Main Menu และอยู่ในช่วงกลางคืน ให้ย้อนกลับไปเริ่มตอนเช้า (Daytime) ของวันนั้นแทน
            GameState targetState = gameData.currentState;
            if (targetState == GameState.Nighttime && gameData.currentDay > 0)
            {
                targetState = GameState.Daytime;
                gameData.currentState = GameState.Daytime; // ปรับ State ใน RAM ให้ตรงกัน
            }

            if (GameManager.Instance != null)
                GameManager.Instance.LoadSceneForState(targetState);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"โหลดเซฟล้มเหลว: {e.Message}");
            ResetData();
        }
    }

    public void ClearSave(int slot)
    {
        string path = GetSaveFilePath(slot);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"<color=red><b>ลบข้อมูลเซฟใน สล็อต {slot} เรียบร้อยแล้ว</b></color>");
        }
        ResetData();
    }

    public void ResetData()
    {
        gameData = new GameData();
    }

    private void NotifyAllManagersToSync()
    {
        if (TimeManager.Instance != null) TimeManager.Instance.SyncWithSaveManager();
        if (StressManager.Instance != null) StressManager.Instance.SyncWithSaveManager();
        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncFromSaveManager();
        if (LevelGenerator.Instance != null) LevelGenerator.Instance.GenerateMapFromSave(gameData.mapSeed);
    }

    public void SetLanguageByCode(string langCode)
    {
        var locales = LocalizationSettings.AvailableLocales.Locales;
        foreach (var locale in locales)
        {
            if (locale.Identifier.Code == langCode)
            {
                LocalizationSettings.SelectedLocale = locale;
                PlayerPrefs.SetString("SelectedLanguageCode", langCode);
                PlayerPrefs.Save();
                break;
            }
        }
    }
}