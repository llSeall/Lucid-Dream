using UnityEngine;
using UnityEngine.Events;

public class EntityTriggerItem : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f; // ระยะห่างที่สามารถกด E ได้
    public Transform playerTransform;

    [Header("Spawn Settings")]
    [Range(0f, 100f)]
    [Tooltip("โอกาสที่จะเกิด Entity เมื่อเก็บของชิ้นนี้ (เช่น 100% สำหรับของชิ้นสำคัญ หรือ 60% ตามดีไซน์)")]
    public float spawnChance = 100f;
    public EntitySpawner entitySpawner;

    [Header("External Integration Events")]
    [Tooltip("ลากฟังก์ชันจากสคริปต์อื่นมาใส่ตรงนี้ได้ เช่น เพิ่มของเข้า Inventory หรือเล่นเสียงเก็บของ")]
    public UnityEvent onInteractEvent;

    private bool isInteractable = true;

    void Start()
    {
        // ค้นหา EntitySpawner อัตโนมัติหากไม่ได้ลากใส่ Inspector
        if (entitySpawner == null)
        {
            entitySpawner = FindObjectOfType<EntitySpawner>();
        }

        // ดึง Transform ผู้เล่นจาก EntitySpawner หากไม่ได้ระบุไว้
        if (playerTransform == null && entitySpawner != null)
        {
            playerTransform = entitySpawner.player;
        }
    }

    void Update()
    {
        if (!isInteractable || playerTransform == null) return;

        // เช็กระยะห่างระหว่างผู้เล่นกับไอเท็ม
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= interactDistance)
        {
            // เมื่อผู้เล่นอยู่ในระยะแล้วกด E
            if (Input.GetKeyDown(interactKey))
            {
                Interact();
            }
        }
    }

    /// <summary>
    /// ฟังก์ชันนี้เปิด public ไว้ เผื่อคุณมีสคริปต์ Raycast Interaction กลาง จะได้เรียก Interact() โดยตรงได้เลย
    /// </summary>
    public void Interact()
    {
        if (!isInteractable) return;
        isInteractable = false;

        Debug.Log($"[Event Item] Interacted with item: {gameObject.name}");

        // 1. เรียกสั่งงาน Event ของสคริปต์ระบบอื่น (ถ้ามี)
        onInteractEvent?.Invoke();

        // 2. คำนวณโอกาสสปอว์น Entity
        float randomRoll = Random.Range(0f, 100f);
        if (randomRoll <= spawnChance)
        {
            if (entitySpawner != null)
            {
                entitySpawner.SpawnEntity();
            }
            else
            {
                Debug.LogWarning("[Event Item] EntitySpawner is missing in Scene!");
            }
        }

        // 3. ซ่อนไอเท็มชิ้นนี้ออกจากฉาก (หรือใช้ Destroy(gameObject) ก็ได้)
        gameObject.SetActive(false);
    }

    // วาดวงกลมรัศมีระยะกดในหน้า Scene View
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}