using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioFadeIn : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("ระยะเวลาในการเฟดเสียงจนถึงระดับเป้าหมาย (วินาที)")]
    [SerializeField] private float fadeDuration = 3.0f;

    [Tooltip("ระดับความดังสูงสุดที่ต้องการเมื่อเฟดเสร็จ (0.0 ถึง 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float targetVolume = 1.0f;

    [Tooltip("หน่วงเวลาก่อนเริ่มเฟดเสียง (วินาที)")]
    [SerializeField] private float startDelay = 0.5f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // ตั้งค่าความดังเริ่มต้นเป็น 0 เพื่อไม่ให้เสียงโผล่มาทันที
        audioSource.volume = 0f;
    }

    private void Start()
    {
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        // รอดีเลย์เล็กน้อยถ้าตั้งค่าไว้
        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        // เล่นเพลงหากยังไม่ได้กด Play ไว้
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, timer / fadeDuration);
            yield return null;
        }

        // ล็อกค่าความดังให้เท่ากับ targetVolume เมื่อจบ Coroutine
        audioSource.volume = targetVolume;
    }

    // (แถม) ฟังก์ชันเผื่อต้องการสั่ง Fade Out ตอนย้ายฉากหรือเปลี่ยนเพลง
    public void StartFadeOut(float duration)
    {
        StartCoroutine(FadeOutRoutine(duration));
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = audioSource.volume;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, timer / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }
}