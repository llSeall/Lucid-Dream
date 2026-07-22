using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Day & AP Settings")]
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;

    public int maxAP = 3;
    public int currentAP = 3;

    // Events สำหรับอัปเดต UI และระบบอื่นๆ ในเกม
    public static event Action OnAPChanged;
    public static event Action OnDayChangedSafe;

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

    private void Start()
    {
        SyncWithSaveManager();
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "MainMenuScene") return;
        HandleCheatKeys();
    }

    /// <summary>
    /// หักแต้ม AP ตามจำนวนที่ระบุ (ใช้เฉพาะช่วงกลางวัน)
    /// </summary>
    public bool UseAP(int amount = 1)
    {
        if (currentState != GameState.Daytime)
        {
            Debug.LogWarning("⚠️ [TimeManager] ไม่สามารถใช้ AP นอกช่วงเวลากลางวันได้!");
            return false;
        }

        if (currentAP >= amount)
        {
            currentAP -= amount;
            Debug.Log($"<color=yellow>⚡ [TimeManager] ใช้ไป {amount} AP! เหลือ AP: {currentAP}/{maxAP}</color>");

            // ส่งสัญญาณแจ้ง UI ให้รีเฟรชหน้าจอ
            OnAPChanged?.Invoke();

            // 🌙 ถ้า AP หมด -> สั่ง GameManager เปลี่ยนฉากเข้าสู่โลกความฝันทันที
            if (currentAP <= 0)
            {
                Debug.Log("<color=red>🌙 [TimeManager] AP หมดแล้ว! กำลังเข้าสู่โลกความฝัน...</color>");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameState.Nighttime);
                }
            }

            return true;
        }
        else
        {
            Debug.LogWarning("⚠️ [TimeManager] แต้ม AP ไม่เพียงพอ!");
            return false;
        }
    }

    /// <summary>
    /// เริ่มต้นวันใหม่ (กลางวัน) รีเซ็ต AP กลับเป็นค่าสูงสุด
    /// </summary>
    public void StartNewDay()
    {
        currentDay++;
        currentState = GameState.Daytime;
        currentAP = maxAP; // รีเซ็ต AP เต็มจำนวน

        Debug.Log($"<color=orange>🌅 [TimeManager] อัปเดตเช้าวันใหม่! วันที่: {currentDay} | AP รีเซ็ตเป็น {currentAP}</color>");

        OnAPChanged?.Invoke();
        OnDayChangedSafe?.Invoke();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    /// <summary>
    /// เข้าสู่โลกความฝัน (กลางคืน) รีเซ็ต AP เป็น 0
    /// </summary>
    public void EnterDreamWorld()
    {
        currentState = GameState.Nighttime;
        currentAP = 0; // ในโลกความฝันไม่มี AP ให้ใช้

        Debug.Log($"<color=purple>🌌 [TimeManager] เข้าสู่มิติโลกความฝัน... (เข้าสู่ช่วงกลางคืน)</color>");

        OnAPChanged?.Invoke();
        OnDayChangedSafe?.Invoke();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    /// <summary>
    /// ดึงค่า AP และวันล่าสุดจากระบบ SaveManager
    /// </summary>
    public void SyncWithSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            currentDay = SaveManager.Instance.gameData.currentDay;
            currentState = SaveManager.Instance.gameData.currentState;
            currentAP = SaveManager.Instance.gameData.currentAP;

            Debug.Log($"⏳ [TimeManager] โอนย้ายข้อมูลสำเร็จ: Day {currentDay} | State: {currentState} | AP: {currentAP}");
            OnAPChanged?.Invoke();
            OnDayChangedSafe?.Invoke();
        }
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM        
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        if (UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame) StartNewDay();
        if (UnityEngine.InputSystem.Keyboard.current.f4Key.wasPressedThisFrame) UseAP(1);
#else
        if (Input.GetKeyDown(KeyCode.F3)) StartNewDay();
        if (Input.GetKeyDown(KeyCode.F4)) UseAP(1);
#endif
    }
}