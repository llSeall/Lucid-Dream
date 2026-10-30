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

    private void Update()
    {
        // 🧪 ปุ่ม Debug F10: บังคับเซฟสล็อตปัจจุบันให้กลายเป็น Day 2 (จบ Demo) เพื่อทดสอบ
        if (Input.GetKeyDown(KeyCode.F10))
        {
            Debug.Log($"<color=yellow><b>[DEBUG F10] บังคับเปลี่ยนสล็อต {currentSlot} เป็นจบ Demo (Day 2)!</b></color>");
            SetSlotAsDemoCleared(currentSlot);
        }
    }

    /// <summary>
    /// ✨ ฟังก์ชัน Debug F10 สำหรับตั้งค่าเซฟให้จบ Demo สดๆ ร้อนๆ
    /// </summary>
    public void SetSlotAsDemoCleared(int slot)
    {
        currentSlot = slot;

        // กำหนดค่าใน RAM
        gameData.currentDay = 2;
        gameData.currentState = GameState.Nighttime;

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.currentDay = 2;
            TimeManager.Instance.currentState = GameState.Nighttime;
        }

        // เขียนลงไฟล์เซฟ
        string json = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(GetSaveFilePath(currentSlot), json);

        // สั่งโหลดฉาก ซึ่ง GameManager จะตรวจพบ Demo Mode และส่งกลับ Main Menu
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneForState(GameState.Nighttime);
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

    /// <summary>
    /// ✨ ฟังก์ชันเตรียมข้อมูลสำหรับเริ่มเกมใหม่ (ล้างข้อมูลทั้งในเซฟและ RAM Manager ต่างๆ)
    /// </summary>
    public void PrepareNewGame(int slot)
    {
        currentSlot = slot;

        // 1. ลบไฟล์เซฟเก่าในดิสก์
        ClearSave(slot);

        // 2. รีเซ็ตค่าใน Manager ต่างๆ ใน RAM ป้องกันข้อมูลค้างจากเกมรอบเดิม
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.currentDay = 0;
            TimeManager.Instance.currentState = GameState.Nighttime;
        }

        if (StressManager.Instance != null)
        {
            // รีเซ็ตค่าความเครียดใน RAM (ถ้ามีฟังก์ชัน ResetStress หรือปรับค่าตรงๆ)
            gameData.currentStress = 0f;
        }

        // 3. ตั้งค่าโครงสร้างข้อมูลใหม่เริ่มต้นที่ Day 0
        gameData = new GameData();
        gameData.currentDay = 0;
        gameData.currentState = GameState.Nighttime;

        // 4. บันทึกไฟล์ใหม่ลงดิสก์
        string json = JsonUtility.ToJson(gameData, true);
        File.WriteAllText(GetSaveFilePath(currentSlot), json);

        Debug.Log($"<color=green><b>[SaveManager] เตรียมข้อมูล New Game สล็อต {slot} เรียบร้อย! เริ่มต้นที่ Day 0</b></color>");
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
            PrepareNewGame(slot);
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

            GameState targetState = gameData.currentState;
            if (targetState == GameState.Nighttime && gameData.currentDay > 0)
            {
                targetState = GameState.Daytime;
                gameData.currentState = GameState.Daytime;
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