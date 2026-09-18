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

    [Tooltip("ใส่ LocalizedString จาก String Table ของ Unity Localization สุ่มเลือกแสดงผล")]
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

    #region 💬 Unity Localization & Background Handling
    private void ShowRandomGlitchText()
    {
        if (glitchTextUI == null || localizedGlitchMessages == null || localizedGlitchMessages.Count == 0) return;

        int randomIndex = Random.Range(0, localizedGlitchMessages.Count);
        LocalizedString selectedLocalizedString = localizedGlitchMessages[randomIndex];

        glitchTextUI.text = selectedLocalizedString.GetLocalizedString();
        glitchTextUI.enabled = true;

        // เปิดพื้นหลังข้อความ
        if (textBackgroundObject != null)
        {
            textBackgroundObject.SetActive(true);
        }
    }

    private void HideGlitchText()
    {
        if (glitchTextUI != null)
        {
            glitchTextUI.enabled = false;
        }

        // ปิดพื้นหลังข้อความ
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