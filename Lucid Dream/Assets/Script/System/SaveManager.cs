using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement; // ✨ ตรวจสอบให้มั่นใจว่ามีบรรทัดนี้อยู่ด้านบนสุด

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
    public float currentHour = 8f;
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

    // ✨ [เพิ่มระบบเบ็ดเสร็จ] ลงทะเบียนเปิดระบบดักจับเมื่อฉากโหลดเสร็จสมบูรณ์
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

    private void Update()
    {
        HandleCheatKeys();
    }

    // ✨ ฟังก์ชันนี้จะทำงานอัตโนมัติ "หลังจาก" ซีนใหม่ตื่นขึ้นมาเสร็จเรียบร้อยแล้ว
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // กรุณาเปลี่ยนชื่อคำว่า "MainMenuScene" ให้ตรงกับชื่อซีนเมนูของคุณ
        if (scene.name == "MainMenuScene") return;

        // ยิงข้อมูลเซฟแจกจ่ายให้ทุกแมเนเจอร์ในฉากเกมทันทีที่พวกเขาตื่นนอนครบทุกคน
        NotifyAllManagersToSync();
        Debug.Log("<color=cyan><b>📡 [SaveManager] ฉากโหลดเสร็จสิ้น ทำการซิงค์เดต้าเซฟเข้าสู่ระบบหลักเรียบร้อย!</b></color>");
    }

    public void SaveGame()
    {
        try
        {
            if (TimeManager.Instance != null)
            {
                gameData.currentDay = TimeManager.Instance.currentDay;
                gameData.currentState = TimeManager.Instance.currentState;
                gameData.currentHour = TimeManager.Instance.currentHour;
            }

            if (PlayerStats.Instance != null) gameData.currentSanity = PlayerStats.Instance.currentSanity;
            if (NPCManager.Instance != null) NPCManager.Instance.PackageDataForSave(ref gameData);
            if (InventoryManager.Instance != null) InventoryManager.Instance.PackageDataForSave(ref gameData);
            if (LevelGenerator.Instance != null) gameData.mapSeed = LevelGenerator.Instance.GetMapSeed();

            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(GetSaveFilePath(currentSlot), json);

            Debug.Log($"<color=green><b>💾 [Slot {currentSlot}] บันทึกความคืบหน้า วันที่ {gameData.currentDay} ({gameData.currentState}) เรียบร้อย!</b></color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ บันทึกเซฟล้มเหลว: {e.Message}");
        }
    }

    public void LoadGame(int slot, bool isContinue = true)
    {
        currentSlot = slot;
        string path = GetSaveFilePath(slot);

        if (!File.Exists(path))
        {
            ResetData();
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            gameData = JsonUtility.FromJson<GameData>(json);

            // ❌ ถอดการสั่งซิงค์ตรงนี้ออก เพื่อรอไปสั่งใน OnSceneLoaded ด้านบนแทนฉากจะได้ไม่ Null

            if (GameManager.Instance != null)
                GameManager.Instance.LoadSceneForState(gameData.currentState);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ โหลดเซฟล้มเหลว: {e.Message}");
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

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM        
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        if (UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame) SaveGame();
        if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame) LoadGame(currentSlot, true);
        if (UnityEngine.InputSystem.Keyboard.current.deleteKey.wasPressedThisFrame) ClearSave(currentSlot);
#else
        if (Input.GetKeyDown(KeyCode.P)) SaveGame();
        if (Input.GetKeyDown(KeyCode.L)) LoadGame(currentSlot, true);
        if (Input.GetKeyDown(KeyCode.Delete)) ClearSave(currentSlot);
#endif
    }
}