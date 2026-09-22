using System.Collections.Generic;
using System.Linq;
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
    [Tooltip("ลาก Text UI ส่วนหัวของหน้าแชต (ชื่อผู้ส่ง) มาใส่ตรงนี้")]
    [SerializeField] private TextMeshProUGUI chatHeaderTitleText;
    [SerializeField] private TextMeshProUGUI chatContentText;

    [Tooltip("ลาก ScrollRect ของหน้าต่างแชตฝั่งขวามาใส่ เพื่อสั่งให้ Scroll Bar กลับไปบนสุดเมื่อสลับแชต")]
    [SerializeField] private ScrollRect chatScrollRect;

    [Header("🖼️ Visual Sprites & Colors")]
    [Tooltip("รูปสไปรท์กรอบแชตเมื่อกำลังเลือกช่องนั้นอยู่")]
    [SerializeField] private Sprite activeChatSprite;
    [Tooltip("รูปสไปรท์กรอบแชตเมื่อไม่ได้เลือก")]
    [SerializeField] private Sprite waitingChatSprite;
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color waitingTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private struct ContactData
    {
        public string senderID;
        public string senderName;
        public int latestDay;
    }

    private List<ContactData> contactList = new List<ContactData>();
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
        if (contactList.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) SelectChat(selectedIndex - 1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) SelectChat(selectedIndex + 1);
    }

    public void LoadChatRoom()
    {
        foreach (var item in spawnedContacts) Destroy(item);
        spawnedContacts.Clear();
        contactList.Clear();

        var manager = ComputerUIManager.Instance;
        if (manager == null || manager.pcData == null) return;

        manager.SyncDayWithTimeManager();

        // 1. ดึงข้อความที่มีวัน <= วันปัจจุบัน
        var availableMessages = manager.pcData.chatMessages
            .Where(m => m.dayNumber <= manager.currentDay)
            .ToList();

        // 2. จัดกลุ่มรายชื่อผู้ส่งไม่ให้ซ้ำกัน
        var groupedSenders = availableMessages.GroupBy(m => m.senderID);

        foreach (var group in groupedSenders)
        {
            var firstMsg = group.First();
            int maxDay = group.Max(m => m.dayNumber);

            contactList.Add(new ContactData
            {
                senderID = group.Key,
                senderName = firstMsg.senderName.GetLocalizedString(),
                latestDay = maxDay
            });
        }

        // 3. สร้างปุ่มรายชื่อฝั่งซ้าย
        for (int i = 0; i < contactList.Count; i++)
        {
            int index = i;
            GameObject newItem = Instantiate(contactItemPrefab, contactListContainer);
            var contact = contactList[i];

            TextMeshProUGUI nameText = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = "@" + contact.senderName;

            // ✨ เช็กและแสดงจุดแดง NotificationDot หากเป็นแชตใหม่ประจำวันที่ยังไม่ได้เปิดอ่าน
            bool isUnread = !readSenderIDs.Contains(contact.senderID);
            Transform dotTransform = newItem.transform.Find("NotificationDot");
            if (dotTransform != null)
            {
                dotTransform.gameObject.SetActive(isUnread);
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
        Debug.Log("SelectChat Called");
        if (contactList.Count == 0) return;

        selectedIndex = Mathf.Clamp(index, 0, contactList.Count - 1);
        var currentContact = contactList[selectedIndex];

        // แสดงชื่อผู้ส่ง
        if (chatHeaderTitleText != null)
        {
            chatHeaderTitleText.text = currentContact.senderName;
        }

        // ดึงประวัติข้อความทั้งหมดมาแสดง
        var manager = ComputerUIManager.Instance;
        if (manager != null && manager.pcData != null && chatContentText != null)
        {
            var chatHistory = manager.pcData.chatMessages
                .Where(m => m.senderID == currentContact.senderID && m.dayNumber <= manager.currentDay)
                .OrderBy(m => m.dayNumber)
                .Select(m => m.messageText.GetLocalizedString())
                .ToList();

            chatContentText.text = string.Join("\n\n", chatHistory);
        }

        // ✨ เมื่อเปิดอ่านแล้ว ให้ทำเครื่องหมายว่าอ่านแล้ว และปิดจุดแดงของแท็บนี้
        readSenderIDs.Add(currentContact.senderID);
        if (selectedIndex < spawnedContacts.Count)
        {
            Transform dotTransform = spawnedContacts[selectedIndex].transform.Find("NotificationDot");
            if (dotTransform != null)
            {
                dotTransform.gameObject.SetActive(false);
            }
        }

        // ✨ รีเซ็ตตำแหน่ง Scroll Bar ให้กลับไปเริ่มต้นที่ด้านบนสุดของข้อความ
        if (chatScrollRect != null)
        {
            // สั่ง Rebuild Layout ให้ Text และ Content คำนวณขนาดความสูงจริงใหม่ทันที
            if (chatContentText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(chatContentText.rectTransform);
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 1f;
        }

        // สลับ Visual Sprites ของแท็บรายชื่อ
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
                    bg.color = Color.white;
                }
            }

            if (txt != null) txt.color = isSelected ? activeTextColor : waitingTextColor;
        }
    }
}