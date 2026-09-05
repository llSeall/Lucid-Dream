using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class NPCSaveData
{
    public string npcID;
    public int relationshipPoints;
    public int lastTalkedDay;
    public int dailyChatCount;
    public int dailyNormalChatCount;
    public bool hasIntroduced;
    public List<string> playedStoryKeys = new List<string>();
}

[System.Serializable]
public class GameData
{
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;
    public int currentAP = 0;

    public float currentSanity = 100f;
    public string mapSeed = "";
    public List<string> collectedItems = new List<string>();
    public List<NPCSaveData> npcSaveStates = new List<NPCSaveData>();
    public List<string> claimedNPCRewards = new List<string>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("💾 Multi-Slot Config")]
    [Range(1, 3)] public int currentSlot = 1;
    public string saveFileNamePrefix = "YandereDream_Slot_";

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
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene") return;

        NotifyAllManagersToSync();
        Debug.Log("<color=cyan><b>[SaveManager] ซิงค์ข้อมูลเซฟเข้าสู่ระบบเรียบร้อย!</b></color>");
        // ✨ เล่นอนิเมชั่นตื่นนอนเมื่อเข้าสู่ฉากเกม (ถ้ามี PlayerWakeUpEffect ในฉาก)
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
                gameData.currentAP = TimeManager.Instance.currentAP;
            }

            if (PlayerStats.Instance != null) gameData.currentSanity = PlayerStats.Instance.currentSanity;
            if (NPCManager.Instance != null) NPCManager.Instance.PackageDataForSave(ref gameData);
            if (InventoryManager.Instance != null) InventoryManager.Instance.PackageDataForSave(ref gameData);
            if (LevelGenerator.Instance != null) gameData.mapSeed = LevelGenerator.Instance.GetMapSeed();

            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(GetSaveFilePath(currentSlot), json);

            Debug.Log($"<color=green><b>[Slot {currentSlot}] บันทึกสำเร็จ! วันที่ {gameData.currentDay} | สถานะ: {gameData.currentState}</b></color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"บันทึกเซฟล้มเหลว: {e.Message}");
        }
    }

    /// <summary>
    /// โหลดเกม
    /// @param isFromMainMenu ถ้า true จะบังคับเกิดตอนเช้า (Daytime) ของวันล่าสุด
    /// </summary>
    public void LoadGame(int slot, bool isFromMainMenu = false)
    {
        currentSlot = slot;
        string path = GetSaveFilePath(slot);

        if (!File.Exists(path))
        {
            ResetData();
            if (GameManager.Instance != null)
                GameManager.Instance.LoadSceneForState(GameState.Daytime);
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            gameData = JsonUtility.FromJson<GameData>(json);

            // ✨ เงื่อนไขสำคัญ: ถ้าโหลดจาก Main Menu ให้บังคับเข้าช่วงเช้า (Daytime) ของวันนั้นเสมอ
            if (isFromMainMenu)
            {
                gameData.currentState = GameState.Daytime;
            }

            if (GameManager.Instance != null)
                GameManager.Instance.LoadSceneForState(gameData.currentState);
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
        if (File.Exists(path)) File.Delete(path);
        ResetData();
    }

    private void ResetData()
    {
        gameData = new GameData();
    }

    private void NotifyAllManagersToSync()
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.SyncWithSaveManager();
        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncFromSaveManager();
        if (TimeManager.Instance != null) TimeManager.Instance.SyncWithSaveManager();
        if (NPCManager.Instance != null) NPCManager.Instance.SyncFromSaveManager();
        if (LevelGenerator.Instance != null) LevelGenerator.Instance.GenerateMapFromSave(gameData.mapSeed);
    }
}