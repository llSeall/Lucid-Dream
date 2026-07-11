using UnityEngine;
using TMPro;
using System; // ✨ เพิ่มเข้ามาเพื่อใช้ Action

public class DialogueUIController : MonoBehaviour
{
    public static DialogueUIController Instance { get; private set; }

    [Header("🪟 UI Components")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;

    // ✨ Event สำหรับให้สคริปต์อื่น (เช่น สคริปต์เดินของตัวละคร) มาลงทะเบียนฟังเพื่อล็อก/ปลดล็อกการเดิน
    public static Action OnDialogueStart;
    public static Action OnDialogueEnd;

    // 💡 [จุดแก้ไขสำคัญ] ปรับให้ค่าความแอคทีฟ รวมไปถึงตอนที่หน้าต่างรับไอเทม (ItemRewardPopup) กำลังเปิดอยู่ด้วย
    // ทำให้สคริปต์ผู้เล่นที่คอยเช็คค่านี้อยู่ จะสั่งหยุดเดินทันทีเมื่อหน้าต่างไอเทมเปิดขึ้นมา!
    public bool IsDialogueActive => (dialoguePanel != null && dialoguePanel.activeSelf) ||
                                    (ItemRewardPopup.Instance != null && ItemRewardPopup.Instance.IsPopupActive);

    private bool openedThisFrame = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (dialoguePanel != null) dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        // 💡 [จุดแก้ไข] เช็คเฉพาะตอนที่ "ตัวกล่องข้อความปกติ" เปิดอยู่เท่านั้น ถึงจะกดปิดไดอะล็อกตรงนี้
        // เพื่อป้องกันไม่ให้ปุ่มกดไปแย่งหน้าที่ของกล่องรับไอเทมครับ
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
                CloseDialogue();
            }
        }
    }

    private void LateUpdate()
    {
        if (openedThisFrame) openedThisFrame = false;
    }

    public void ShowDialogue(string npcName, string text)
    {
        if (dialoguePanel == null || nameText == null || dialogueText == null) return;

        nameText.text = npcName;
        dialogueText.text = text;
        dialoguePanel.SetActive(true);

        openedThisFrame = true;

        // 🔥 แจ้งเตือนระบบว่า "เริ่มคุยแล้วนะ" (เพื่อให้สคริปต์ผู้เล่นสั่งหยุดเดิน)
        OnDialogueStart?.Invoke();
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);

        // 🔥 แจ้งเตือนระบบว่า "คุยจบแล้วนะ" (จะยิงปลดล็อกก็ต่อเมื่อไม่มีหน้าต่างไอเทมเปิดค้างอยู่เท่านั้น)
        if (ItemRewardPopup.Instance == null || !ItemRewardPopup.Instance.IsPopupActive)
        {
            OnDialogueEnd?.Invoke();
        }
    }
}