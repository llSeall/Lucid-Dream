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

        // 2. ล็อคพิกัด Y ของเป้าหมายให้เท่ากับสไปรท์เพื่อตัดมุมก้ม-เงย
        targetPosition.y = transform.position.y;

        // 3. หันสไปรท์ไปทิศทางเป้าหมาย
        transform.LookAt(targetPosition);

        // 4. ✨ เพิ่มบรรทัดนี้: หมุนแกน Y กลับ 180 องศาเพื่อแก้ UI กลับด้านซ้าย-ขวา
        transform.Rotate(0, 180f, 0);
    }
}