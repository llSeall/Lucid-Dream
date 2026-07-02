using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class NPCSaveData
{
    public string npcID;
    public int relationshipPoints; // นับเป็นจำนวน "วัน" ที่เคยเดินมาคุย
    public int lastTalkedDay;
    public int dailyChatCount;
    public int dailyNormalChatCount; // ตัวนับคิวบทพูดประจำวัน
    public bool hasIntroduced;       // ✨ จำว่าเคยแนะนำตัวหรือยัง
    public List<string> playedStoryKeys = new List<string>(); // ✨ จำบทเนื้อเรื่องที่กดดูไปแล้ว
}

[System.Serializable]
public class GameData
{
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;
    public float currentHour = 8f;
    public float currentSanity = 100f;
    public List<string> collectedItems = new List<string>();
    public List<NPCSaveData> npcSaveStates = new List<NPCSaveData>();
    public List<string> claimedNPCRewards = new List<string>();
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("Save File Config")]
    public string saveFileName = "YandereDream_CheckpointSave.json";
    private string saveFilePath;

    [Header("Current Game Data")]
    public GameData gameData = new GameData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
            LoadGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        HandleCheatKeys();
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

            if (PlayerStats.Instance != null)
            {
                gameData.currentSanity = PlayerStats.Instance.currentSanity;
            }

            if (NPCManager.Instance != null)
            {
                NPCManager.Instance.PackageDataForSave(ref gameData);
            }

            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"<color=green><b>💾 [SaveManager] บันทึก Checkpoint สำเร็จ!</b></color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"--- [SaveManager] บันทึกเซฟล้มเหลว: {e.Message} ---");
        }
    }

    public void LoadGame()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                string json = File.ReadAllText(saveFilePath);
                gameData = JsonUtility.FromJson<GameData>(json);
                Debug.Log("<color=cyan><b>🔄 [SaveManager] โหลด Checkpoint สำเร็จ</b></color>");

                NotifyAllManagersToSync();

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.LoadSceneForState(gameData.currentState);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"--- [SaveManager] โหลดเซฟล้มเหลว: {e.Message} ---");
                ResetData();
            }
        }
        else
        {
            ResetData();
            NotifyAllManagersToSync();
        }
    }

    public void ClearSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
        ResetData();
        NotifyAllManagersToSync();
    }

    private void ResetData()
    {
        gameData.currentDay = 1;
        gameData.currentState = GameState.Daytime;
        gameData.currentHour = 8f;
        gameData.currentSanity = 100f;
        gameData.collectedItems.Clear();
        gameData.npcSaveStates.Clear();
        gameData.claimedNPCRewards.Clear();
    }

    private void NotifyAllManagersToSync()
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.SyncWithSaveManager();
        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncFromSaveManager();
        if (TimeManager.Instance != null) TimeManager.Instance.SyncWithSaveManager();
        if (NPCManager.Instance != null) NPCManager.Instance.SyncFromSaveManager();
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        if (UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame) SaveGame();
        if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame) LoadGame();
        if (UnityEngine.InputSystem.Keyboard.current.deleteKey.wasPressedThisFrame) ClearSave();
#else
        if (Input.GetKeyDown(KeyCode.P)) SaveGame();
        if (Input.GetKeyDown(KeyCode.L)) LoadGame();
        if (Input.GetKeyDown(KeyCode.Delete)) ClearSave();
#endif
    }
}