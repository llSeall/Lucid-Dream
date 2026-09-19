using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;
using TMPro;

public class LanguageDropdownController : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown languageDropdown;

    private IEnumerator Start()
    {
        // 1. รอให้ระบบ Localization โหลดภาษาทั้งหมดให้เสร็จเรียบร้อยก่อน
        yield return LocalizationSettings.InitializationOperation;

        if (languageDropdown == null)
            languageDropdown = GetComponent<TMP_Dropdown>();

        if (languageDropdown == null) yield break;

        // 2. เคลียร์ตัวเลือกเก่าใน Dropdown ออกให้หมด
        languageDropdown.ClearOptions();

        List<string> options = new List<string>();
        int selectedIndex = 0;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var currentLocale = LocalizationSettings.SelectedLocale;

        // 3. วนลูปอ่านภาษาทั้งหมดที่มีใน Unity Localization
        for (int i = 0; i < locales.Count; i++)
        {
            var locale = locales[i];

            // ดึงชื่อภาษาแบบเป็นทางการหรือภาษาท้องถิ่น (เช่น "ไทย", "English")
            string languageName = locale.Identifier.CultureInfo != null
                ? locale.Identifier.CultureInfo.NativeName
                : locale.name;

            // ตัวอย่าง: แปลงตัวอักษรแรกเป็นตัวใหญ่เพื่อความสวยงาม
            if (!string.IsNullOrEmpty(languageName))
            {
                languageName = char.ToUpper(languageName[0]) + languageName.Substring(1);
            }

            options.Add(languageName);

            // เช็กว่าภาษาไหนเป็นภาษาที่เปิดใช้งานอยู่ปัจจุบัน
            if (locale == currentLocale)
            {
                selectedIndex = i;
            }
        }

        // 4. ใส่รายชื่อภาษาลงใน Dropdown
        languageDropdown.AddOptions(options);
        languageDropdown.value = selectedIndex;
        languageDropdown.RefreshShownValue();

        // 5. ผูก Event เมื่อผู้เล่นกดเปลี่ยนภาษา
        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
    }

    private void OnLanguageChanged(int index)
    {
        // เปลี่ยนภาษาผ่าน SettingsManager หรือ LocalizationSettings
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.SetLanguage(index);
        }
        else
        {
            var selectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
            LocalizationSettings.SelectedLocale = selectedLocale;
            PlayerPrefs.SetInt("SelectedLanguage", index);
            PlayerPrefs.Save();
        }
    }
}