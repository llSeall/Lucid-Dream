using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using TMPro;

public class GhostStaticEffectUI : MonoBehaviour
{
    public static GhostStaticEffectUI Instance { get; private set; }

    [Header("📺 Static Screen Overlay UI")]
    public RawImage staticOverlayImage;
    public Texture2D[] staticTextures;

    [Header("💬 Glitch Text UI (Unity Localization) ✨")]
    [Tooltip("UI TextMeshProUGUI สำหรับแสดงข้อความ")]
    public TextMeshProUGUI glitchTextUI;

    [Tooltip("GameObject หรือ Panel พื้นหลังของข้อความ (จะติด-ดับ พร้อมกับข้อความ)")]
    public GameObject textBackgroundObject;

    [Tooltip("ใส่ LocalizedString จาก String Table ของ Unity Localization สุ่มเลือกแสดงผล (ใช้กับระบบผี)")]
    public List<LocalizedString> localizedGlitchMessages = new List<LocalizedString>();

    [Header("🔊 Static Sound Settings")]
    public AudioSource staticAudioSource;
    public AudioClip staticAudioClip;
    [Range(0f, 1f)] public float maxStaticVolume = 0.8f;

    [Header("⚡ Horror Glitch Rhythm")]
    public float minGlitchDuration = 0.08f;
    public float maxGlitchDuration = 0.3f;
    public float minPauseClose = 0.15f;
    public float maxPauseClose = 0.5f;
    public float minPauseFar = 1.5f;
    public float maxPauseFar = 4.0f;
    public float frameRate = 0.03f;

    private float currentIntensity = 0f;
    private bool isGlitching = false;
    private float stateTimer = 0f;
    private float frameTimer = 0f;
    private int currentFrameIndex = 0;

    // ตัวแปรสำหรับรองรับข้อความพิเศษภายนอก (เช่น ไอเท็มใน StaticVanishTrigger)
    private bool isCustomTextActive = false;
    private string customTextMessage = "";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (staticAudioSource != null && staticAudioClip != null)
        {
            staticAudioSource.clip = staticAudioClip;
            staticAudioSource.loop = true;
            staticAudioSource.volume = 0f;
            staticAudioSource.Play();
        }

        SetOverlayAlpha(0f);
        HideGlitchText();
        ScheduleNextPause();
    }

    private void Update()
    {
        stateTimer -= Time.deltaTime;

        if (isGlitching)
        {
            AnimateNoiseTexture();

            float alpha = Mathf.Lerp(0.5f, 1f, currentIntensity);
            SetOverlayAlpha(alpha);

            if (staticAudioSource != null)
            {
                staticAudioSource.volume = currentIntensity * maxStaticVolume;
            }

            if (stateTimer <= 0f)
            {
                isGlitching = false;
                HideGlitchText();
                ScheduleNextPause();
            }
        }
        else
        {
            SetOverlayAlpha(0f);
            HideGlitchText();

            if (staticAudioSource != null)
            {
                staticAudioSource.volume = 0f;
            }

            if (stateTimer <= 0f && currentIntensity > 0.05f)
            {
                isGlitching = true;
                stateTimer = Random.Range(minGlitchDuration, maxGlitchDuration);

                ShowRandomGlitchText();
            }
        }

        currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * 3f);
    }

    #region 💬 Custom Item Message Handling
    /// <summary>
    /// ฟังก์ชันสั่งให้แสดงจอซ่าพร้อมข้อความเฉพาะ (จะล็อกไม่ให้ข้อความผีสุ่มขึ้นมาทับ)
    /// </summary>
    public void TriggerCustomGlitch(float intensity, string message, float duration)
    {
        currentIntensity = intensity;
        isGlitching = true;
        stateTimer = duration;

        if (!string.IsNullOrEmpty(message))
        {
            isCustomTextActive = true;
            customTextMessage = message;

            if (glitchTextUI != null)
            {
                glitchTextUI.text = customTextMessage;
                glitchTextUI.enabled = true;
            }

            if (textBackgroundObject != null)
            {
                textBackgroundObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// ล้างข้อความพิเศษและปิดจอซ่า
    /// </summary>
    public void ClearCustomGlitch()
    {
        isCustomTextActive = false;
        customTextMessage = "";
        currentIntensity = 0f;
        isGlitching = false;

        if (glitchTextUI != null) glitchTextUI.enabled = false;
        if (textBackgroundObject != null) textBackgroundObject.SetActive(false);
        SetOverlayAlpha(0f);
    }
    #endregion

    #region 💬 Unity Localization & Background Handling
    private void ShowRandomGlitchText()
    {
        // ถ้ามีการใช้ข้อความพิเศษจากไอเท็มอยู่ ให้แสดงข้อความไอเท็มต่อ ไม่ต้องสุ่มข้อความผี
        if (isCustomTextActive)
        {
            if (glitchTextUI != null)
            {
                glitchTextUI.text = customTextMessage;
                glitchTextUI.enabled = true;
            }
            if (textBackgroundObject != null)
            {
                textBackgroundObject.SetActive(true);
            }
            return;
        }

        if (glitchTextUI == null || localizedGlitchMessages == null || localizedGlitchMessages.Count == 0) return;

        int randomIndex = Random.Range(0, localizedGlitchMessages.Count);
        LocalizedString selectedLocalizedString = localizedGlitchMessages[randomIndex];

        glitchTextUI.text = selectedLocalizedString.GetLocalizedString();
        glitchTextUI.enabled = true;

        if (textBackgroundObject != null)
        {
            textBackgroundObject.SetActive(true);
        }
    }

    private void HideGlitchText()
    {
        // ถ้าอยู่ในโหมดข้อความพิเศษของไอเท็ม จะไม่ซ่อนข้อความอัตโนมัติจนกว่าไอเท็มจะสั่ง Clear
        if (isCustomTextActive) return;

        if (glitchTextUI != null)
        {
            glitchTextUI.enabled = false;
        }

        if (textBackgroundObject != null)
        {
            textBackgroundObject.SetActive(false);
        }
    }
    #endregion

    private void ScheduleNextPause()
    {
        float minPause = Mathf.Lerp(minPauseFar, minPauseClose, currentIntensity);
        float maxPause = Mathf.Lerp(maxPauseFar, maxPauseClose, currentIntensity);
        stateTimer = Random.Range(minPause, maxPause);
    }

    private void AnimateNoiseTexture()
    {
        if (staticTextures != null && staticTextures.Length > 0)
        {
            frameTimer += Time.deltaTime;
            if (frameTimer >= frameRate)
            {
                frameTimer = 0f;
                currentFrameIndex = (currentFrameIndex + 1) % staticTextures.Length;
                if (staticOverlayImage != null)
                {
                    staticOverlayImage.texture = staticTextures[currentFrameIndex];
                }
            }
        }
    }

    private void SetOverlayAlpha(float alpha)
    {
        if (staticOverlayImage != null)
        {
            Color c = staticOverlayImage.color;
            c.a = alpha;
            staticOverlayImage.color = c;
        }
    }

    public void ReportGhostDistance(float intensity)
    {
        if (intensity > currentIntensity)
        {
            currentIntensity = intensity;
        }
    }
}