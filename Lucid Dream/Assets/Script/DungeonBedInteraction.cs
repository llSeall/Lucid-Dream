using UnityEngine;

public class DungeonBedInteraction : MonoBehaviour
{
    [Header("🎯 Interaction Config")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 3f;

    [Header("📺 UI Prompt")]
    [Tooltip("ลาก Text UI แจ้งเตือน เช่น 'กด E เพื่อเข้านอน (ผ่านด่าน)' มาใส่ตรงนี้")]
    [SerializeField] private GameObject interactionPromptUI;

    private bool isPlayerInRange = false;
    private Transform playerTransform;

    private void Start()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;
            if (interactionPromptUI != null) interactionPromptUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance)
            {
                TriggerClearLevel();
            }
        }
    }

    private void TriggerClearLevel()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        Debug.Log("<color=lime>🎉 [DungeonBed] ผู้เล่นกดนอนท้ายด่านฝันร้าย! ผ่านด่านกลางคืนแล้ว กำลังเตรียมตัวตื่นตอนเช้า...</color>");

        // 🌅 สั่งเปลี่ยนสถานะเกมกลับสู่ตอนเช้า (Daytime)
        // ✨ [แก้ไขล็อกบั๊ก] ตัดระบบบวกวันแมนนวลออก เนื่องจากคำสั่ง ChangeState ด้านล่างนี้
        // จะวิ่งเข้าไปเรียกฟังก์ชัน TimeManager.Instance.StartNewDay() ซึ่งทำหน้าที่บวกวัน, 
        // ปรับเวลาเป็น 8 โมงเช้า, อัปเดตบทสนทนา NPC และเซฟเกมให้คุณโดยอัตโนมัติอยู่แล้วครับ!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Daytime);
        }
        else
        {
            Debug.LogError("🚨 ไม่พบ GameManager ในฉาก! ไม่สามารถเปลี่ยนสถานะกลับเป็นตอนเช้าได้");
        }
    }
}