using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time & Day Settings")]
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;

    [Range(0f, 24f)]
    public float currentHour = 8f;
    public float timeSpeed = 0.05f;

    // สัญญาณ Event ปลอดภัยส่งบอกให้ NPC ตรวจเช็คการเปิด/ปิดตัวละครตามตารางวัน
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
                StartNewDay(); // หมุนครบวันสั่งขึ้นวันใหม่และเซฟเกมอัตโนมัติ
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

            Debug.Log($"⏳ [TimeManager] โหลดเวลาสำเร็จ: วันที่ {currentDay}");

            // สั่งให้ NPC ทั่วทั้งแผนที่อัปเดตตำแหน่งตามวันในไฟล์เซฟทันที
            OnDayChangedSafe?.Invoke();
        }
    }

    public void StartNewDay()
    {
        currentDay++;
        currentState = GameState.Daytime;
        currentHour = 8f;

        Debug.Log($"<color=orange>🌅 [TimeManager] เริ่มต้นเช้าวันใหม่! วันที่: {currentDay}</color>");

        // เรียกให้ NPC เปลี่ยนตารางการเกิดสปอน
        OnDayChangedSafe?.Invoke();

        // 💾 บันทึกเซฟหลักของเกมที่จุดตื่นนอน (Checkpoint)
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    public void EnterDreamWorld()
    {
        currentState = GameState.Nighttime;
        currentHour = 0f;

        Debug.Log($"<color=purple>🌌 [TimeManager] เข้าสู่มิติโลกความฝัน... (เวลาหยุดเดิน)</color>");

        OnDayChangedSafe?.Invoke();

        // 💾 บันทึกเซฟหลักของเกมตอนเข้านอน (Checkpoint)
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