using UnityEngine;

public class PlayerLookDetector : MonoBehaviour
{
    [Header("🎥 Camera Reference")]
    [SerializeField] private Camera playerCamera;

    [Header("🎯 Detection Settings")]
    [SerializeField] private float maxDistance = 12f;       // ระยะทางสายตาที่มองเห็น
    [SerializeField] private LayerMask scareLayer;         // Layer ของวัตถุตกใจ
    [SerializeField] private LayerMask obstacleLayer;      // ✨ Layer ของกำแพง/สิ่งกีดขวาง (เช่น Default, Wall)

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // ✨ รวม Layer วัตถุตกใจ + Layer กำแพงสิ่งกีดขวาง เข้าด้วยกัน
        LayerMask combinedMask = scareLayer | obstacleLayer;

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, combinedMask))
        {
            // ตรวจสอบว่าสิ่งที่ Raycast ชนเป็นอันดับแรกคือ JumpscareTarget หรือไม่
            JumpscareTarget scareObject = hit.collider.GetComponent<JumpscareTarget>();

            // ถ้าโดนวัตถุตกใจก่อน (ไม่มีกำแพงบัง) ให้ทำงานทันที
            if (scareObject != null && !scareObject.HasBeenTriggered)
            {
                scareObject.TriggerJumpscare();
            }
            // ถ้าชนกำแพง/สิ่งกีดขวางก่อน สคริปต์จะไม่ทำอะไรเลย (ป้องกันการเห็นทะลุกำแพง)
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxDistance);
        }
    }
}