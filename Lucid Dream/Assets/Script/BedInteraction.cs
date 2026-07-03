using UnityEngine;
using UnityEngine.UI; // ลบออกได้หากใช้ TextMeshPro

public class BedInteraction : MonoBehaviour
{
    [Header("🎯 Interaction Config")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 3f;

    [Header("📺 UI Prompt")]
    [Tooltip("ลาก Text UI แจ้งเตือน เช่น 'กด E เพื่อเข้านอน' มาใส่ตรงนี้")]
    [SerializeField] private GameObject interactionPromptUI;

    private bool isPlayerInRange = false;
    private Transform playerTransform;

    private void Start()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ตรวจสอบว่าผู้เล่นเดินเข้ามาในระยะเตียง (สมมติว่าผู้เล่นติดแท็ก "Player")
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
            // เช็คระยะห่างจริงอีกรอบเพื่อความปลอดภัย
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance)
            {
                TriggerSleep();
            }
        }
    }

    private void TriggerSleep()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        Debug.Log("<color=purple>💤 ผู้เล่นเข้านอนแล้ว กำลังเดินทางเข้าสู่โลกความฝัน...</color>");

        // เรียกผ่าน GameManager เพื่อเปลี่ยนสถานะเป็นกลางคืน ย้ายซีนอัตโนมัติ และเซฟเกมลงไทม์ไลน์
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.Nighttime);
        }
        else
        {
            Debug.LogError("🚨 ไม่พบ GameManager ในฉาก! กรุณาตรวจสอบว่ามี Prefab Manager อยู่ในระบบ");
        }
    }
}