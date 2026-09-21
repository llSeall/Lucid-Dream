using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class ChatAppUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform contactListContainer;
    [SerializeField] private GameObject contactItemPrefab;

    [Header("💬 Chat Content Display")]
    [Tooltip("ลาก Text UI ส่วนหัวของหน้าแชท (เช่น ชื่อเพื่อน/ชื่อคนส่ง) มาใส่ตรงนี้")]
    [SerializeField] private TextMeshProUGUI chatHeaderTitleText;
    [SerializeField] private TextMeshProUGUI chatContentText;

    [Header("🖼️ Visual Sprites & Colors")]
    [Tooltip("รูปสไปรท์กรอบแชตเมื่อกำลังเลือกช่องนั้นอยู่")]
    [SerializeField] private Sprite activeChatSprite;
    [Tooltip("รูปสไปรท์กรอบแชตเมื่อไม่ได้เลือก")]
    [SerializeField] private Sprite waitingChatSprite;
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color waitingTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private List<ChatMessageData> activeChats = new List<ChatMessageData>();
    private List<GameObject> spawnedContacts = new List<GameObject>();
    private HashSet<string> readSenderIDs = new HashSet<string>();
    private int selectedIndex = 0;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        LoadChatRoom();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
    {
        LoadChatRoom();
    }

    private void Update()
    {
        if (activeChats.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) SelectChat(selectedIndex - 1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) SelectChat(selectedIndex + 1);
    }

    public void LoadChatRoom()
    {
        foreach (var item in spawnedContacts) Destroy(item);
        spawnedContacts.Clear();
        activeChats.Clear();

        var manager = ComputerUIManager.Instance;
        if (manager == null || manager.pcData == null) return;

        manager.SyncDayWithTimeManager();

        foreach (var chat in manager.pcData.chatMessages)
        {
            if (chat.dayNumber <= manager.currentDay)
            {
                int existingIndex = activeChats.FindIndex(c => c.senderID == chat.senderID);
                if (existingIndex >= 0)
                {
                    if (chat.dayNumber > activeChats[existingIndex].dayNumber)
                        activeChats[existingIndex] = chat;
                }
                else
                {
                    activeChats.Add(chat);
                }
            }
        }

        for (int i = 0; i < activeChats.Count; i++)
        {
            int index = i;
            GameObject newItem = Instantiate(contactItemPrefab, contactListContainer);
            var chatData = activeChats[i];

            TextMeshProUGUI nameText = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = "@" + chatData.senderName.GetLocalizedString();

            // เช็กจุดแจ้งเตือนประจำวันในช่องแชท
            bool isNewTodayMessage = (chatData.dayNumber == manager.currentDay) && !readSenderIDs.Contains(chatData.senderID);
            Transform dotTransform = newItem.transform.Find("NotificationDot");
            if (dotTransform != null)
            {
                dotTransform.gameObject.SetActive(isNewTodayMessage);
            }

            Image bgImage = newItem.GetComponent<Image>();
            if (bgImage != null) bgImage.raycastTarget = true;

            Button btn = newItem.GetComponent<Button>();
            if (btn == null) btn = newItem.AddComponent<Button>();

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectChat(index));

            spawnedContacts.Add(newItem);
        }

        SelectChat(selectedIndex);
    }

    public void SelectChat(int index)
    {
        if (activeChats.Count == 0) return;

        selectedIndex = Mathf.Clamp(index, 0, activeChats.Count - 1);
        var currentChat = activeChats[selectedIndex];

        // แสดงชื่อผู้ส่งที่หัวข้อบนสุด
        if (chatHeaderTitleText != null)
        {
            chatHeaderTitleText.text = currentChat.senderName.GetLocalizedString();
        }

        // แสดงข้อความแชท
        if (chatContentText != null)
        {
            chatContentText.text = currentChat.messageText.GetLocalizedString();
        }

        // อ่านแล้วปิดจุดแจ้งเตือน
        readSenderIDs.Add(currentChat.senderID);
        if (selectedIndex < spawnedContacts.Count)
        {
            Transform dotTransform = spawnedContacts[selectedIndex].transform.Find("NotificationDot");
            if (dotTransform != null)
            {
                dotTransform.gameObject.SetActive(false);
            }
        }

        // ✨ สลับ Sprite ของกรอบแชต (Active vs Waiting)
        for (int i = 0; i < spawnedContacts.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            Image bg = spawnedContacts[i].GetComponent<Image>();
            TextMeshProUGUI txt = spawnedContacts[i].GetComponentInChildren<TextMeshProUGUI>();

            if (bg != null)
            {
                Sprite targetSprite = isSelected ? activeChatSprite : waitingChatSprite;
                if (targetSprite != null)
                {
                    bg.sprite = targetSprite;
                    bg.color = Color.white; // รีเซ็ตสีเพื่อแสดงสีจริงของรูปสไปรท์
                }
            }

            if (txt != null) txt.color = isSelected ? activeTextColor : waitingTextColor;
        }
    }
}