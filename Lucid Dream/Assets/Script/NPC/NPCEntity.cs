using UnityEngine;

public class NPCEntity : MonoBehaviour
{
    [Header("📄 NPC Data Settings")]
    [SerializeField] private NPCConfig npcConfiguration;

    private bool isPlayerInRange = false;

    private void OnEnable()
    {
        TimeManager.OnDayChangedSafe += UpdateNPCRegistry;
        UpdateNPCRegistry();
    }

    private void OnDisable()
    {
        TimeManager.OnDayChangedSafe -= UpdateNPCRegistry;
    }

    private void Update()
    {
        if (isPlayerInRange)
        {
            if (DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive)
                return;

#if ENABLE_INPUT_SYSTEM            
            bool pressedE = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
#else
            bool pressedE = Input.GetKeyDown(KeyCode.E);
#endif

            if (pressedE)
            {
                OnInteract();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    public void UpdateNPCRegistry()
    {
        if (npcConfiguration == null) return;
        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;

        if (npcConfiguration.activeDays.Contains(currentDay))
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
            isPlayerInRange = false;
        }
    }

    public void OnInteract()
    {
        if (npcConfiguration == null || NPCManager.Instance == null) return;

        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;

        // 1. ประมวลผลบทพูดประจำวัน (และแจกไอเทมหลังบ้านผ่านลอจิกภายใน)
        string dialogueResult = NPCManager.Instance.InteractWithNPC(npcConfiguration, currentDay);

        // 2. แปลชื่อ NPC
        string localizedName = npcConfiguration.npcID;
        try
        {
            if (!string.IsNullOrEmpty(npcConfiguration.npcNameKey))
            {
                localizedName = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase
                    .GetLocalizedString(npcConfiguration.localizationTableName, npcConfiguration.npcNameKey);
            }
        }
        catch { localizedName = npcConfiguration.npcID; }

        // ✨ [ลอจิกจัดคิวใหม่] เช็คสถานะการเปิดป๊อปอัปไอเทม
        if (ItemRewardPopup.Instance != null && ItemRewardPopup.Instance.IsPopupActive)
        {
            // 🎁 ถ้าระบบเปิดกล่องรางวัลขึ้นมาแล้ว ให้ส่งบทพูดปกติไป "ฝากคิวต่อท้ายไว้ก่อน" อย่าเพิ่งขึ้นจอ!
            ItemRewardPopup.Instance.SetPendingDialogue(localizedName, dialogueResult);
            Debug.Log($"[NPCEntity] มีการแจกไอเทม! ส่งบทพูดของ {localizedName} เข้าไปต่อคิวใน ItemRewardPopup แล้ว");
        }
        else if (DialogueUIController.Instance != null)
        {
            // 💬 ถ้าไม่มีการแจกของในรอบนี้ ให้เปิดกล่องสนทนาปกติของ NPC ทันทีตามปกติ
            DialogueUIController.Instance.ShowDialogue(localizedName, dialogueResult);
        }
    }
}