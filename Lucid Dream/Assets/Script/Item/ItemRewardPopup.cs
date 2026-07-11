using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemRewardPopup : MonoBehaviour
{
    public static ItemRewardPopup Instance { get; private set; }

    [Header("📦 UI Panel")]
    [SerializeField] private GameObject popupPanel;

    [Header("📝 Text Elements")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Header("🖼️ Visual Elements")]
    [SerializeField] private Image itemImage;

    public bool IsPopupActive { get; private set; }
    private bool openedThisFrame = false;

    // ⏳ ตัวแปรสำหรับระบบฝากคิวบทพูดปกติ
    private string savedNpcName;
    private string savedDialogueText;
    private bool hasPendingDialogue = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
    }

    private void Start()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    private void Update()
    {
        if (IsPopupActive && !openedThisFrame)
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
                ClosePopup();
            }
        }
    }

    private void LateUpdate()
    {
        if (openedThisFrame) openedThisFrame = false;
    }

    public void ShowReward(string npcName, ItemData itemData, string firstEncounterDialogue)
    {
        if (popupPanel == null || itemData == null) return;

        string translatedItemName = itemData.itemName.GetLocalizedString();

        // ตั้งค่า UI
        if (titleText != null) titleText.text = $" ได้รับไอเท็มจาก <b><color=yellow>{npcName}</color></b>";
        if (itemNameText != null) itemNameText.text = translatedItemName;
        if (dialogueText != null) dialogueText.text = firstEncounterDialogue;
        if (itemImage != null) itemImage.sprite = itemData.itemIcon;

        popupPanel.SetActive(true);
        IsPopupActive = true;
        openedThisFrame = true;

        // 🔥 เรียกใช้ Event เริ่มไดอะล็อกทันที เพื่อสั่งล็อกตัวละครแบบเดียวกับบทพูดปกติ
        DialogueUIController.OnDialogueStart?.Invoke();
    }

    public void SetPendingDialogue(string npcName, string dialogueText)
    {
        savedNpcName = npcName;
        savedDialogueText = dialogueText;
        hasPendingDialogue = true;
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
        IsPopupActive = false; // ปิดสถานะตรงนี้ก่อน เพื่อให้ IsDialogueActive ตัวแปรกลางทำงานถูกต้อง

        if (hasPendingDialogue)
        {
            hasPendingDialogue = false;
            if (DialogueUIController.Instance != null)
            {
                // ส่งต่อไปหน้าต่างบทพูดปกติ (มันจะล็อกผู้เล่นต่อเนื่องให้เอง)
                DialogueUIController.Instance.ShowDialogue(savedNpcName, savedDialogueText);
            }
        }
        else
        {
            // 🔥 ถ้าไม่มีบทพูดอะไรมาต่อคิวแล้ว ให้ยิง Event ปลดล็อกผู้เล่นให้กลับมาเดินได้ทันที
            DialogueUIController.OnDialogueEnd?.Invoke();
        }
    }
}