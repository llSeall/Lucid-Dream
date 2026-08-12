using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomVisibility : MonoBehaviour
{
    private Transform playerTransform;
    private GameObject graphicsContainer;

    private Renderer[] roomRenderers;
    private bool currentVisibilityState = true;

    private float safeDistance;     // ⭕ ระยะวงกลมรอบตัวที่ "ต้องเปิดเสมอ"
    private float maxViewDistance;  // 📏 ระยะมองเห็นสูงสุดข้างหน้า
    private float viewAngle;        // 👁️ องศากรวยสายตา

    private float checkInterval = 0.2f;

    // ✨ เพิ่มเวลาดีเลย์ก่อนเริ่มซ่อนภาพ (หน่วงเวลาให้ NavMesh และ AI โหลดแมพครบ 100% ก่อน)
    [Header("⏱️ Initialization Settings")]
    [Tooltip("ระยะเวลารอ (วินาที) ให้แมพโหลดและอบ NavMesh ครบก่อนเริ่มระบบซ่อนภาพ")]
    [SerializeField] private float initialDelay = 2.0f;

    public void SetupOptimization(Transform player, float safeDist, float maxDist, float angle, float delay = 2.0f)
    {
        playerTransform = player;
        safeDistance = safeDist;
        maxViewDistance = maxDist;
        viewAngle = angle;
        initialDelay = delay;

        Transform graphicsTransform = transform.Find("Graphics");
        if (graphicsTransform != null) graphicsContainer = graphicsTransform.gameObject;
        else if (transform.childCount > 0) graphicsContainer = transform.GetChild(0).gameObject;

        if (graphicsContainer != null)
        {
            roomRenderers = graphicsContainer.GetComponentsInChildren<Renderer>(true);
            StartCoroutine(VisibilityCheckLoop());
        }
    }

    private IEnumerator VisibilityCheckLoop()
    {
        // ⏳ 1. รอให้เวลาผ่านไปตาม initialDelay เพื่อให้ NavMesh และ GhostAI โหลดแมพทั้งหมดเสร็จสมบูรณ์
        yield return new WaitForSeconds(initialDelay);

        // 🔄 2. เมื่อครบกำหนดเวลาดีเลย์แล้ว ค่อยเริ่มลูปเช็คกรวยสายตาและสั่งซ่อนภาพ
        while (true)
        {
            if (playerTransform != null && roomRenderers != null && roomRenderers.Length > 0)
            {
                float distance = Vector3.Distance(playerTransform.position, transform.position);
                bool shouldBeVisible = false;

                // เช็คระยะปลอดภัยรอบตัว
                if (distance <= safeDistance)
                {
                    shouldBeVisible = true;
                }
                // เช็คระยะสายตาและองศากรวยการมองเห็น
                else if (distance <= maxViewDistance)
                {
                    Vector3 directionToRoom = (transform.position - playerTransform.position).normalized;
                    directionToRoom.y = 0;

                    Vector3 playerForward = playerTransform.forward;
                    playerForward.y = 0;
                    playerForward.Normalize();

                    float angleBetween = Vector3.Angle(playerForward, directionToRoom);

                    if (angleBetween <= viewAngle / 2f)
                    {
                        shouldBeVisible = true;
                    }
                }

                // สลับการแสดงผลเฉพาะ Renderer
                if (currentVisibilityState != shouldBeVisible)
                {
                    currentVisibilityState = shouldBeVisible;
                    SetRenderersEnabled(shouldBeVisible);
                }
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void SetRenderersEnabled(bool isEnabled)
    {
        for (int i = 0; i < roomRenderers.Length; i++)
        {
            if (roomRenderers[i] != null)
            {
                roomRenderers[i].enabled = isEnabled;
            }
        }
    }
}