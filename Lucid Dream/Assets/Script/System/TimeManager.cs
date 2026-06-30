using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time & Day Settings")]
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;

    [Range(0f, 24f)]
    public float currentHour = 8f;
    public float timeSpeed = 0.05f;

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
            }
        }
    }

    public void ResetTime()
    {
        if (currentState == GameState.Daytime)
        {
            currentHour = 8f;
        }
        else
        {
            currentHour = 0f;
        }
        Debug.Log("[TimeManager] ResetTime() ถูกเรียกใช้งานสำเร็จเพื่อคืนค่าเวลาเริ่มต้น");
    }

    public void SyncWithSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            // 🔥 [ปรับปรุง] ดึงค่าชั่วโมงที่บันทึกไว้ใน JSON ตรงๆ ไม่โดน Hardcode ปัดเศษทิ้งอีกต่อไป
            currentDay = SaveManager.Instance.gameData.currentDay;
            currentState = SaveManager.Instance.gameData.currentState;
            currentHour = SaveManager.Instance.gameData.currentHour;

            Debug.Log($"⏳ [TimeManager] โหลดเวลาสำเร็จ: วันที่ {currentDay} / เวลา {currentHour:0.00} น. / สถานะ {currentState}");
        }
    }

    public void StartNewDay()
    {
        currentDay++;
        currentState = GameState.Daytime;
        currentHour = 8f;

        Debug.Log($"<color=orange>🌅 [TimeManager] เริ่มต้นเช้าวันใหม่! วันที่: {currentDay}</color>");

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    public void EnterDreamWorld()
    {
        currentState = GameState.Nighttime;
        currentHour = 0f;

        Debug.Log($"<color=purple>🌌 [TimeManager] เข้าสู่มิติโลกความฝัน... (เวลาหยุดเดิน)</color>");

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