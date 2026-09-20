using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
public enum GameState { Daytime, Nighttime }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game States")]
    public GameState currentState = GameState.Nighttime;

    [Header("Ghost Respawn Flag ✨")]
    [Tooltip("บอกระบบว่าการรีโหลดฉากครั้งนี้เกิดจากผีจับได้หรือไม่")]
    public bool wasCaughtByGhost = false;

    [Header("Scene Names Config")]
    public string daytimeSceneName = "DaytimeScene";
    public string tutorialSceneName = "TutorialScene";

    [Tooltip("ใส่ชื่อฉากฝันร้ายเรียงตามวัน เช่น Index 0 = Day 1 Night, Index 1 = Day 2 Night")]
    public string[] nightSceneNames;

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

    public string GetNightSceneNameForCurrentDay()
    {
        int day = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 0;

        if (day <= 0)
        {
            return tutorialSceneName;
        }

        int index = day - 1;
        if (nightSceneNames != null && index >= 0 && index < nightSceneNames.Length)
        {
            return nightSceneNames[index];
        }

        Debug.LogWarning($"[GameManager] ไม่พบชื่อฉากกลางคืนสำหรับ Day {day}! กำลังดึงฉากสุดท้ายมาใช้");
        if (nightSceneNames != null && nightSceneNames.Length > 0)
        {
            return nightSceneNames[nightSceneNames.Length - 1];
        }

        return tutorialSceneName;
    }

    public void LoadSceneForState(GameState state)
    {
        currentState = state;

        // หากมีการย้าย State ปกติ ให้แน่ใจว่า Flag โดนผีจับถูกรีเซ็ต
        wasCaughtByGhost = false;

        if (currentState == GameState.Daytime)
        {
            Debug.Log($"<color=yellow>--- สลับฉากสู่โลกจริง: {daytimeSceneName} ---</color>");
            SceneManager.LoadScene(daytimeSceneName);
        }
        else if (currentState == GameState.Nighttime)
        {
            string targetNightScene = GetNightSceneNameForCurrentDay();
            Debug.Log($"<color=purple>--- สลับฉากสู่โลกฝันร้าย (Day {TimeManager.Instance?.currentDay}): {targetNightScene} ---</color>");
            SceneManager.LoadScene(targetNightScene);
        }
    }

    /// <summary>
    /// เรียกเมื่อผู้เล่นแพ้/ตายในฝันร้าย (โดนผีจับได้)
    /// </summary>
    public void OnPlayerDiedInDream()
    {
        Debug.Log("<color=red><b>ผู้เล่นแพ้ในความฝัน! เพิ่มความเครียดและสะดุ้งตื่นขึ้นใหม่...</b></color>");

        wasCaughtByGhost = true;

        // ✨ ปิดจอดำสนิททันที ก่อนสั่งรีโหลด Scene (จอดำจะไม่ถูกลบเพราะเป็น DontDestroyOnLoad)
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.SetBlackInstant();
        }

        if (StressManager.Instance != null)
        {
            StressManager.Instance.IncreaseStressOnDeath();
        }

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGame();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
    public void OnDreamCleared()
    {
        Debug.Log("<color=green><b>ผู้เล่นออกจากฝันสำเร็จ! ตื่นนอนเข้าสู่ตอนเช้า...</b></color>");
        wasCaughtByGhost = false;

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.ExitDreamToDaytime();
        }
    }
    /// <summary>
    /// สั่งเปลี่ยนฉากแบบมี Fade Out เป็นจอดำก่อนย้ายฉาก
    /// </summary>
    public void LoadSceneWithFade(string sceneName, CanvasGroup fadeCanvasGroup, float fadeDuration = 1.0f)
    {
        StartCoroutine(FadeAndLoadSceneRoutine(sceneName, fadeCanvasGroup, fadeDuration));
    }

    private IEnumerator FadeAndLoadSceneRoutine(string sceneName, CanvasGroup fadeCanvasGroup, float fadeDuration)
    {
        // 1. ค่อยๆ ปรับจอดำจากใส (0) เป็นดำสนิท (1)
        if (fadeCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
                yield return null;
            }
            fadeCanvasGroup.alpha = 1f;
        }

        // 2. รอสั้นๆ ให้มั่นใจว่าจอดำสนิทแล้ว
        yield return new WaitForSeconds(0.1f);

        // 3. โหลดฉากใหม่
        SceneManager.LoadScene(sceneName);
    }
}