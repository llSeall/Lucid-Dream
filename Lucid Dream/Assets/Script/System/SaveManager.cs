using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class GameData
{
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;
    public float currentHour = 8f;                           // 🔥 [เพิ่ม] ตัวแปรเก็บชั่วโมงเวลาจริงลงไฟล์เซฟ
    public float currentSanity = 100f;
    public List<string> collectedItems = new List<string>();
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
            // 🔥 [ปรับปรุง] ดึงค่าล่าสุดจากทุก Manager มาอัปเดตลง gameData ก่อนเขียนไฟล์แบบ Real-time
            if (TimeManager.Instance != null)
            {
                gameData.currentDay = TimeManager.Instance.currentDay;
                gameData.currentState = TimeManager.Instance.currentState;
                gameData.currentHour = TimeManager.Instance.currentHour; // เซฟชั่วโมงละเอียด
            }

            if (PlayerStats.Instance != null)
            {
                gameData.currentSanity = PlayerStats.Instance.currentSanity; // ดึงค่าสติล่าสุดชัวร์ๆ
            }

            string json = JsonUtility.ToJson(gameData, true);
            File.WriteAllText(saveFilePath, json);
            Debug.Log($"<color=green><b>💾 [SaveManager] บันทึก Checkpoint สำเร็จ! (วันที่: {gameData.currentDay} / เวลา: {gameData.currentHour:0.00} / สถานะ: {gameData.currentState})</b></color>");
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
                Debug.Log("<color=cyan><b>🔄 [SaveManager] พบไฟล์เซฟเก่า! โหลด Checkpoint สำเร็จ</b></color>");

                // 1. ซิงค์ค่าตัวแปรเข้าสู่ทุก Manager ที่ลอยอยู่ข้ามฉาก
                NotifyAllManagersToSync();

                // 2. 🔥 [แก้บั๊กใหญ่] สั่งให้ GameManager โหลดฉากใหม่ให้ตรงกับข้อมูลในเซฟทันที!
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
            Debug.Log("--- [SaveManager] ไม่พบไฟล์เซฟในเครื่อง เริ่มต้นข้อมูลวันแรกใหม่ให้ ---");
            ResetData();
            NotifyAllManagersToSync();
        }
    }

    public void ClearSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("<color=red><b>❌ [SaveManager] ลบไฟล์เซฟออกจากเครื่องถาวรแล้ว!</b></color>");
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
    }

    private void NotifyAllManagersToSync()
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.SyncWithSaveManager();
        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncFromSaveManager();
        if (TimeManager.Instance != null) TimeManager.Instance.SyncWithSaveManager();
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