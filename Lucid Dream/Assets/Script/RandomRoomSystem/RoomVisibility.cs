using System.Collections;
using UnityEngine;

public class RoomVisibility : MonoBehaviour
{
    private Transform playerTransform;
    private GameObject graphicsContainer;

    private float safeDistance;     // ⭕ ระยะวงกลมรอบตัวที่ "ต้องเปิดเสมอ" กันห้องที่เรายืนอยู่หาย
    private float maxViewDistance;  // 📏 ระยะมองเห็นสูงสุดข้างหน้า (มองไกลแค่ไหน)
    private float viewAngle;        // 👁️ องศากรวยสายตา (เช่น 90 หรือ 120 องศาข้างหน้า)

    private float checkInterval = 0.2f; // ⏱️ เพิ่มความถี่ในการเช็คเป็น 0.2 วินาทีเพื่อให้ตอบสนองตอนหันหน้าไวขึ้น

    public void SetupOptimization(Transform player, float safeDist, float maxDist, float angle)
    {
        playerTransform = player;
        safeDistance = safeDist;
        maxViewDistance = maxDist;
        viewAngle = angle;

        Transform graphicsTransform = transform.Find("Graphics");
        if (graphicsTransform != null) graphicsContainer = graphicsTransform.gameObject;
        else if (transform.childCount > 0) graphicsContainer = transform.GetChild(0).gameObject;

        if (graphicsContainer != null) StartCoroutine(VisibilityCheckLoop());
    }

    private IEnumerator VisibilityCheckLoop()
    {
        while (true)
        {
            if (playerTransform != null && graphicsContainer != null)
            {
                // 1. หาความห่างระหว่างผู้เล่นกับห้อง
                float distance = Vector3.Distance(playerTransform.position, transform.position);
                bool shouldBeVisible = false;

                // 2. เงื่อนไขแรก: อยู่ในระยะปลอดภัยรอบตัวไหม? (ถ้าใช่ เปิดแน่ๆ ไม่ต้องสนทิศทาง)
                if (distance <= safeDistance)
                {
                    shouldBeVisible = true;
                }
                // 3. เงื่อนไขที่สอง: ถ้าอยู่นอกระยะปลอดภัย แต่อยู่ในระยะมองเห็นสูงสุดข้างหน้า?
                else if (distance <= maxViewDistance)
                {
                    //คำนวณหาทิศทางจากตัวผู้เล่นพุ่งไปหาห้อง
                    Vector3 directionToRoom = (transform.position - playerTransform.position).normalized;

                    // ปรับค่าแนวตั้ง (Y) ให้เป็น 0 เพื่อคิดองศาแค่ในแนวราบขนานพื้น (กันบั๊กเวลาผู้เล่นก้มหรือเงยหน้า)
                    directionToRoom.y = 0;
                    Vector3 playerForward = playerTransform.forward;
                    playerForward.y = 0;
                    playerForward.Normalize();

                    // หาองศาระหว่าง "ทางที่หันหน้าไป" กับ "ทางที่ห้องตั้งอยู่"
                    float angleBetween = Vector3.Angle(playerForward, directionToRoom);

                    // ถ้าระยะองศาน้อยกว่าครึ่งหนึ่งของกรวยสายตา แปลว่า "ห้องนี้อยู่ข้างหน้าเรา" ➔ สั่งเปิด!
                    if (angleBetween <= viewAngle / 2f)
                    {
                        shouldBeVisible = true;
                    }
                }

                // สั่งเปิดหรือปิดตามผลลัพธ์คณิตศาสตร์ข้างบน
                if (graphicsContainer.activeSelf != shouldBeVisible)
                {
                    graphicsContainer.SetActive(shouldBeVisible);
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }
}