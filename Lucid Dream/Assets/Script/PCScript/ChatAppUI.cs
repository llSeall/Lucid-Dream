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
    [SerializeField] private TextMeshProUGUI chatContentText;

    [Header("🎨 Visual Colors")]
    [SerializeField] private Color activeChatColor = new Color(0f, 0.45f, 0.85f, 1f);
    [SerializeField] private Color activeTextColor = Color.white;
    [SerializeField] private Color waitingChatColor = new Color(0.15f, 0.15f, 0.2f, 1f);
    [SerializeField] private Color waitingTextColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    private List<ChatMessageData> activeChats = new List<ChatMessageData>();
    private List<GameObject> spawnedContacts = new List<GameObject>();
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

            TextMeshProUGUI nameText = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = "@" + activeChats[i].senderName.GetLocalizedString();

            // 🖱️ ระบบรองรับการคลิกเม้าส์สำหรับ Image Prefab
            Image bgImage = newItem.GetComponent<Image>();
            if (bgImage != null) bgImage.raycastTarget = true; // เปิดรับคลิกเม้าส์

            Button btn = newItem.GetComponent<Button>();
            if (btn == null) btn = newItem.AddComponent<Button>(); // เพิ่มคอมโพเนนต์ Button ให้อัตโนมัติ

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

        if (chatContentText != null)
            chatContentText.text = currentChat.messageText.GetLocalizedString();

        for (int i = 0; i < spawnedContacts.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            Image bg = spawnedContacts[i].GetComponent<Image>();
            TextMeshProUGUI txt = spawnedContacts[i].GetComponentInChildren<TextMeshProUGUI>();

            if (bg != null) bg.color = isSelected ? activeChatColor : waitingChatColor;
            if (txt != null) txt.color = isSelected ? activeTextColor : waitingTextColor;
        }
    }
}