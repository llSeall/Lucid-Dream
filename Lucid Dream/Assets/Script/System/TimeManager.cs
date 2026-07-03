using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; } //[cite: 16]

    [Header("Time & Day Settings")]
    public int currentDay = 1; //[cite: 16]
    public GameState currentState = GameState.Daytime; //[cite: 16]

    [Range(0f, 24f)] public float currentHour = 8f; //[cite: 16]
    public float timeSpeed = 0.05f; //[cite: 16]

    public static event Action OnDayChangedSafe; //[cite: 16]

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; //[cite: 16]
            DontDestroyOnLoad(gameObject); //[cite: 16]
        }
        else
        {
            Destroy(gameObject); //[cite: 16]
        }
    }

    private void Update()
    {
        RunGameTime(); //[cite: 16]
        HandleCheatKeys(); //[cite: 16]
    }

    private void RunGameTime()
    {
        if (currentState == GameState.Daytime) //[cite: 16, 17]
        {
            currentHour += Time.deltaTime * timeSpeed; //[cite: 16]

            if (currentHour >= 24f) //[cite: 16]
            {
                currentHour = 0f; //[cite: 16]
                StartNewDay(); //[cite: 16]
            }
        }
    }

    public void ResetTime()
    {
        if (currentState == GameState.Daytime) currentHour = 8f; //[cite: 16, 17]
        else currentHour = 0f; //[cite: 16]
    }

    public void SyncWithSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null) //[cite: 15, 16]
        {
            currentDay = SaveManager.Instance.gameData.currentDay; //[cite: 15, 16]
            currentState = SaveManager.Instance.gameData.currentState; //[cite: 15, 16]
            currentHour = SaveManager.Instance.gameData.currentHour; //[cite: 15, 16]

            Debug.Log($"⏳ [TimeManager] โันติเวลาตามข้อมูลเซฟย้อนอดีต: Day {currentDay}"); //[cite: 16]
            OnDayChangedSafe?.Invoke(); //[cite: 16]
        }
    }

    public void StartNewDay()
    {
        currentDay++; //[cite: 16]
        currentState = GameState.Daytime; //[cite: 16, 17]
        currentHour = 8f; //[cite: 16]

        Debug.Log($"<color=orange>🌅 [TimeManager] อัปเดตเช้าวันใหม่! วันที่: {currentDay}</color>"); //[cite: 16]
        OnDayChangedSafe?.Invoke(); //[cite: 16]

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame(); //[cite: 15, 16]
    }

    public void EnterDreamWorld()
    {
        currentState = GameState.Nighttime; //[cite: 16, 17]
        currentHour = 0f; //[cite: 16]

        Debug.Log($"<color=purple>🌌 [TimeManager] เข้าสู่มิติโลกความฝัน... (เวลาหยุดเดิน)</color>"); //[cite: 16]
        OnDayChangedSafe?.Invoke(); //[cite: 16]

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame(); //[cite: 15, 16]
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current == null) return; //[cite: 16]
        if (UnityEngine.InputSystem.Keyboard.current.f3Key.wasPressedThisFrame) StartNewDay(); //[cite: 16]
        if (UnityEngine.InputSystem.Keyboard.current.f4Key.wasPressedThisFrame) EnterDreamWorld(); //[cite: 16]
#else
        if (Input.GetKeyDown(KeyCode.F3)) StartNewDay(); //[cite: 16]
        if (Input.GetKeyDown(KeyCode.F4)) EnterDreamWorld(); //[cite: 16]
#endif
    }
}