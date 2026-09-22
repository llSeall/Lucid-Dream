using System.Collections;
using UnityEngine;
using UnityEngine.UI; // สำหรับจัดการ Image UI
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelResetTrigger : MonoBehaviour
{
    [Header("Reset Settings")]
    [Tooltip("Tag ของตัวละครผู้เล่น")]
    public string playerTag = "Player";

    [Tooltip("ระยะเวลาหน่วงเพิ่มเติมหลังจากภาพมืดสนิทแล้ว ก่อนจะเริ่มฉากใหม่ (วินาที)")]
    public float delayAfterFade = 0.2f;

    [Header("Fade Screen Settings ✨")]
    [Tooltip(" Image สีดำบน Canvas (ขยายเต็มจอ) ที่ใช้สำหรับทำภาพมืด")]
    public Image fadeImage;

    [Tooltip("หรือจะใช้ CanvasGroup ของ UI ก็ได้ (เลือกใส่อย่างใดอย่างหนึ่ง)")]
    public CanvasGroup fadeCanvasGroup;

    [Tooltip("ระยะเวลา (วินาที) ที่ภาพจะค่อยๆ เฟดมืดลงจนมองไม่เห็น")]
    public float fadeDuration = 1.0f;

    [Header("Audio Effects (Optional)")]
    public AudioClip resetSound;
    private AudioSource audioSource;

    private bool isResetting = false;

    private void Start()
    {
        // บังคับให้ Collider บน Object นี้เป็น Is Trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        // ดึง AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && resetSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // รีเซ็ตค่าความใสดำ (Alpha) ให้เป็น 0 (โปร่งใส) ตอนเริ่มเกม
        ResetFadeAlpha();
    }

    private void ResetFadeAlpha()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
        }

        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isResetting) return;

        if (other.CompareTag(playerTag))
        {
            isResetting = true;
            StartCoroutine(ResetLevelRoutine());
        }
    }

    private IEnumerator ResetLevelRoutine()
    {
        // 1. เล่นเสียงถ้าใส่ไว้
        if (resetSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(resetSound);
        }

        // 2. เริ่มกระบวนการ Fade to Black (ค่อยๆ มืดลง)
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / fadeDuration);

            if (fadeCanvasGroup != null)
            {
                fadeCanvasGroup.alpha = progress;
            }
            if (fadeImage != null)
            {
                Color color = fadeImage.color;
                color.a = progress;
                fadeImage.color = color;
            }

            yield return null;
        }

        // บังคับให้ดำสนิท 100%
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;
        if (fadeImage != null)
        {
            Color color = fadeImage.color;
            color.a = 1f;
            fadeImage.color = color;
        }

        // 3. หน่วงเวลาเล็กน้อยหลังจากภาพมืดสนิท
        if (delayAfterFade > 0f)
        {
            yield return new WaitForSeconds(delayAfterFade);
        }

        // 4. โหลดฉากปัจจุบันขึ้นมาใหม่
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
        Collider col = GetComponent<Collider>();

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}