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
            // ตรวจสอบการกดปุ่ม E ตามระบบ Input ของโปรเจกต์
#if ENABLE_INPUT_SYSTEM
            bool pressedE = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
#else
            bool pressedE = Input.GetKeyDown(KeyCode.E);
#endif

            if (pressedE)
            {
                // 🔍 บันทึกเช็คจุดที่ 1: ปุ่ม E กดติดไหม
                Debug.Log($"[NPCEntity] -> ยืนยัน! ตรวจพบการกดปุ่ม E ที่ตัววัตถุ: {gameObject.name}");
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

    public void OnInteract()
    {
        // 🔍 บันทึกเช็คจุดที่ 2: เช็คว่าลืมลากไฟล์ Config ใส่ช่องใน Inspector หรือเปล่า
        if (npcConfiguration == null)
        {
            Debug.LogError($"<color=red><b>❌ [Error] กด E แล้วแต่คุยไม่ได้! เพราะคุณลืมลากไฟล์ NPC Config มาใส่ในช่องของวัตถุ [{gameObject.name}] ในหน้า Inspector ครับ</b></color>");
            return;
        }

        // 🔍 บันทึกเช็คจุดที่ 3: เช็คว่าในฉากมีวัตถุ NPCManager เปิดทำงานอยู่ไหม
        if (NPCManager.Instance == null)
        {
            Debug.LogError("<color=red><b>❌ [Error] ไม่พบ NPCManager ในฉาก! ตรวจดูว่าได้สร้าง GameObject ชื่อ NPCManager และแปะสคริปต์ไว้ในฉากเปิดเกมแล้วหรือยัง</b></color>");
            return;
        }

        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;

        // ประมวลผลและดึงบทพูด
        string dialogueResult = NPCManager.Instance.InteractWithNPC(npcConfiguration, currentDay);

        // 📢 บันทึกเช็คจุดที่ 4: พ่นบทพูดออกทาง Console ตัวจริง
        Debug.Log($"<color=yellow>💬 <b>[{npcConfiguration.npcID}] พูดว่า:</b> {dialogueResult}</color>");
    }
}