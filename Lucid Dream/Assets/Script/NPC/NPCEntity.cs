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
            // ถ้ากล่องข้อความเปิดอยู่ ไม่ต้องจับการกด E ของตัวละครซ้ำ (ปล่อยให้ UI คุมระบบปิดกล่อง)
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
            string npcName = npcConfiguration != null ? npcConfiguration.npcID : "ไม่ทราบชื่อ";
            Debug.Log($"[NPC] ผู้เล่นเดินเข้ามาใกล้ <b>{npcName}</b> แล้ว (กด E เพื่อคุย)");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            Debug.Log($"[NPC] ผู้เล่นเดินออกจากระยะแล้ว");
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

    // ✨ ฟังก์ชันนี้มีได้เพียงตัวเดียวในสคริปต์ ห้ามประกาศซ้ำ
    public void OnInteract()
    {
        if (npcConfiguration == null)
        {
            Debug.LogError($"❌ [Error] ลืมลากไฟล์ NPC Config ใส่ในช่องของ [{gameObject.name}] ใน Inspector ครับ");
            return;
        }

        if (NPCManager.Instance == null)
        {
            Debug.LogError("❌ [Error] ไม่พบ NPCManager ในฉาก! ตรวจดูว่าได้เปิดวัตถุคุมระบบแล้วหรือยัง");
            return;
        }

        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;

        // 1. ดึงบทพูดประจำวันที่ผ่านระบบแปลภาษา (Localization) มาแล้ว
        string dialogueResult = NPCManager.Instance.InteractWithNPC(npcConfiguration, currentDay);

        // 2. ดึงชื่อ NPC ที่ผ่านระบบแปลภาษามาจากตารางเดียวกันด้วย
        string localizedName = npcConfiguration.npcID;
        try
        {
            if (!string.IsNullOrEmpty(npcConfiguration.npcNameKey))
            {
                localizedName = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase
                    .GetLocalizedString(npcConfiguration.localizationTableName, npcConfiguration.npcNameKey);
            }
        }
        catch
        {
            localizedName = npcConfiguration.npcID; // แผนสำรองกรณีตารางภาษาไม่มีคีย์นี้
        }

        // 3. ส่งข้อมูลชื่อและบทพูดขึ้นสู่หน้าจอ UI ของจริง
        if (DialogueUIController.Instance != null)
        {
            DialogueUIController.Instance.ShowDialogue(localizedName, dialogueResult);
        }

        Debug.Log($"<color=yellow>💬 <b>[{npcConfiguration.npcID}] พูดว่า:</b> {dialogueResult}</color>");
    }
}