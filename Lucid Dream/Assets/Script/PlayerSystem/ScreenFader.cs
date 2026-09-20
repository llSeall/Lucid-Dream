using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Header("Main Menu / Initial Scene Settings")]
    [Tooltip("ติ๊กถูกเพื่อให้จอดำค่อยๆ เลือนสว่างออกเองเมื่อเริ่มเกมที่หน้า Main Menu")]
    [SerializeField] private bool autoFadeInOnStart = true;
    [SerializeField] private float startFadeDuration = 1.2f;

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
            return;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f; // บังคับดำสนิทไว้ก่อนในเฟรมแรก
        }
    }

    private void Start()
    {
        // ถ้าเปิดเกมมาหน้า Main Menu ให้ค่อยๆ เลือนจอดำออกเองอัตโนมัติ
        if (autoFadeInOnStart)
        {
            FadeToClear(startFadeDuration);
        }
    }

    public void SetBlackInstant()
    {
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;
    }

    public Coroutine FadeToClear(float duration)
    {
        return StartCoroutine(FadeRoutine(1f, 0f, duration));
    }

    public Coroutine FadeToBlack(float duration)
    {
        return StartCoroutine(FadeRoutine(0f, 1f, duration));
    }

    private IEnumerator FadeRoutine(float startAlpha, float targetAlpha, float duration)
    {
        if (fadeCanvasGroup == null) yield break;

        float timer = 0f;
        fadeCanvasGroup.alpha = startAlpha;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }
}