using UnityEngine;
using UnityEngine.Localization.Settings;

public class LanguageSetting : MonoBehaviour
{
    private const string LANGUAGE_KEY = "SelectedLanguage";

    private int languageIndex = 0;

    private void Start()
    {
        languageIndex = PlayerPrefs.GetInt(LANGUAGE_KEY, GetCurrentLanguageIndex());

        UpdateLanguage();
    }

    public void SetLanguageByCode(string languageCode)
    {
        var locale = LocalizationSettings.AvailableLocales.Locales.Find(l => l.Identifier.Code == languageCode);

        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;

            PlayerPrefs.SetString(LANGUAGE_KEY, languageCode);
            PlayerPrefs.Save();
        }
    }

    public void SetLanguage(int index)
    {
        if (index < 0 || index >= LocalizationSettings.AvailableLocales.Locales.Count)
            return;

        var selectedLocale = LocalizationSettings.AvailableLocales.Locales[index];
        LocalizationSettings.SelectedLocale = selectedLocale;

        PlayerPrefs.SetInt(LANGUAGE_KEY, index);
        PlayerPrefs.Save();
    }

    private void UpdateLanguage()
    {
        var selectedLocale = LocalizationSettings.AvailableLocales.Locales[languageIndex];
        LocalizationSettings.SelectedLocale = selectedLocale;

        PlayerPrefs.SetInt(LANGUAGE_KEY, languageIndex);
        PlayerPrefs.Save();
    }

    private int GetCurrentLanguageIndex()
    {
        var currentLocale = LocalizationSettings.SelectedLocale;
        return LocalizationSettings.AvailableLocales.Locales.IndexOf(currentLocale);
    }
}