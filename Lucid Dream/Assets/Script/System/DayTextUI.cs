using UnityEngine;
using TMPro;

public class DayTextUI : MonoBehaviour
{
    [Header("📺 UI References")]
    [SerializeField] private TextMeshProUGUI dayText;
    [SerializeField] private string prefixFormat = "วันที่ ";

    private void OnEnable()
    {
        // ลงทะเบียน Event เมื่อมีการเปลี่ยนวันหรือซิงค์ข้อมูลเซฟ
        TimeManager.OnDayChangedSafe += UpdateDayUI;
    }

    private void OnDisable()
    {
        // ยกเลิกการลงทะเบียนเพื่อป้องกัน Memory Leak
        TimeManager.OnDayChangedSafe -= UpdateDayUI;
    }

    private void Start()
    {
        // อัปเดตข้อความทันทีเมื่อเริ่มเข้าซีน
        UpdateDayUI();
    }

    public void UpdateDayUI()
    {
        if (dayText == null)
            dayText = GetComponent<TextMeshProUGUI>();

        if (dayText != null && TimeManager.Instance != null)
        {
            dayText.text = $"{prefixFormat}{TimeManager.Instance.currentDay}";
        }
    }
}