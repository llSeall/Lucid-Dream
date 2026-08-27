using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
public class LockedHingeDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public string requiredKeyID = "MainKey";
    public bool isLocked = true;
    public float pushForce = 8f;

    [Header("UI Settings ✨")]
    [Tooltip("UI รูปแม่กุญแจล็อก (แสดงตอนที่ผู้เล่นยังไม่มีกุญแจ)")]
    public GameObject lockedIconUI;
    [Tooltip("UI รูปปุ่ม E (แสดงตอนมีกุญแจพร้อมปลดล็อก)")]
    public GameObject interactIconUI;

    private Rigidbody rb;
    private HingeJoint hinge;
    private bool isPlayerNearby = false;
    private bool isOpen = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();

        rb.isKinematic = isLocked;

        HideAllUI();
    }

    void Update()
    {
        if (!isPlayerNearby) return;

        UpdateUI();

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (isLocked)
            {
                if (PlayerKeyHolder.Instance != null && PlayerKeyHolder.Instance.HasKey(requiredKeyID))
                {
                    UnlockDoor();
                }
            }
            else
            {
                ToggleDoor();
            }
        }
    }

    void UpdateUI()
    {
        // หากประตูปลดล็อกแล้ว ให้ปิด UI ทั้งหมดทันที ไม่ต้องโชว์ปุ่ม E อีก ✨
        if (!isLocked)
        {
            HideAllUI();
            return;
        }

        bool hasKey = (PlayerKeyHolder.Instance != null && PlayerKeyHolder.Instance.HasKey(requiredKeyID));

        if (hasKey)
        {
            // มีกุญแจแล้ว -> ซ่อนรูปแม่กุญแจ แสดงรูปปุ่ม E เพื่อกดปลดล็อก
            if (lockedIconUI != null) lockedIconUI.SetActive(false);
            if (interactIconUI != null) interactIconUI.SetActive(true);
        }
        else
        {
            // ยังไม่มีกุญแจ -> แสดงรูปแม่กุญแจ ซ่อนรูปปุ่ม E
            if (lockedIconUI != null) lockedIconUI.SetActive(true);
            if (interactIconUI != null) interactIconUI.SetActive(false);
        }
    }

    void UnlockDoor()
    {
        isLocked = false;
        rb.isKinematic = false;
        HideAllUI(); // ซ่อน UI ทันทีเมื่อปลดล็อกสำเร็จ ✨
        ToggleDoor();
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;
        Vector3 pushDirection = isOpen ? transform.forward : -transform.forward;
        rb.AddForce(pushDirection * pushForce, ForceMode.Impulse);
    }

    void HideAllUI()
    {
        if (lockedIconUI != null) lockedIconUI.SetActive(false);
        if (interactIconUI != null) interactIconUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            UpdateUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            HideAllUI();
        }
    }
}