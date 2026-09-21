using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LanguageSetting : MonoBehaviour
{
    private const string LANGUAGE_KEY = "SelectedLanguageCode";

    private IEnumerator Start()
    {
        // ⏳ 1. รอให้ระบบ Unity Localization โหลดไฟล์ตารางภาษาเสร็จสมบูรณ์ก่อน
        yield return LocalizationSettings.InitializationOperation;

        // 📥 2. ดึงรหัสภาษาที่เคยเซฟไว้ (ถ้าไม่มี ให้ใช้ภาษาปัจจุบันของเครื่อง/ระบบ)
        string defaultCode = LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.Identifier.Code
            : "th";

        string savedLanguageCode = PlayerPrefs.GetString(LANGUAGE_KEY, defaultCode);

        // 🌐 3. ปรับภาษาตามค่าที่โหลดมา
        SetLanguageByCode(savedLanguageCode);
    }

    /// <summary>
    /// 🌐 เปลี่ยนภาษาตาม Code (เช่น "th", "en") - แนะนำให้ใช้ฟังก์ชันนี้เป็นหลัก
    /// </summary>
    public void SetLanguageByCode(string languageCode)
    {
        if (LocalizationSettings.AvailableLocales == null) return;

        var locale = LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == languageCode);

        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;

            // บันทึกค่าลง PlayerPrefs เป็น String
            PlayerPrefs.SetString(LANGUAGE_KEY, languageCode);
            PlayerPrefs.Save();

            // ซิงค์ภาษาเข้า SaveManager (ถ้ามี)
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.gameData.selectedLanguage = languageCode;
            }

            Debug.Log($"<color=green><b>[LanguageSetting] เปลี่ยนภาษาเป็น: {languageCode} สำเร็จ!</b></color>");
        }
    }

    /// <summary>
    /// 🔢 เปลี่ยนภาษาตาม Index (0 = TH, 1 = EN)
    /// </summary>
    public void SetLanguage(int index)
    {
        if (LocalizationSettings.AvailableLocales == null) return;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (index >= 0 && index < locales.Count)
        {
            SetLanguageByCode(locales[index].Identifier.Code);
        }
    }
}