using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Day & State Settings")]
    public int currentDay = 0; // 0 = คืนแรก (Night 0)
    public GameState currentState = GameState.Nighttime;

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
    /// เรียกเมื่อผู้เล่นหนีออกจากฝันสำเร็จ -> ตื่นนอนตอนเช้าของวันถัดไป
    /// </summary>
    public void ExitDreamToDaytime()
    {
        currentDay++; // คืนที่ 0 ตื่นมาจะกลายเป็น วันที่ 1
        currentState = GameState.Daytime;

        OnDayChangedSafe?.Invoke();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        if (GameManager.Instance != null) GameManager.Instance.LoadSceneForState(GameState.Daytime);
    }

    /// <summary>
    /// เรียกเมื่อหมดเวลาตอนกลางวัน หรือกดเข้านอน -> เข้าสู่ความฝันคืนนั้น
    /// </summary>
    public void EnterNighttime()
    {
        currentState = GameState.Nighttime;

        OnDayChangedSafe?.Invoke();

        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        if (GameManager.Instance != null) GameManager.Instance.LoadSceneForState(GameState.Nighttime);
    }

    public void SyncWithSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            currentDay = SaveManager.Instance.gameData.currentDay;
            currentState = SaveManager.Instance.gameData.currentState;

            OnDayChangedSafe?.Invoke();
        }
    }
}