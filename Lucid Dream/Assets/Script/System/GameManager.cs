using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Daytime, Nighttime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    public GameState currentState = GameState.Daytime;

    [Header("Scene Names (ต้องสะกดให้ตรงกับใน Unity)")]
    public string daytimeSceneName = "DaytimeScene";
    public string nighttimeSceneName = "NighttimeScene";

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

    public void ChangeState(GameState newState)
    {
        currentState = newState;

        if (TimeManager.Instance != null)
        {
            if (currentState == GameState.Daytime)
            {
                TimeManager.Instance.StartNewDay();
            }
            else if (currentState == GameState.Nighttime)
            {
                TimeManager.Instance.EnterDreamWorld();
            }
        }
        else
        {
            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        }

        if (currentState == GameState.Daytime)
        {
            Debug.Log("<color=yellow>--- [GameManager] กำลังตื่นนอน... โหลดเข้าสู่โลกความจริง (กลางวัน) ---</color>");
            SceneManager.LoadScene(daytimeSceneName);
        }
        else if (currentState == GameState.Nighttime)
        {
            Debug.Log("<color=purple>--- [GameManager] กำลังเข้าสู่ห้วงนิทรา... โหลดเข้าสู่โลกความฝัน (กลางคืน) ---</color>");
            SceneManager.LoadScene(nighttimeSceneName);
        }
    }

    public void LoadSceneForState(GameState state)
    {
        currentState = state;
        if (currentState == GameState.Daytime)
        {
            Debug.Log("<color=yellow>--- [GameManager] โหลดฉากโลกความจริงจากเซฟเก่าสำเร็จ ---</color>");
            SceneManager.LoadScene(daytimeSceneName);
        }
        else if (currentState == GameState.Nighttime)
        {
            Debug.Log("<color=purple>--- [GameManager] โหลดฉากโลกความฝันจากเซฟเก่าสำเร็จ ---</color>");
            SceneManager.LoadScene(nighttimeSceneName);
        }
    }

    private void Update()
    {
        HandleCheatKeys();
    }

    private void HandleCheatKeys()
    {
#if ENABLE_INPUT_SYSTEM        
        if (UnityEngine.InputSystem.Keyboard.current == null) return;
        if (UnityEngine.InputSystem.Keyboard.current.f1Key.wasPressedThisFrame) ChangeState(GameState.Daytime);
        if (UnityEngine.InputSystem.Keyboard.current.f2Key.wasPressedThisFrame) ChangeState(GameState.Nighttime);
#else
        if (Input.GetKeyDown(KeyCode.F1)) ChangeState(GameState.Daytime);
        if (Input.GetKeyDown(KeyCode.F2)) ChangeState(GameState.Nighttime);
#endif
    }
}