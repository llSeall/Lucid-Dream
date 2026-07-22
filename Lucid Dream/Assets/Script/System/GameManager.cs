using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState { Daytime, Nighttime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    public GameState currentState = GameState.Daytime;

    [Header("Scene Names")]
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

    /// <summary>
    /// เปลี่ยนสถานะเกม + เรียกฟังก์ชันใน TimeManager + โหลด Scene ใหม่
    /// </summary>
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

        // สลับ Scene ตามสถานะใหม่
        if (currentState == GameState.Daytime)
        {
            Debug.Log("<color=yellow>--- [GameManager] สลับฉากสู่โลกความจริง (Daytime) ---</color>");
            SceneManager.LoadScene(daytimeSceneName);
        }
        else if (currentState == GameState.Nighttime)
        {
            Debug.Log("<color=purple>--- [GameManager] สลับฉากสู่โลกความฝัน (Nighttime) ---</color>");
            SceneManager.LoadScene(nighttimeSceneName);
        }
    }

    public void LoadSceneForState(GameState state)
    {
        currentState = state;
        if (currentState == GameState.Daytime)
        {
            SceneManager.LoadScene(daytimeSceneName);
        }
        else if (currentState == GameState.Nighttime)
        {
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