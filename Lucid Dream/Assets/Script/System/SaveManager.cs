using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;

[System.Serializable]
public class GameData
{
    public int currentDay = 0; // เริ่มต้นที่คืนที่ 0
    public GameState currentState = GameState.Nighttime; // เริ่มต้นที่กลางคืน
    public float currentStress = 0f; // ค่าความเครียดสะสม
    public string mapSeed = "";
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

            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(GetSaveFilePath(currentSlot), json);

            Debug.Log($"<color=green><b>[Slot {currentSlot}] บันทึกสำเร็จ! วันที่/คืนที่ {gameData.currentDay} | สถานะ: {gameData.currentState}</b></color>");
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
}