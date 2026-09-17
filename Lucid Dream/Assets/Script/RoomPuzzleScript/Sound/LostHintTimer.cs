using UnityEngine;

public class LostHintTimer : MonoBehaviour
{
    [Header("⏱️ Timer Settings")]
    [Tooltip("ระยะเวลาที่ต้องรอให้ผู้เล่นหลงทาง ก่อนจะเล่นเสียงใบ้ (วินาที)")]
    [SerializeField] private float timeToHint = 60f;

    [Tooltip("วนเล่นเสียงใบ้ซ้ำทุกๆ X วินาที หากผู้เล่นยังคงหาทางออกไม่ได้")]
    [SerializeField] private bool repeatHint = true;
    [Tooltip("ระยะเวลาเว้นช่วงเล่นเสียงซ้ำ (วินาที)")]
    [SerializeField] private float repeatInterval = 30f;

    [Header("🔊 Audio Settings")]
    [Tooltip("AudioSource สำหรับเล่นเสียงใบ้ (ตั้งค่า Spatial Blend = 1.0 เพื่อให้เป็นเสียง 3D)")]
    [SerializeField] private AudioSource hintAudioSource;
    [Tooltip("ไฟล์เสียงคำใบ้/เสียงเคาะ/เสียงกระซิบ")]
    [SerializeField] private AudioClip hintSoundClip;

    [Header("🔒 State")]
    [Tooltip("ให้เริ่มนับเวลาตั้งแต่เริ่มเกมเลยหรือไม่ (ถ้าทำเป็นระบบโซน แนะนำให้ติ๊กออก)")]
    [SerializeField] private bool isTimerActive = false;

    private float timer = 0f;
    private bool hasPlayedFirstTime = false;
    private bool isPermanentlyDisabled = false;

    private void Start()
    {
        if (hintAudioSource == null)
        {
            hintAudioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (!isTimerActive || isPermanentlyDisabled) return;

        timer += Time.deltaTime;

        if (!hasPlayedFirstTime)
        {
            if (timer >= timeToHint)
            {
                PlayHintSound();
                hasPlayedFirstTime = true;
                timer = 0f;
            }
        }
        else if (repeatHint)
        {
            if (timer >= repeatInterval)
            {
                PlayHintSound();
                timer = 0f;
            }
        }
    }

    private void PlayHintSound()
    {
        if (hintAudioSource != null && hintSoundClip != null)
        {
            hintAudioSource.PlayOneShot(hintSoundClip);
            Debug.Log($"[Hint System] เล่นเสียงใบ้ ณ ตำแหน่ง: {hintAudioSource.transform.position}");
        }
    }

    /// <summary>
    /// ✨ ฟังก์ชันเริ่มนับเวลา (เรียกใช้เมื่อผู้เล่นเดินเหยียบเข้ามาในโซน)
    /// </summary>
    public void StartHintTimer()
    {
        if (isPermanentlyDisabled) return;

        isTimerActive = true;
        timer = 0f; // รีเซ็ตเวลาเริ่มต้นนับใหม่
        Debug.Log("[Hint System] ▶️ ผู้เล่นเข้าโซน: เริ่มนับเวลาเสียงใบ้แล้ว");
    }

    /// <summary>
    /// ✨ ปิดระบบเสียงใบ้นี้อย่างถาวร (เรียกใช้เมื่อออกจากโซนหรือแก้พัซเซิลสำเร็จ)
    /// </summary>
    public void DisableHintPermanently()
    {
        isPermanentlyDisabled = true;
        isTimerActive = false;

        if (hintAudioSource != null && hintAudioSource.isPlaying)
        {
            hintAudioSource.Stop();
        }

        Debug.Log("[Hint System] 🛑 ปิดระบบเสียงใบ้ของโซนนี้เรียบร้อยแล้ว");
    }
}