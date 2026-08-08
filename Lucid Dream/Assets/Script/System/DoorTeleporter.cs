using UnityEngine;

public class DoorTeleporter : MonoBehaviour
{
    [Header("🎯 Interaction Config")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 3f;

    [Header("🚪 Teleport Settings")]
    [Tooltip("ลาก Empty GameObject จุดที่ต้องการให้ผู้เล่นวาร์ปไปโผล่มาใส่ตรงนี้")]
    [SerializeField] private Transform targetSpawnPoint;

    [Header("📺 UI Prompt & Outline")]
    [Tooltip("ลาก Text UI แจ้งเตือน เช่น 'กด E เพื่อเข้าประตู' มาใส่ตรงนี้")]
    [SerializeField] private GameObject interactionPromptUI;

    [Tooltip("ลากคอมโพเนนต์ Outline ที่ติดอยู่กับประตูมาใส่ตรงนี้")]
    [SerializeField] private Outline targetOutline;

    private bool isPlayerInRange = false;
    private Transform playerTransform;

    private void Start()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        if (targetOutline != null) targetOutline.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าผู้เล่นเดินเข้ามาในระยะประตู (สมมติว่าผู้เล่นติดแท็ก "Player")
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;
            if (interactionPromptUI != null) interactionPromptUI.SetActive(true);
            if (targetOutline != null) targetOutline.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
            if (targetOutline != null) targetOutline.enabled = false;
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            // เช็คระยะห่างจริงอีกรอบเพื่อความปลอดภัย
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance)
            {
                TeleportPlayer();
            }
        }
    }

    private void TeleportPlayer()
    {
        if (targetSpawnPoint == null)
        {
            Debug.LogError($"🚨 [{gameObject.name}] ยังไม่ได้ใส่ targetSpawnPoint ใน Inspector!");
            return;
        }

        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        if (targetOutline != null) targetOutline.enabled = false;

        // หากผู้เล่นใช้ CharacterController ต้องสั่งปิดชั่วคราวก่อนย้ายพิกัดเพื่อป้องกัน Bug ตำแหน่งไม่เปลี่ยน
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // ย้ายตำแหน่งและทิศทางการหันหน้าของผู้เล่นไปยังจุดปลายทาง
        playerTransform.position = targetSpawnPoint.position;
        playerTransform.rotation = targetSpawnPoint.rotation;

        if (cc != null) cc.enabled = true;

        Debug.Log($"🚪 <color=green>ผู้เล่นผ่านประตูไปยัง {targetSpawnPoint.name} เรียบร้อยแล้ว</color>");
    }
}