using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Localization.Settings;
using System.Collections.Generic;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("🔊 Audio Settings")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("🖱️ Controls & Camera Settings")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider fovSlider;

    [Header("🌐 Language Settings")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private const string KEY_MASTER_VOL = "MasterVolume";
    private const string KEY_BGM_VOL = "BGMVolume";
    private const string KEY_SFX_VOL = "SFXVolume";
    private const string KEY_SENSITIVITY = "MouseSensitivity";
    private const string KEY_FOV = "CameraFOV";
    private const string KEY_LANGUAGE = "SelectedLanguage";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // โหลดและตั้งค่า Min/Max ของ Slider ก่อนผูก Event เพื่อป้องกันการแจ้งเตือนเปลี่ยนค่าโดยไม่ตั้งใจ
        LoadAndApplyAllSettings();
        SetupUIListeners();
    }

    private void LoadAndApplyAllSettings()
    {
        PlayerController3D_InputAction player = FindObjectOfType<PlayerController3D_InputAction>();

        // --- 1. Audio ---
        float masterVol = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
        float bgmVol = PlayerPrefs.GetFloat(KEY_BGM_VOL, 1f);
        float sfxVol = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);

        if (masterVolumeSlider) masterVolumeSlider.value = masterVol;
        if (bgmVolumeSlider) bgmVolumeSlider.value = bgmVol;
        if (sfxVolumeSlider) sfxVolumeSlider.value = sfxVol;

        SetMasterVolume(masterVol);
        SetBGMVolume(bgmVol);
        SetSFXVolume(sfxVol);

        // --- 2. Mouse Sensitivity ---
        float defaultSens = (player != null) ? player.GetDefaultSensitivity() : 2f;
        if (defaultSens <= 0) defaultSens = 2f;

        if (sensitivitySlider != null)
        {
            // ตั้งค่า Min และ Max ให้อยู่รอบๆ ค่าเริ่มต้น เพื่อให้ค่าเริ่มต้นอยู่ "ตรงกลางหลอด" พอดี
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = defaultSens * 2f; // ตัวอย่าง: ถ้าตั้งใน Inspector ไว้ 2 -> Max จะเท่ากับ 4 (ตรงกลางคือ 2 พอดี)

            float sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, defaultSens);
            sensitivitySlider.value = sensitivity;
            SetMouseSensitivity(sensitivity);
        }

        // --- 3. Camera FOV ---
        float defaultFOV = (player != null) ? player.GetDefaultFOV() : 60f;
        if (defaultFOV <= 0) defaultFOV = 60f;

        if (fovSlider != null)
        {
            // ตั้งค่า Min และ Max ให้ครอบคลุม ±30 จากค่าเริ่มต้น เพื่อให้ค่าเริ่มต้นอยู่ "ตรงกลางหลอด"
            fovSlider.minValue = Mathf.Max(10f, defaultFOV - 30f); // ตัวอย่าง: ถ้าตั้งกล้องไว้ 60 -> Min = 30
            fovSlider.maxValue = defaultFOV + 60f;                  // Max = 90 (ตรงกลางคือ 60 พอดี)

            float fov = PlayerPrefs.GetFloat(KEY_FOV, defaultFOV);
            fovSlider.value = fov;
            SetFOV(fov);
        }

        // --- 4. Language ---
        if (languageDropdown != null)
        {
            int langIndex = PlayerPrefs.GetInt(KEY_LANGUAGE, 0);
            languageDropdown.value = langIndex;
            SetLanguage(langIndex);
        }
    }

    private void SetupUIListeners()
    {
        if (masterVolumeSlider) masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        if (bgmVolumeSlider) bgmVolumeSlider.onValueChanged.AddListener(SetBGMVolume);
        if (sfxVolumeSlider) sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetMouseSensitivity);
        if (fovSlider) fovSlider.onValueChanged.AddListener(SetFOV);

        if (languageDropdown) languageDropdown.onValueChanged.AddListener(SetLanguage);
    }

    #region --- Audio System ---
    public void SetMasterVolume(float value)
    {
        if (mainAudioMixer != null)
            mainAudioMixer.SetFloat("MasterVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_MASTER_VOL, value);
    }

    public void SetBGMVolume(float value)
    {
        if (mainAudioMixer != null)
            mainAudioMixer.SetFloat("BGMVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_BGM_VOL, value);
    }

    public void SetSFXVolume(float value)
    {
        if (mainAudioMixer != null)
            mainAudioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
        PlayerPrefs.SetFloat(KEY_SFX_VOL, value);
    }

    private float LinearToDecibel(float linear)
    {
        return linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f;
    }
    #endregion

    #region --- Controls & Camera System ---
    public void SetMouseSensitivity(float value)
    {
        PlayerPrefs.SetFloat(KEY_SENSITIVITY, value);

        PlayerController3D_InputAction player = FindObjectOfType<PlayerController3D_InputAction>();
        if (player != null)
        {
            player.SetMouseSensitivity(value);
        }
    }

    public void SetFOV(float value)
    {
        PlayerPrefs.SetFloat(KEY_FOV, value);

        PlayerController3D_InputAction player = FindObjectOfType<PlayerController3D_InputAction>();
        if (player != null)
        {
            player.SetFOV(value);
        }
    }
    #endregion

    #region --- Language System ---
    public void SetLanguage(int index)
    {
        if (LocalizationSettings.AvailableLocales.Locales.Count > index)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
            PlayerPrefs.SetInt(KEY_LANGUAGE, index);
            PlayerPrefs.Save();
        }
    }
    #endregion
}