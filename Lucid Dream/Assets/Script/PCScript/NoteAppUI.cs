using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class NoteAppUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Transform noteListContainer;
    [SerializeField] private GameObject noteListItemPrefab;
    [SerializeField] private TextMeshProUGUI noteContentText;

    [Header("🎨 Visual Colors")]
    [SerializeField] private Color activeTabColor = new Color(0.8f, 0.8f, 0.8f, 1f);
    [SerializeField] private Color activeTextColor = Color.black;
    [SerializeField] private Color waitingTabColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color waitingTextColor = Color.gray;

    private List<NoteData> availableNotes = new List<NoteData>();
    private List<GameObject> spawnedItems = new List<GameObject>();
    private int selectedIndex = 0;

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        LoadNotes();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
    {
        LoadNotes();
    }

    private void Update()
    {
        if (availableNotes.Count == 0) return;

        if (Input.GetKeyDown(KeyCode.UpArrow)) SelectNote(selectedIndex - 1);
        else if (Input.GetKeyDown(KeyCode.DownArrow)) SelectNote(selectedIndex + 1);
    }

    public void LoadNotes()
    {
        foreach (var item in spawnedItems) Destroy(item);
        spawnedItems.Clear();
        availableNotes.Clear();

        var manager = ComputerUIManager.Instance;
        if (manager == null || manager.pcData == null) return;

        foreach (var note in manager.pcData.notes)
        {
            if (note.dayNumber <= manager.currentDay) availableNotes.Add(note);
        }

        for (int i = 0; i < availableNotes.Count; i++)
        {
            int index = i;
            GameObject newItem = Instantiate(noteListItemPrefab, noteListContainer);

            TextMeshProUGUI tabText = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (tabText != null)
                tabText.text = availableNotes[i].noteTitle.GetLocalizedString();

            // 🖱️ ระบบรองรับการคลิกเม้าส์สำหรับ Image Prefab
            Image bgImage = newItem.GetComponent<Image>();
            if (bgImage != null) bgImage.raycastTarget = true; // เปิดรับคลิกเม้าส์

            Button btn = newItem.GetComponent<Button>();
            if (btn == null) btn = newItem.AddComponent<Button>(); // เพิ่มคอมโพเนนต์ Button ให้อัตโนมัติ

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectNote(index));

            spawnedItems.Add(newItem);
        }

        SelectNote(selectedIndex);
    }

    public void SelectNote(int index)
    {
        if (availableNotes.Count == 0) return;

        selectedIndex = Mathf.Clamp(index, 0, availableNotes.Count - 1);
        var selectedNote = availableNotes[selectedIndex];

        if (noteContentText != null)
            noteContentText.text = selectedNote.noteContent.GetLocalizedString();

        for (int i = 0; i < spawnedItems.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            Image bg = spawnedItems[i].GetComponent<Image>();
            TextMeshProUGUI txt = spawnedItems[i].GetComponentInChildren<TextMeshProUGUI>();

            if (bg != null) bg.color = isSelected ? activeTabColor : waitingTabColor;
            if (txt != null) txt.color = isSelected ? activeTextColor : waitingTextColor;
        }
    }
}