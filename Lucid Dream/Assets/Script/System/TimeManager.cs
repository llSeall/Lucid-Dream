using System;
using UnityEngine;
using UnityEngine.SceneManagement; // ✨ เพิ่มบรรทัดนี้เข้ามาเพื่อให้รู้จัก SceneManager ครับ

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time & Day Settings")]
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;

    [Range(0f, 24f)] public float currentHour = 8f;
    public float timeSpeed = 0.05f;

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

    private void Update()
    {
        // ⏱️ เช็คชื่อซีน ถ้าเป็นหน้าเมนูหลัก ให้หยุดเดินเวลาทันที
        if (SceneManager.GetActiveScene().name == "MainMenuScene")
        {
            return;
        }

        RunGameTime();
        HandleCheatKeys();
    }

    private void RunGameTime()
    {
        if (currentState == GameState.Daytime)
        {
            currentHour += Time.deltaTime * timeSpeed;

            if (currentHour >= 24f)
            {
                currentHour = 0f;
                StartNewDay();
            }
        }
    }

    public void ResetTime()
    {
        if (currentState == GameState.Daytime) currentHour = 8f;
        else currentHour = 0f;
    }

    public void SyncWithSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            currentDay = SaveManager.Instance.gameData.currentDay;
            currentState = SaveManager.Instance.gameData.currentState;
            currentHour = SaveManager.Instance.gameData.currentHour;

            Debug.Log($"⏳ [TimeManager] โอนย้ายเวลาตามข้อมูลเซฟสำเร็จ: Day {currentDay}");
            OnDayChangedSafe?.Invoke();
        }
    }

    public void StartNewDay()
    {
        currentDay++;
        currentState = GameState.Daytime;
        currentHour = 8f;

        Debug.Log($"<color=orange>🌅 [TimeManager] อัปเดตเช้าวันใหม่! วันที่: {currentDay}</color>");
        OnDayChangedSafe?.Invoke();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    public void EnterDreamWorld()
    {
        currentState = GameState.Nighttime;
        currentHour = 0f;

        Debug.Log($"<color=purple>🌌 [TimeManager] เข้าสู่มิติโลกความฝัน... (เวลาหยุดเดิน)</color>");
        OnDayChangedSafe?.Invoke();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM        
        if (UnityEngine.InputSystem.Keyboard.current == null) return; 
        if (UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame) StartNewDay(); 
        if (UnityEngine.InputSystem.Keyboard.current.f4Key.wasPressedThisFrame) EnterDreamWorld(); 
#else
        if (Input.GetKeyDown(KeyCode.F3)) StartNewDay();
        if (Input.GetKeyDown(KeyCode.F4)) EnterDreamWorld();
#endif
    }
}