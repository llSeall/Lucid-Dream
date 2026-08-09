using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic; // ✨ เพิ่มใช้งาน List

public class DialogueUIController : MonoBehaviour
{
    public static DialogueUIController Instance { get; private set; }

    [Header("🪟 UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    public static Action OnDialogueStart;
    public static Action OnDialogueEnd;

    public bool IsDialogueActive => (dialoguePanel != null && dialoguePanel.activeSelf) ||
                                    (ItemRewardPopup.Instance != null && ItemRewardPopup.Instance.IsPopupActive);

    private bool openedThisFrame = false;

    // ✨ เพิ่มคิวจัดการบทพูดทีละบั้บเบิ้ล
    private List<string> currentDialogueLines = new List<string>();
    private int currentLineIndex = 0;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (dialoguePanel != null && dialoguePanel.activeSelf && !openedThisFrame)
        {
#if ENABLE_INPUT_SYSTEM            
            bool closePressed = UnityEngine.InputSystem.Keyboard.current != null && 
                               (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame || 
                                UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame);
#else
            bool closePressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space);
#endif

            if (closePressed)
            {
                AdvanceDialogue(); // ✨ กดเพื่อเลื่อนไปประโยคถัดไป
            }
        }
    }

    private void LateUpdate()
    {
        if (openedThisFrame) openedThisFrame = false;
    }

    // ✨ รองรับการส่งบทพูดหลายๆ ประโยคเข้ามา (List<string>)
    public void ShowDialogue(string npcName, List<string> lines)
    {
        if (dialoguePanel == null || nameText == null || dialogueText == null) return;
        if (lines == null || lines.Count == 0) return;

        currentDialogueLines = lines;
        currentLineIndex = 0;

        nameText.text = npcName;
        dialogueText.text = currentDialogueLines[currentLineIndex];
        dialoguePanel.SetActive(true);

        openedThisFrame = true;

        OnDialogueStart?.Invoke();
    }

    // ✨ Overload รองรับข้อความเดี่ยวแบบเดิม
    public void ShowDialogue(string npcName, string text)
    {
        ShowDialogue(npcName, new List<string> { text });
    }

    // ✨ ฟังก์ชันกดข้ามบทพูดทีละประโยค
    public void AdvanceDialogue()
    {
        currentLineIndex++;

        // หากยังมีบทพูดถัดไปในคิว ให้เปลี่ยนข้อความ
        if (currentLineIndex < currentDialogueLines.Count)
        {
            dialogueText.text = currentDialogueLines[currentLineIndex];
        }
        else
        {
            CloseDialogue(); // ถ้าครบหมดแล้วให้ปิด UI
        }
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        currentDialogueLines.Clear();
        currentLineIndex = 0;

        if (ItemRewardPopup.Instance == null || !ItemRewardPopup.Instance.IsPopupActive)
        {
            OnDialogueEnd?.Invoke();
        }
    }
}