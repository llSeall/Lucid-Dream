using System.Collections;
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
    // ✨ เพิ่มตัวแปรสำหรับลาก ScrollRect ของหน้าต่าง Note มาใส่
    [SerializeField] private ScrollRect noteScrollRect;
    [Header("⌨️ Typewriter Effect & Sound")]
    [Tooltip("ความเร็วในการพิมพ์ตัวอักษรต่อตัว (วินาที)")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private AudioSource audioSource;
    [Tooltip("ใส่ไฟล์เสียงต๊อกแต๊กพิมพ์ดีด (สามารถใส่หลายๆ เสียงเพื่อสุ่มได้)")]
    [SerializeField] private AudioClip[] typingSounds;

    [Header("🖼️ Visual Sprites & Colors")]
    [Tooltip("รูปสไปรท์แท็บโน๊ตเมื่อกำลังเลือก")]
    [SerializeField] private Sprite activeTabSprite;
    [Tooltip("รูปสไปรท์แท็บโน๊ตเมื่อไม่ได้เลือก")]
    [SerializeField] private Sprite waitingTabSprite;
    [SerializeField] private Color activeTextColor = Color.black;
    [SerializeField] private Color waitingTextColor = Color.gray;

    private List<NoteData> availableNotes = new List<NoteData>();
    private List<GameObject> spawnedItems = new List<GameObject>();
    private int selectedIndex = 0;
    private Coroutine typewriterCoroutine;

    // ✨ จดจำวันของโน๊ตที่เคยพิมพ์พิมพ์ดีดไปแล้ว เพื่อไม่ให้เล่นซ้ำ
    private HashSet<int> typedNoteDays = new HashSet<int>();

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        if (ComputerUIManager.Instance != null)
        {
            ComputerUIManager.Instance.hasReadTodayNote = true;
        }

        LoadNotes();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
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

        manager.SyncDayWithTimeManager();

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
            {
                tabText.text = availableNotes[i].noteTitle.GetLocalizedString();
            }

            Image bgImage = newItem.GetComponent<Image>();
            if (bgImage != null) bgImage.raycastTarget = true;

            Button btn = newItem.GetComponent<Button>();
            if (btn == null) btn = newItem.AddComponent<Button>();

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

        // ✨ สลับ Sprite ของแท็บโน๊ต (Active vs Waiting)
        for (int i = 0; i < spawnedItems.Count; i++)
        {
            bool isSelected = (i == selectedIndex);
            Image bg = spawnedItems[i].GetComponent<Image>();
            TextMeshProUGUI txt = spawnedItems[i].GetComponentInChildren<TextMeshProUGUI>();

            if (bg != null)
            {
                Sprite targetSprite = isSelected ? activeTabSprite : waitingTabSprite;
                if (targetSprite != null)
                {
                    bg.sprite = targetSprite;
                    bg.color = Color.white;
                }
            }

            if (txt != null) txt.color = isSelected ? activeTextColor : waitingTextColor;
        }

        if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);

        string localizedContent = selectedNote.noteContent.GetLocalizedString();

        // ✨ เช็กว่าโน๊ตประจำวันนั้นเคยเล่นอนิเมชันพิมพ์ไปหรือยัง
        if (typedNoteDays.Contains(selectedNote.dayNumber))
        {
            if (noteContentText != null)
            {
                noteContentText.text = localizedContent;
            }

            // ✨ สั่ง Rebuild Layout และรีเซ็ต Scrollbar ไปบนสุด
            ResetScrollPosition();
        }
        else
        {
            typedNoteDays.Add(selectedNote.dayNumber);
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(localizedContent));
        }
    }

   
    private IEnumerator TypewriterRoutine(string textToType)
    {
        if (noteContentText == null) yield break;

        noteContentText.text = "";

        // ✨ บังคับ Rebuild Layout ตั้งแต่เริ่มเพื่อให้ Content ขยายขนาดรับข้อความเต็มตั้งแต่วินาทีแรก (ป้องกัน Scroll เด้ง)
        if (noteContentText != null)
        {
            noteContentText.text = textToType;
            LayoutRebuilder.ForceRebuildLayoutImmediate(noteContentText.rectTransform);
            if (noteScrollRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(noteScrollRect.content);
            noteContentText.text = ""; // เคลียร์ข้อความเพื่อเริ่มพิมพ์ดีด
        }

        foreach (char letter in textToType)
        {
            noteContentText.text += letter;

            if (audioSource != null && typingSounds != null && typingSounds.Length > 0 && letter != ' ' && letter != '\n')
            {
                AudioClip clip = typingSounds[Random.Range(0, typingSounds.Length)];
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clip);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        // ✨ เมื่อพิมพ์เสร็จสมบูรณ์ คำนวณขนาดจริงอีกครั้ง
        ResetScrollPosition();
    }

    public void ResetScrollPosition()
    {
        if (noteScrollRect != null)
        {
            if (noteContentText != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(noteContentText.rectTransform);
            }
            if (noteScrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(noteScrollRect.content);
            }
            Canvas.ForceUpdateCanvases();
            // ปรับตำแหน่งไปบนสุด
            noteScrollRect.verticalNormalizedPosition = 1f;
        }
    }
}