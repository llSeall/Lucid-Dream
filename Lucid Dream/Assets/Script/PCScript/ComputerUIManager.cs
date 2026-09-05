using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

public class ComputerUIManager : MonoBehaviour
{
    public static ComputerUIManager Instance { get; private set; }

    [Header("🌐 Global Settings")]
    public int currentDay = 1;
    public PCData pcData;

    [Header("🖥️ Windows & Apps")]
    [SerializeField] private GameObject noteWindow;
    [SerializeField] private GameObject chatWindow;

    [Header("🔴 Desktop Badges")]
    [SerializeField] private GameObject chatNotificationDot;

    [Header("🔔 Toast Notification (Slide Animation)")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private RectTransform notificationRect; // ✨ ลาก RectTransform ของ NotificationPanel มาใส่
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private LocalizedString newMessageNotificationText;

    [Header("📐 Slide Settings")]
    [SerializeField] private Vector2 hiddenAnchoredPosition = new Vector2(400f, -200f); // ✨ ตำแหน่งซ่อนนอกจอ (ขวา)
    [SerializeField] private Vector2 shownAnchoredPosition = new Vector2(0f, -200f);   // ✨ ตำแหน่งแสดงบนจอ
    [SerializeField] private float slideDuration = 0.4f;                              // ความเร็วในการสไลด์ เข้า/ออก
    [SerializeField] private float toastDuration = 2.5f;                              // ระยะเวลาแช่ค้างไว้ก่อนสไลด์ออก

    private int lastShownDay = -1; // ✨ บันทึกวันที่เคยแสดงแจ้งเตือนไปแล้ว
    private Coroutine toastCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        CloseAllWindows();
        if (notificationPanel != null) notificationPanel.SetActive(false);
        UpdateChatDotStatus();
    }

    public void OpenNoteApp()
    {
        CloseAllWindows();
        if (noteWindow != null) noteWindow.SetActive(true);
    }

    public void OpenChatApp()
    {
        CloseAllWindows();
        if (chatWindow != null) chatWindow.SetActive(true);
        if (chatNotificationDot != null) chatNotificationDot.SetActive(false);
    }

    public void CloseChatApp()
    {
        if (chatWindow != null) chatWindow.SetActive(false);
    }

    public void CloseNoteApp()
    {
        if (noteWindow != null) noteWindow.SetActive(false);
    }

    public void CloseAllWindows()
    {
        if (noteWindow != null) noteWindow.SetActive(false);
        if (chatWindow != null) chatWindow.SetActive(false);
    }

    public void AdvanceToNextDay()
    {
        currentDay++;
        UpdateChatDotStatus();
    }

    // อัปเดตจุดสีแดงบนไอคอนแชต
    public void UpdateChatDotStatus()
    {
        bool hasNewMessage = pcData != null && pcData.chatMessages.Exists(m => m.dayNumber == currentDay);
        if (chatNotificationDot != null) chatNotificationDot.SetActive(hasNewMessage);
    }

    // ✨ ฟังก์ชันที่จะถูกเรียกเมื่อผู้เล่นกดเปิดคอมพิวเตอร์สำเร็จ
    public void TryShowDailyNotification()
    {
        // 🛑 ถ้าวันนี้เคยโชว์แจ้งเตือนไปแล้ว จะไม่โชว์ซ้ำอีก
        if (currentDay == lastShownDay) return;

        bool hasNewMessage = pcData != null && pcData.chatMessages.Exists(m => m.dayNumber == currentDay);

        if (hasNewMessage)
        {
            lastShownDay = currentDay; // บันทึกว่าวันนี้แสดงผลแล้ว

            string msg = (newMessageNotificationText != null && !newMessageNotificationText.IsEmpty)
                ? newMessageNotificationText.GetLocalizedString()
                : "New Message";

            ShowNotification(msg);
        }
    }

    public void ShowNotification(string message)
    {
        if (toastCoroutine != null) StopCoroutine(toastCoroutine);
        toastCoroutine = StartCoroutine(SlideToastRoutine(message));
    }

    // 🎬 Coroutine สำหรับอนิเมชัน สไลด์เข้า -> แช่ค้าง -> สไลด์ออก
    private IEnumerator SlideToastRoutine(string msg)
    {
        if (notificationPanel == null || notificationRect == null) yield break;

        if (notificationText != null) notificationText.text = msg;

        // ตั้งค่าตำแหน่งเริ่มต้นนอกจอก่อนเปิด Object
        notificationRect.anchoredPosition = hiddenAnchoredPosition;
        notificationPanel.SetActive(true);

        // 1. ⏩ สไลด์เข้า (Hidden -> Shown)
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            notificationRect.anchoredPosition = Vector2.Lerp(hiddenAnchoredPosition, shownAnchoredPosition, t);
            yield return null;
        }
        notificationRect.anchoredPosition = shownAnchoredPosition;

        // 2. ⏳ แช่ค้างไว้
        yield return new WaitForSeconds(toastDuration);

        // 3. ⏪ สไลด์ออก (Shown -> Hidden)
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            notificationRect.anchoredPosition = Vector2.Lerp(shownAnchoredPosition, hiddenAnchoredPosition, t);
            yield return null;
        }
        notificationRect.anchoredPosition = hiddenAnchoredPosition;

        notificationPanel.SetActive(false);
    }
}