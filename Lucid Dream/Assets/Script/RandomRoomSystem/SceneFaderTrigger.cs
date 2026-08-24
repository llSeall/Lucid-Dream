using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFaderTrigger : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("ชื่อของ Scene ที่ต้องการย้ายไป (ต้องตรงกับชื่อใน Build Settings)")]
    [SerializeField] private string targetSceneName;

    [Header("Fade Settings")]
    [Tooltip("CanvasGroup ของภาพสีดำที่จะใช้ทำจางหน้าจอ")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;

    [Tooltip("ระยะเวลาในการค่อยๆ ดำสนิท (วินาที)")]
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Player Settings")]
    [SerializeField] private string playerTag = "Player";

    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) && !isTriggered)
        {
            isTriggered = true;
            StartCoroutine(FadeAndChangeScene());
        }
    }

    private IEnumerator FadeAndChangeScene()
    {
        float timer = 0f;

        // ค่อยๆ ปรับ Alpha จาก 0 (ใส) ให้เป็น 1 (ดำสนิท)
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            }
            yield return null;
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
        }

        // ย้ายไปยัง Scene เป้าหมาย
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError("ยังไม่ได้ใส่ชื่อ Target Scene Name ใน Inspector!");
        }
    }
}