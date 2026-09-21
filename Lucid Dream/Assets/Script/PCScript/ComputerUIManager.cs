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

    [Header("🔊 Audio FX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip notificationSound; // ✨ เสียงเอฟเฟกต์เวลามีข้อความเข้า

    [Header("🖥️ Windows & Apps")]
    [SerializeField] private GameObject noteWindow;
    [SerializeField] private GameObject chatWindow;

    [Header("🔴 Desktop Badges")]
    [SerializeField] private GameObject chatNotificationDot;

    [Header("🔔 Toast Notification (Slide Animation)")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private RectTransform notificationRect;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private LocalizedString newMessageNotificationText;

    [Header("📐 Slide Settings")]
    [SerializeField] private Vector2 hiddenAnchoredPosition = new Vector2(400f, -200f);
    [SerializeField] private Vector2 shownAnchoredPosition = new Vector2(0f, -200f);
    [SerializeField] private float slideDuration = 0.4f;
    [SerializeField] private float toastDuration = 2.5f;

    [Header("📋 Quest Conditions")]
    public bool hasReadTodayNote = false; // ✨ เช็กว่าผู้เล่นอ่านโน๊ตของวันนั้นหรือยัง

    private int lastShownDay = -1;
    private Coroutine toastCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        SyncDayWithTimeManager();
        CloseAllWindows();
        if (notificationPanel != null) notificationPanel.SetActive(false);
        UpdateChatDotStatus();
    }

    /// <summary>
    /// ซิงค์วันกับ TimeManager อัตโนมัติ
    /// </summary>
    public void SyncDayWithTimeManager()
    {
        if (TimeManager.Instance != null)
        {
            if (currentDay != TimeManager.Instance.currentDay)
            {
                currentDay = TimeManager.Instance.currentDay;
                hasReadTodayNote = false; // ✨ รีเซ็ตสถานะการอ่านโน๊ตเมื่อขึ้นวันใหม่
            }
        }
    }

    public void OpenNoteApp()
    {
        CloseAllWindows();
        if (noteWindow != null) noteWindow.SetActive(true);

        // ✨ ปลดล็อก: เมื่อผู้เล่นกดเปิดแอปโน๊ต ถือว่าได้อ่านโน๊ตประจำวันเรียบร้อยแล้ว
        hasReadTodayNote = true;
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
        hasReadTodayNote = false;
        UpdateChatDotStatus();
    }

    public void UpdateChatDotStatus()
    {
        SyncDayWithTimeManager();
        bool hasNewMessage = pcData != null && pcData.chatMessages.Exists(m => m.dayNumber == currentDay);
        if (chatNotificationDot != null) chatNotificationDot.SetActive(hasNewMessage);
    }

    public void TryShowDailyNotification()
    {
        SyncDayWithTimeManager();

        if (currentDay == lastShownDay) return;

        bool hasNewMessage = pcData != null && pcData.chatMessages.Exists(m => m.dayNumber == currentDay);

        if (hasNewMessage)
        {
            lastShownDay = currentDay;

            string msg = (newMessageNotificationText != null && !newMessageNotificationText.IsEmpty)
                ? newMessageNotificationText.GetLocalizedString()
                : "New Message";

            ShowNotification(msg);
        }
    }

    public void ShowNotification(string message)
    {
        if (toastCoroutine != null) StopCoroutine(toastCoroutine);

        // 🔊 เล่นเสียงเอฟเฟกต์แจ้งเตือน
        if (audioSource != null && notificationSound != null)
        {
            audioSource.PlayOneShot(notificationSound);
        }

        toastCoroutine = StartCoroutine(SlideToastRoutine(message));
    }

    private IEnumerator SlideToastRoutine(string msg)
    {
        if (notificationPanel == null || notificationRect == null) yield break;

        if (notificationText != null) notificationText.text = msg;

        notificationRect.anchoredPosition = hiddenAnchoredPosition;
        notificationPanel.SetActive(true);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / slideDuration;
            notificationRect.anchoredPosition = Vector2.Lerp(hiddenAnchoredPosition, shownAnchoredPosition, t);
            yield return null;
        }
        notificationRect.anchoredPosition = shownAnchoredPosition;

        yield return new WaitForSeconds(toastDuration);

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