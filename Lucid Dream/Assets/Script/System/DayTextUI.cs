using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class DayTextUI : MonoBehaviour
{
    [Header("📺 UI References")]
    [SerializeField] private TextMeshProUGUI dayText;

    [Header("🌐 Localization Settings")]
    [Tooltip("ดึงข้อความแปลภาษา เช่น 'Day {0}' หรือ 'วันที่ {0}' จาก String Table")]
    [SerializeField] private LocalizedString dayLocalizedString;

    [Tooltip("ข้อความสำรอง กรณีไม่ได้ตั้งค่า LocalizedString (ใช้อย่างเช่น 'วันที่ {0}')")]
    [SerializeField] private string fallbackFormat = "วันที่ {0}";

    private void OnEnable()
    {
        // 1. ลงทะเบียน Event เมื่อมีการเปลี่ยนวัน และเมื่อผู้เล่นกดเปลี่ยนภาษา
        TimeManager.OnDayChangedSafe += UpdateDayUI;
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // อัปเดตข้อความทันทีเมื่อ Object เปิดใช้งาน
        UpdateDayUI();
    }

    private void OnDisable()
    {
        // ยกเลิกการลงทะเบียนเพื่อป้องกัน Memory Leak
        TimeManager.OnDayChangedSafe -= UpdateDayUI;
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        UpdateDayUI();
    }

    private void Start()
    {
        UpdateDayUI();
    }

    public void UpdateDayUI()
    {
        if (dayText == null)
            dayText = GetComponent<TextMeshProUGUI>();

        if (dayText != null && TimeManager.Instance != null)
        {
            int currentDay = TimeManager.Instance.currentDay;

            // ✨ ดึงคำแปลจาก String Table พร้อมแทนค่าตัวเลขอัตโนมัติใน {0}
            if (dayLocalizedString != null && !dayLocalizedString.IsEmpty)
            {
                dayLocalizedString.Arguments = new object[] { currentDay };
                dayText.text = dayLocalizedString.GetLocalizedString();
            }
            else
            {
                // ✨ ข้อความสำรองหากยังไม่ได้ตั้งค่า LocalizedString
                dayText.text = string.Format(fallbackFormat, currentDay);
            }
        }
    }
}