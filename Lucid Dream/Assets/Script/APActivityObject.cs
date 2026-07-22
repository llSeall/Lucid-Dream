using UnityEngine;

public class APActivityObject : MonoBehaviour
{
    [Header("🎯 Activity Settings")]
    public string activityName = "นั่งสมาธิ / กินกาแฟ";
    public int apCost = 1;              // แต้ม AP ที่ต้องจ่าย
    public float sanityRestore = 25f;   // ค่าสติที่จะได้คืน

    [Header("🛏️ Special Mode: Bed Config")]
    [Tooltip("ถ้าติ๊กถูก วัตถุนี้จะทำงานเป็นเตียงนอน (กดแล้วย้ายไปความฝันทันที ไม่ต้องหัก AP)")]
    public bool isBed = false;

    [Header("📺 UI Prompt & Interaction")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";
    [Tooltip(" UI แจ้งเตือนในฉาก เช่น ข้อความ 'กด E เพื่อทำกิจกรรม'")]
    [SerializeField] private GameObject interactionPromptUI;

    private bool isPlayerInRange = false;

    private void Start()
    {
        // ปิด UI Prompt ไว้ก่อนในตอนเริ่มต้น
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
    }

    private void Update()
    {
        // เช็คการกดปุ่ม E เมื่ออยู่ในระยะ
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    /// <summary>
    /// ฟังก์ชันทำงานเมื่อผู้เล่น Interact กับสิ่งของ
    /// </summary>
    public void Interact()
    {
        if (GameManager.Instance == null || TimeManager.Instance == null || PlayerStats.Instance == null)
        {
            Debug.LogError("🚨 [APActivityObject] ไม่พบ GameManager / TimeManager / PlayerStats ในระบบ!");
            return;
        }

        // 🛌 1. กรณีเป็นเตียงนอน
        if (isBed)
        {
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
            Debug.Log($"<color=purple>💤 [Bed] เข้านอนที่ '{activityName}' กำลังวาร์ปไปโลกความฝัน...</color>");

            // สั่ง GameManager สลับมิติและโหลด Scene กลางคืนทันที
            GameManager.Instance.ChangeState(GameState.Nighttime);
            return;
        }

        // ☕ 2. กรณีเป็นกิจกรรมทั่วไป (กินกาแฟ, อ่านหนังสือ ฯลฯ)
        bool success = TimeManager.Instance.UseAP(apCost);

        if (success)
        {
            // เพิ่มค่าสติให้ผู้เล่น
            PlayerStats.Instance.ModifySanity(sanityRestore);
            Debug.Log($"<color=cyan>✨ [Activity] ทำกิจกรรม '{activityName}' สำเร็จ! (ใช้ {apCost} AP / ฟื้นสติ +{sanityRestore})</color>");

            // 🌙 เช็คว่า AP เหลือ 0 หรือยัง? ถ้าหมดแล้ว ให้ย้ายScene ไปความฝันทันที
            if (TimeManager.Instance.currentAP <= 0)
            {
                if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
                Debug.Log("<color=purple>🌙 [AP Empty] AP หมดเกลื่อนแล้ว! กำลังตัดภาพเข้าสู่โลกความฝัน...</color>");

                GameManager.Instance.ChangeState(GameState.Nighttime);
            }
        }
        else
        {
            Debug.LogWarning($"<color=yellow>⚠️ [Activity] ทำกิจกรรม '{activityName}' ไม่สำเร็จ! AP ไม่พอ</color>");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = true;
            if (interactionPromptUI != null) interactionPromptUI.SetActive(true);

            if (isBed)
            {
                Debug.Log($"<color=green>💬 [Interact] เข้าระยะเตียง '{activityName}' -> กด [{interactKey}] เพื่อเข้านอน</color>");
            }
            else
            {
                Debug.Log($"<color=green>💬 [Interact] เข้าระยะ '{activityName}' -> กด [{interactKey}] เพื่อใช้งาน (ใช้ {apCost} AP)</color>");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = false;
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
            Debug.Log($"<color=orange>👋 [Interact] ออกจากระยะ '{activityName}'</color>");
        }
    }
}