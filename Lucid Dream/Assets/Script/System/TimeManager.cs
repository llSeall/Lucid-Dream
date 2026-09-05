using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Day & State Settings")]
    public int currentDay = 1;
    public GameState currentState = GameState.Daytime;

    public int maxAP = 3;
    public int currentAP = 0;

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

    /// <summary>
    /// หัก AP ทำกิจกรรม (ไม่มีเรื่องเวลานับถอยหลังแล้ว)
    /// </summary>
    public bool UseAP(int amount = 1)
    {
        if (currentState != GameState.Daytime) return false;

        if (currentAP >= amount)
        {
            currentAP -= amount;
            OnAPChanged?.Invoke();

            // เมื่อ AP หมด ให้เปลี่ยนเข้าช่วงกลางคืนอัตโนมัติ
            if (currentAP <= 0)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.ChangeState(GameState.Nighttime);
                }
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// เริ่มต้นวันใหม่ (กลางวัน) -> ทำการบันทึก Checkpoint กลางวัน
    /// </summary>
    public void StartNewDay()
    {
        currentDay++;
        currentState = GameState.Daytime;
        currentAP = maxAP;

        OnAPChanged?.Invoke();
        OnDayChangedSafe?.Invoke();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    /// <summary>
    /// เข้าสู่โลกความฝัน (กลางคืน) -> ทำการบันทึก Checkpoint กลางคืน
    /// </summary>
    public void EnterDreamWorld()
    {
        currentState = GameState.Nighttime;
        currentAP = 0;

        OnAPChanged?.Invoke();
        OnDayChangedSafe?.Invoke();

        // ✨ บันทึกเซฟทันทีเมื่อเข้ากลางคืน (หากตายในคืนนี้ โหลดกลับมาจะเริ่มที่คืนนี้)
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
    }

    public void SyncWithSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            currentDay = SaveManager.Instance.gameData.currentDay;
            currentState = SaveManager.Instance.gameData.currentState;
            currentAP = SaveManager.Instance.gameData.currentAP;

            OnAPChanged?.Invoke();
            OnDayChangedSafe?.Invoke();
        }
    }
}