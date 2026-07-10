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

        Debug.Log($"[ItemRewardPopup] 📺 สั่งเปิดหน้าต่างแสดงรางวัลไอเทมชิ้น: {itemData.itemID} สำเร็จ!");

        string translatedItemName = itemData.itemName.GetLocalizedString();

        if (titleText != null) titleText.text = $"🎁 ได้รับไอเท็มจาก <b><color=yellow>{npcName}</color></b>";
        if (itemNameText != null) itemNameText.text = translatedItemName;
        if (dialogueText != null) dialogueText.text = firstEncounterDialogue;
        if (itemImage != null) itemImage.sprite = itemData.itemIcon;

        popupPanel.SetActive(true);
        IsPopupActive = true;
        openedThisFrame = true;

        // ล็อกตัวละครไว้ห้ามขยับ
        DialogueUIController.OnDialogueStart?.Invoke();
    }

    // ✨ ฟังก์ชันใหม่: ใช้สำหรับฝากบทพูดรายวันให้มาต่อคิวไว้
    public void SetPendingDialogue(string npcName, string dialogueText)
    {
        savedNpcName = npcName;
        savedDialogueText = dialogueText;
        hasPendingDialogue = true;
    }

    public void ClosePopup()
    {
        popupPanel.SetActive(false);
        IsPopupActive = false;

        // 🔥 [จุดสำคัญ] ถ้ามีบทพูดปกติฝากคิวไว้ ให้เปิดมันขึ้นมาทันทีหลังจากกล่องของรางวัลปิดลง!
        if (hasPendingDialogue)
        {
            hasPendingDialogue = false; // รีเซ็ตสถานะคิว
            if (DialogueUIController.Instance != null)
            {
                DialogueUIController.Instance.ShowDialogue(savedNpcName, savedDialogueText);
                // ไม่ต้องสั่ง OnDialogueEnd เพราะกล่องสนทนาปกติจะมารับช่วงคุมการล็อกตัวละครต่อให้เองครับ
            }
        }
        else
        {
            // ถ้าไม่มีอะไรค้างคาในคิวแล้ว ถึงปลดล็อกตัวละครให้เดินได้ตามปกติ
            DialogueUIController.OnDialogueEnd?.Invoke();
        }
    }
}