using UnityEngine;
using UnityEngine.InputSystem;

public class InteractiveDoor : MonoBehaviour
{
    public enum RotationAxis { X, Y, Z }
    public enum FacingAxis { LocalX, LocalY, LocalZ }

    [Header("Door Settings")]
    [Tooltip("เลือกแกนที่ต้องการให้ประตูหมุน")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Z;

    [Tooltip("เลือกแกนที่เป็นด้านหน้าบานประตู (แกนที่พุ่งออกจากหน้าบานประตู)")]
    [SerializeField] private FacingAxis doorFacingAxis = FacingAxis.LocalZ;

    [Tooltip("มุมที่ประตูจะเปิด (องศา)")]
    [SerializeField] private float openAngle = 90f;

    [Tooltip("ความเร็วในการหมุนเปิด-ปิด")]
    [SerializeField] private float doorSpeed = 5f;

    [Tooltip("ติ๊กถูกช่องนี้หากทดสอบแล้วประตูยังเปิดเข้ามาหาตัวผู้เล่น (ใช้สำหรับกลับทิศทางผลักออก)")]
    [SerializeField] private bool invertDirection = false;

    [Header("Interaction Settings")]
    [Tooltip("Tag ของตัวละครผู้เล่น")]
    [SerializeField] private string playerTag = "Player";

    private bool isOpen = false;
    private bool isPlayerInZone = false;
    private Transform playerTransform;

    private Quaternion defaultRotation;
    private Quaternion targetRotation;

    void Awake()
    {
        defaultRotation = transform.localRotation;
        targetRotation = defaultRotation;
    }

    void Update()
    {
        bool eKeyPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            eKeyPressed = true;
#else
        if (Input.GetKeyDown(KeyCode.E))
            eKeyPressed = true;
#endif

        if (isPlayerInZone && eKeyPressed)
        {
            ToggleDoor();
        }

        // ค่อยๆ หมุนประตูไปยังเป้าหมาย
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRotation, Time.deltaTime * doorSpeed);
    }

    private void ToggleDoor()
    {
        isOpen = !isOpen;

        if (isOpen)
        {
            if (playerTransform != null)
            {
                // ✨ แปลงตำแหน่ง World Space ของผู้เล่นให้อยู่ใน Local Space ของประตู
                Vector3 localPlayerPos = transform.InverseTransformPoint(playerTransform.position);

                // ดึงค่าตำแหน่งผู้เล่นตามแกนหน้าบานประตูที่เลือก
                float playerSide = 0f;
                switch (doorFacingAxis)
                {
                    case FacingAxis.LocalX: playerSide = localPlayerPos.x; break;
                    case FacingAxis.LocalY: playerSide = localPlayerPos.y; break;
                    case FacingAxis.LocalZ: playerSide = localPlayerPos.z; break;
                }

                // ถ้าผู้เล่นอยู่ฝั่งบวก (playerSide >= 0) ให้หมุนไปฝั่งลบ (-openAngle) เพื่อผลักออกจากตัว
                // ถ้าผู้เล่นอยู่ฝั่งลบ (playerSide < 0) ให้หมุนไปฝั่งบวก (+openAngle)
                float targetAngle = (playerSide >= 0f) ? -openAngle : openAngle;

                // หากสลับทิศทางไว้ ให้คูณ -1
                if (invertDirection)
                {
                    targetAngle *= -1f;
                }

                // กำหนดมุมหมุนตามแกนหมุนที่เลือกไว้
                Vector3 eulerAngle = Vector3.zero;
                switch (rotationAxis)
                {
                    case RotationAxis.X: eulerAngle = new Vector3(targetAngle, 0f, 0f); break;
                    case RotationAxis.Y: eulerAngle = new Vector3(0f, targetAngle, 0f); break;
                    case RotationAxis.Z: eulerAngle = new Vector3(0f, 0f, targetAngle); break;
                }

                targetRotation = defaultRotation * Quaternion.Euler(eulerAngle);
            }
        }
        else
        {
            // ปิดประตูหมุนกลับมุมเดิม
            targetRotation = defaultRotation;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = true;
            playerTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInZone = false;
            playerTransform = null;
        }
    }
}