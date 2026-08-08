using UnityEngine;

public class Billboard25D : MonoBehaviour
{
    [Tooltip("ลาก Main Camera หรือ Transform ของผู้เล่นมาใส่ตรงนี้ (ถ้าไม่ใส่จะดึง Main Camera อัตโนมัติ)")]
    [SerializeField] private Transform targetToFace;

    private void Start()
    {
        // หากไม่ได้ใส่เป้าหมาย ระบบจะหากล้องหลักให้อัตโนมัติ
        if (targetToFace == null && Camera.main != null)
        {
            targetToFace = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (targetToFace == null) return;

        // 1. ดึงพิกัดตำแหน่งของเป้าหมาย
        Vector3 targetPosition = targetToFace.position;

        // 2. ✨ หัวใจสำคัญ: ล็อคพิกัด Y ของเป้าหมายให้เท่ากับสไปรท์
        // เพื่อตัดการคำนวณมุมก้ม-เงย (มุมบน-ล่าง) ออกไป
        targetPosition.y = transform.position.y;

        // 3. หันสไปรท์ไปทิศทางนั้น (จะหมุนเฉพาะแกน Y เท่านั้น)
        transform.LookAt(targetPosition);
    }
}