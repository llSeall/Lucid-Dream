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

    [Header("🖥️ Display Settings")]
    [Tooltip("ใส่ Toggle สำหรับเปิด/ปิด Fullscreen (ถ้าใช้ Toggle)")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("ใส่ Dropdown สำหรับเลือกโหมด เช่น 0: Fullscreen, 1: Windowed, 2: Borderless (ถ้าใช้ Dropdown)")]
    [SerializeField] private TMP_Dropdown displayModeDropdown;
    [Tooltip("✨ ใส่ Dropdown สำหรับเลือก Resolution เช่น 1920x1080, 1280x720")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;

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
    private const string KEY_FULLSCREEN = "IsFullscreen";
    private const string KEY_DISPLAY_MODE = "DisplayModeIndex";
    private const string KEY_RESOLUTION = "ResolutionIndex";

    private List<Resolution> filteredResolutions = new List<Resolution>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // โหลดและตั้งค่า Min/Max ของ Slider ก่อนผูก Event เพื่อป้องกันการแจ้งเตือนเปลี่ยนค่าโดยไม่ตั้งใจ
        InitResolutionDropdown();
        LoadAndApplyAllSettings();
        SetupUIListeners();
    }

    /// <summary>
    /// ✨ ดึงรายการ Resolution ทั้งหมดที่จอรองรับเข้ามาใส่ใน Dropdown
    /// </summary>
    private void InitResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        Resolution[] allResolutions = Screen.resolutions;
        filteredResolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;
        double currentRefreshRate = Screen.currentResolution.refreshRateRatio.value;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            // กรองเฉพาะ Refresh Rate ที่ตรงกับจอปัจจุบัน เพื่อไม่ให้มี Resolution ซ้ำกัน
            if (Mathf.Approximately((float)allResolutions[i].refreshRateRatio.value, (float)currentRefreshRate))
            {
                filteredResolutions.Add(allResolutions[i]);
                string option = allResolutions[i].width + " x " + allResolutions[i].height;
                options.Add(option);

                if (allResolutions[i].width == Screen.currentResolution.width &&
                    allResolutions[i].height == Screen.currentResolution.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        // กรณีลูปข้างบนไม่มีค่าตรง ให้ดึงทั้งหมดมาใช้งาน
        if (filteredResolutions.Count == 0)
        {
            foreach (var res in allResolutions)
            {
                filteredResolutions.Add(res);
                options.Add(res.width + " x " + res.height);
            }
        }

        resolutionDropdown.AddOptions(options);

        // โหลดค่า Resolution ที่เคยเซฟไว้
        int savedIndex = PlayerPrefs.GetInt(KEY_RESOLUTION, currentResolutionIndex);
        savedIndex = Mathf.Clamp(savedIndex, 0, filteredResolutions.Count - 1);

        resolutionDropdown.value = savedIndex;
        resolutionDropdown.RefreshShownValue();
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

        // --- 2. Display Settings (Fullscreen & Windowed & Resolution) ---
        if (fullscreenToggle != null)
        {
            bool isFullscreen = PlayerPrefs.GetInt(KEY_FULLSCREEN, 1) == 1;
            fullscreenToggle.isOn = isFullscreen;
            SetFullscreen(isFullscreen);
        }

        if (displayModeDropdown != null)
        {
            int modeIndex = PlayerPrefs.GetInt(KEY_DISPLAY_MODE, 0);
            displayModeDropdown.value = modeIndex;
            SetDisplayMode(modeIndex);
        }

        if (resolutionDropdown != null && filteredResolutions.Count > 0)
        {
            int resIndex = PlayerPrefs.GetInt(KEY_RESOLUTION, resolutionDropdown.value);
            SetResolution(resIndex);
        }

        // --- 3. Mouse Sensitivity ---
        float defaultSens = (player != null) ? player.GetDefaultSensitivity() : 2f;
        if (defaultSens <= 0) defaultSens = 2f;

        if (sensitivitySlider != null)
        {
            sensitivitySlider.minValue = 0.1f;
            sensitivitySlider.maxValue = defaultSens * 2f;

            float sensitivity = PlayerPrefs.GetFloat(KEY_SENSITIVITY, defaultSens);
            sensitivitySlider.value = sensitivity;
            SetMouseSensitivity(sensitivity);
        }

        // --- 4. Camera FOV ---
        float defaultFOV = (player != null) ? player.GetDefaultFOV() : 60f;
        if (defaultFOV <= 0) defaultFOV = 60f;

        if (fovSlider != null)
        {
            fovSlider.minValue = Mathf.Max(10f, defaultFOV - 30f);
            fovSlider.maxValue = defaultFOV + 60f;

            float fov = PlayerPrefs.GetFloat(KEY_FOV, defaultFOV);
            fovSlider.value = fov;
            SetFOV(fov);
        }

        // --- 5. Language ---
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

        if (fullscreenToggle) fullscreenToggle.onValueChanged.AddListener(SetFullscreen);
        if (displayModeDropdown) displayModeDropdown.onValueChanged.AddListener(SetDisplayMode);
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);

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

    #region --- Display Settings System ---
    /// <summary>
    /// ✨ เปลี่ยนความละเอียดหน้าจอ (Resolution)
    /// </summary>
    public void SetResolution(int index)
    {
        if (filteredResolutions == null || index < 0 || index >= filteredResolutions.Count) return;

        Resolution res = filteredResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt(KEY_RESOLUTION, index);
        PlayerPrefs.Save();

        Debug.Log($"<color=yellow>[Display Settings] Resolution set to: {res.width}x{res.height}</color>");
    }

    /// <summary>
    /// สลับโหมดเต็มจอด้วย Toggle (True = เต็มจอ, False = หน้าต่าง)
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(KEY_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();

        Debug.Log($"<color=yellow>[Display Settings] Fullscreen Status: {isFullscreen}</color>");
    }

    /// <summary>
    /// สลับโหมดหน้าจอด้วย Dropdown 
    /// 0: Fullscreen (Exclusive)
    /// 1: Windowed
    /// 2: Borderless Window
    /// </summary>
    public void SetDisplayMode(int index)
    {
        switch (index)
        {
            case 0:
                Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
                break;
            case 1:
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            case 2:
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
        }

        PlayerPrefs.SetInt(KEY_DISPLAY_MODE, index);
        PlayerPrefs.Save();

        Debug.Log($"<color=yellow>[Display Settings] Mode set to: {Screen.fullScreenMode}</color>");
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
        if (LocalizationSettings.AvailableLocales != null && LocalizationSettings.AvailableLocales.Locales.Count > index)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
            PlayerPrefs.SetInt(KEY_LANGUAGE, index);
            PlayerPrefs.Save();
        }
    }
    #endregion
}