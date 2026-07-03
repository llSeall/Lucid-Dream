using System.Collections.Generic;
using UnityEngine;
using System.IO;

[System.Serializable]
public class NPCSaveData
{
    public string npcID; //[cite: 15]
    public int relationshipPoints; //[cite: 15]
    public int lastTalkedDay; //[cite: 15]
    public int dailyChatCount; //[cite: 15]
    public int dailyNormalChatCount; //[cite: 15]
    public bool hasIntroduced; //[cite: 15]
    public List<string> playedStoryKeys = new List<string>(); //[cite: 15]
}

[System.Serializable]
public class GameData
{
    public int currentDay = 1; //[cite: 15]
    public GameState currentState = GameState.Daytime; //[cite: 15]
    public float currentHour = 8f; //[cite: 15]
    public float currentSanity = 100f; //[cite: 15]
    public string mapSeed = "";        // ✨ [เพิ่มใหม่] เพื่อผูกด่านสุ่มเข้ากับประวัติศาสตร์เซฟ[cite: 13, 15]
    public List<string> collectedItems = new List<string>(); //[cite: 15]
    public List<NPCSaveData> npcSaveStates = new List<NPCSaveData>(); //[cite: 15]
    public List<string> claimedNPCRewards = new List<string>(); //[cite: 15]
}

[System.Serializable]
public class DayPhaseData
{
    public bool hasDaytimeSave = false; //[cite: 15]
    public GameData daytimeData; //[cite: 15]
    public bool hasNighttimeSave = false; //[cite: 15]
    public GameData nighttimeData; //[cite: 15]
}

[System.Serializable]
public class SlotData
{
    public int slotID; //[cite: 15]
    public int latestPlayedDay = 1; //[cite: 15]
    public GameState latestPlayedState = GameState.Daytime; //[cite: 15]
    public List<DayPhaseData> dailyHistory = new List<DayPhaseData>(); // ✨ บันทึกประวัติศาสตร์ 14 วัน[cite: 15]
}

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; } //[cite: 15]

    [Header("💾 Multi-Slot Config")]
    [Range(1, 3)] public int currentSlot = 1;
    public string saveFileNamePrefix = "YandereDream_Slot_"; //[cite: 15]

    [Header("Current RAM Data")]
    public GameData gameData = new GameData(); //[cite: 15]
    public SlotData currentSlotData = new SlotData(); //[cite: 15]

    private string GetSaveFilePath(int slot) => Path.Combine(Application.persistentDataPath, $"{saveFileNamePrefix}{slot}.json"); //[cite: 15]

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; //[cite: 15]
            DontDestroyOnLoad(gameObject); //[cite: 15]
        }
        else
        {
            Destroy(gameObject); //[cite: 15]
        }
    }

    private void Start()
    {
        // เริ่มเกมมาให้โหลดสล็อตเริ่มต้นเตรียมไว้ใช้งาน[cite: 15]
        LoadGame(currentSlot, true);
    }

    private void Update()
    {
        HandleCheatKeys(); //[cite: 15]
    }

    public void SaveGame()
    {
        try
        {
            // 1. ดึงข้อมูลแบบเรียลไทม์มาพักไว้ใน RAM[cite: 15]
            if (TimeManager.Instance != null)
            {
                gameData.currentDay = TimeManager.Instance.currentDay; //[cite: 15, 16]
                gameData.currentState = TimeManager.Instance.currentState; //[cite: 15, 16]
                gameData.currentHour = TimeManager.Instance.currentHour; //[cite: 15, 16]
            }

            if (PlayerStats.Instance != null) gameData.currentSanity = PlayerStats.Instance.currentSanity; //[cite: 15]
            if (NPCManager.Instance != null) NPCManager.Instance.PackageDataForSave(ref gameData); //[cite: 15]
            if (InventoryManager.Instance != null) InventoryManager.Instance.PackageDataForSave(ref gameData); //[cite: 15]

            // 🔒 [แก้ไขบั๊ก] ล็อกซีดส์ด่านปัจจุบันเข้ากล่องเดต้าเซฟ[cite: 13, 15]
            if (LevelGenerator.Instance != null) gameData.mapSeed = LevelGenerator.Instance.GetMapSeed();

            // 2. ขยายขนาดลิสต์ประวัติศาสตร์ให้เท่ากับวันปัจจุบัน[cite: 15]
            int targetDay = gameData.currentDay;
            while (currentSlotData.dailyHistory.Count < targetDay)
            {
                currentSlotData.dailyHistory.Add(new DayPhaseData()); //[cite: 15]
            }

            int index = targetDay - 1;
            DayPhaseData dayPhase = currentSlotData.dailyHistory[index]; //[cite: 15]

            // 3. แยกจัดเก็บตามช่วงเวลา และลบอนาคตเผื่อมีการย้อนเวลาเล่นใหม่ (Butterfly Effect)[cite: 15]
            if (gameData.currentState == GameState.Daytime) //[cite: 15, 17]
            {
                dayPhase.daytimeData = CloneGameData(gameData); //[cite: 15]
                dayPhase.hasDaytimeSave = true; //[cite: 15]
                dayPhase.hasNighttimeSave = false; //[cite: 15]
                dayPhase.nighttimeData = null; //[cite: 15]
            }
            else //[cite: 15, 17]
            {
                dayPhase.nighttimeData = CloneGameData(gameData); //[cite: 15]
                dayPhase.hasNighttimeSave = true; //[cite: 15]
            }

            // ถ้าย้อนอดีตมาเซฟใหม่ ให้ล้างประวัติศาสตร์วันที่เกินเลยจากนี้ทิ้งทั้งหมด![cite: 15]
            if (currentSlotData.dailyHistory.Count > targetDay)
            {
                currentSlotData.dailyHistory.RemoveRange(targetDay, currentSlotData.dailyHistory.Count - targetDay); //[cite: 15]
            }

            // 4. บันทึกข้อมูลภาพรวมของสล็อต[cite: 15]
            currentSlotData.slotID = currentSlot; //[cite: 15]
            currentSlotData.latestPlayedDay = gameData.currentDay; //[cite: 15]
            currentSlotData.latestPlayedState = gameData.currentState; //[cite: 15]

            // แปลงข้อมูลลงไฟล์ JSON[cite: 15]
            string json = JsonUtility.ToJson(currentSlotData, true); //[cite: 15]
            File.WriteAllText(GetSaveFilePath(currentSlot), json); //[cite: 15]
            Debug.Log($"<color=green><b>💾 [Slot {currentSlot}] บันทึก Day {targetDay} ({gameData.currentState}) ลงไทม์ไลน์แล้ว!</b></color>"); //[cite: 15]
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ บันทึกเซฟล้มเหลว: {e.Message}"); //[cite: 15]
        }
    }

    // ฟังก์ชันยืดหยุ่นสูง: โหลดเล่นต่อ (Continue) หรือจิ้มเลือกวันย้อนอดีตจาก UI สล็อตไทม์ไลน์ได้อิสระ
    public void LoadGame(int slot, bool isContinue = false, int requestedDay = 1, GameState requestedPhase = GameState.Daytime)
    {
        currentSlot = slot; //[cite: 15]
        string path = GetSaveFilePath(slot); //[cite: 15]

        if (!File.Exists(path)) //[cite: 15]
        {
            ResetData(); //[cite: 15]
            NotifyAllManagersToSync(); //[cite: 15]
            return; //[cite: 15]
        }

        try
        {
            string json = File.ReadAllText(path); //[cite: 15]
            currentSlotData = JsonUtility.FromJson<SlotData>(json); //[cite: 15]

            int targetDay = isContinue ? currentSlotData.latestPlayedDay : requestedDay; //[cite: 15]
            GameState targetPhase = isContinue ? currentSlotData.latestPlayedState : requestedPhase; //[cite: 15]

            int index = targetDay - 1;
            if (index >= 0 && index < currentSlotData.dailyHistory.Count) //[cite: 15]
            {
                DayPhaseData dayPhase = currentSlotData.dailyHistory[index]; //[cite: 15]
                GameData targetData = (targetPhase == GameState.Daytime) ? dayPhase.daytimeData : dayPhase.nighttimeData; //[cite: 15, 17]

                if (targetData != null) //[cite: 15]
                {
                    gameData = CloneGameData(targetData); //[cite: 15]
                    NotifyAllManagersToSync(); //[cite: 15]

                    // 🔒 [แก้ไขบั๊กลำดับโค้ด] โหลดฉากย้ายโลกหลังจากข้อมูลใน RAM ซิงค์เสร็จสิ้นแล้วเท่านั้น[cite: 15, 17]
                    if (GameManager.Instance != null) GameManager.Instance.LoadSceneForState(gameData.currentState);
                    return;
                }
            }

            ResetData(); //[cite: 15]
            NotifyAllManagersToSync(); //[cite: 15]
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ โหลดเซฟล้มเหลว: {e.Message}"); //[cite: 15]
            ResetData(); //[cite: 15]
        }
    }

    public void ClearSave(int slot)
    {
        string path = GetSaveFilePath(slot); //[cite: 15]
        if (File.Exists(path)) File.Delete(path); //[cite: 15]
        ResetData(); //[cite: 15]
        NotifyAllManagersToSync(); //[cite: 15]
    }

    private void ResetData()
    {
        gameData = new GameData(); //[cite: 15]
        currentSlotData = new SlotData { slotID = currentSlot }; //[cite: 15]
    }

    private void NotifyAllManagersToSync()
    {
        if (PlayerStats.Instance != null) PlayerStats.Instance.SyncWithSaveManager(); //[cite: 15]
        if (InventoryManager.Instance != null) InventoryManager.Instance.SyncFromSaveManager(); //[cite: 15]
        if (TimeManager.Instance != null) TimeManager.Instance.SyncWithSaveManager(); //[cite: 15, 16]
        if (NPCManager.Instance != null) NPCManager.Instance.SyncFromSaveManager(); //[cite: 15]

        // ✨ [เพิ่มใหม่] สั่งให้ระบบสร้างด่านล็อก Seed ตามอดีตที่บันทึกไว้ทันทีที่โหลดข้อมูลเสร็จ[cite: 13, 15]
        if (LevelGenerator.Instance != null) LevelGenerator.Instance.GenerateMapFromSave(gameData.mapSeed);
    }

    private GameData CloneGameData(GameData source)
    {
        string json = JsonUtility.ToJson(source); //[cite: 15]
        return JsonUtility.FromJson<GameData>(json); //[cite: 15]
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current == null) return; //[cite: 15]
        if (UnityEngine.InputSystem.Keyboard.current.pKey.wasPressedThisFrame) SaveGame(); //[cite: 15]
        if (UnityEngine.InputSystem.Keyboard.current.lKey.wasPressedThisFrame) LoadGame(currentSlot, true); //[cite: 15]
        if (UnityEngine.InputSystem.Keyboard.current.deleteKey.wasPressedThisFrame) ClearSave(currentSlot); //[cite: 15]
#else
        if (Input.GetKeyDown(KeyCode.P)) SaveGame(); //[cite: 15]
        if (Input.GetKeyDown(KeyCode.L)) LoadGame(currentSlot, true); //[cite: 15]
        if (Input.GetKeyDown(KeyCode.Delete)) ClearSave(currentSlot); //[cite: 15]
#endif
    }
}