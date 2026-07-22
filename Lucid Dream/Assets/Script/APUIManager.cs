using UnityEngine;
using UnityEngine.UI; // เพิ่มเผื่อใช้งาน Slider หรือ Image
using TMPro;

public class APUIManager : MonoBehaviour
{
    [Header("📺 Time & AP UI Elements")]
    [SerializeField] private TextMeshProUGUI apText;    // แสดง AP เช่น "AP: 3 / 3"
    [SerializeField] private TextMeshProUGUI dayText;   // แสดงวัน เช่น "Day 1"

    [Header("🧠 Sanity UI Elements")]
    [SerializeField] private TextMeshProUGUI sanityText; // ข้อความแสดงค่าสติ เช่น "สติ: 100 / 100"
    [SerializeField] private Slider sanitySlider;        // (ตัวเลือกเสริม) หลอดค่าสติ
    [SerializeField] private Image sanityFillImage;      // (ตัวเลือกเสริม) สีของหลอดสติ

    private void OnEnable()
    {
        // 1. ดักฟัง Event จาก TimeManager
        TimeManager.OnAPChanged += UpdateTimeUI;
        TimeManager.OnDayChangedSafe += UpdateTimeUI;

        // 2. ✨ ดักฟัง Event จาก PlayerStats เมื่อค่าสติเปลี่ยน
        PlayerStats.OnSanityChanged += UpdateSanityUI;
    }

    private void OnDisable()
    {
        TimeManager.OnAPChanged -= UpdateTimeUI;
        TimeManager.OnDayChangedSafe -= UpdateTimeUI;

        // ถอดการดักฟังเมื่อ UI ถูกปิด
        PlayerStats.OnSanityChanged -= UpdateSanityUI;
    }

    private void Start()
    {
        // อัปเดตข้อมูลครั้งแรกเมื่อเปิดเกม
        UpdateTimeUI();

        if (PlayerStats.Instance != null)
        {
            UpdateSanityUI(PlayerStats.Instance.currentSanity, PlayerStats.Instance.maxSanity);
        }
    }

    /// <summary>
    /// อัปเดตข้อความเวลา และ AP
    /// </summary>
    public void UpdateTimeUI()
    {
        if (TimeManager.Instance == null) return;

        if (apText != null)
        {
            if (TimeManager.Instance.currentState == GameState.Daytime)
            {
                apText.text = $"AP: {TimeManager.Instance.currentAP} / {TimeManager.Instance.maxAP}";
            }
            else
            {
                apText.text = "AP: - (Nighttime)";
            }
        }

        if (dayText != null)
        {
            dayText.text = $"Day {TimeManager.Instance.currentDay}";
        }
    }

    /// <summary>
    /// ✨ อัปเดต UI ค่าสติ (รับค่าส่งมาจาก Event ของ PlayerStats)
    /// </summary>
    public void UpdateSanityUI(float currentSanity, float maxSanity)
    {
        // อัปเดตตัวหนังสือ
        if (sanityText != null)
        {
            sanityText.text = $"🧠 สติ: {Mathf.RoundToInt(currentSanity)} / {Mathf.RoundToInt(maxSanity)}";
        }

        // อัปเดตหลอด Slider (ถ้ามี)
        if (sanitySlider != null)
        {
            sanitySlider.maxValue = maxSanity;
            sanitySlider.value = currentSanity;

            // เปลี่ยนสีหลอดตามวิกฤตสุขภาพจิต
            if (sanityFillImage != null)
            {
                float ratio = currentSanity / maxSanity;
                if (ratio <= 0.3f) sanityFillImage.color = Color.red;       // วิกฤต (ต่ำกว่า 30%)
                else if (ratio <= 0.6f) sanityFillImage.color = Color.yellow; // เริ่มปานกลาง
                else sanityFillImage.color = Color.cyan;                   // ปกติ/สติเต็ม
            }
        }
    }
}