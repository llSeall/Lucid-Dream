using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
public class PurePhysicsDoor : MonoBehaviour
{
    [Header("Door Physics Settings")]
    [Tooltip("มุมเปิดกว้างสุดของประตู (องศา)")]
    [SerializeField] private float maxOpenAngle = 90f;
    [Tooltip("น้ำหนักประตู (ยิ่งเยอะยิ่งต้องใช้แรงเดินชนดันมาก)")]
    [SerializeField] private float doorWeight = 10f;
    [Tooltip("แรงต้านการหมุน (ช่วยให้ประตูไม่หมุนเคว้ง หรือเด้งกลับไปกลับมา)")]
    [SerializeField] private float doorDrag = 5f;

    private Rigidbody rb;
    private HingeJoint hinge;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();

        // 1. ล็อกตำแหน่งประตูป้องกันลอย/หลุดแกน และเพิ่มความนุ่มนวลในการเคลื่อนที่
        rb.mass = doorWeight;
        rb.angularDamping = doorDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        // 2. กำหนดขอบเขตการเปิด-ปิดประตู
        hinge.useLimits = true;
        JointLimits limits = hinge.limits;
        limits.min = 0f;
        limits.max = maxOpenAngle;
        limits.bounciness = 0f; // ปิดแรงเด้งขอบประตู กันอาการสั่น
        limits.bounceMinVelocity = 0f;
        hinge.limits = limits;

        // 3. ปิดการใช้งานมอเตอร์โดยสิ้นเชิง
        hinge.useMotor = false;
    }
}