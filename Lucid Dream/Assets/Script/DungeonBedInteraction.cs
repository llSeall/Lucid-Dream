using UnityEngine;

public class DungeonBedInteraction : MonoBehaviour
{
    [Header("🎯 Interaction Config")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 3f;

    [Header("📺 UI Prompt")]
    [Tooltip("ลาก Text UI แจ้งเตือน เช่น 'กด E เพื่อตื่นนอน (ออกจากฝัน)' มาใส่ตรงนี้")]
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
        Debug.Log("<color=lime>🎉 [DungeonBed] ออกจากฝันร้ายสำเร็จ! กำลังตื่นนอนเข้าสู่ช่วงเช้า...</color>");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnDreamCleared();
        }
        else if (TimeManager.Instance != null)
        {
            TimeManager.Instance.ExitDreamToDaytime();
        }
        else
        {
            Debug.LogError("🚨 ไม่พบ GameManager หรือ TimeManager ในฉาก!");
        }
    }
}