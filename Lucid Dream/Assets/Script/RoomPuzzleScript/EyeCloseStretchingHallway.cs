using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EyeCloseStretchingHallway : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;        // ตัวละครผู้เล่น
    [SerializeField] private Transform playerCamera;           // กล้องผู้เล่น (ใช้เช็กทิศทางการมอง)
    [SerializeField] private Transform hallwayEnd;            // ประตู/กำแพงปลายทางเดิน
    [SerializeField] private Transform originalPoint;         // จุดตั้งต้นปกติของประตู
    [SerializeField] private Transform stretchedPoint;        // จุดสูงสุดที่ประตูยืดออกไป
    [SerializeField] private Transform treadmillLimitPoint;   // จุดลิมิตกั้นไม่ให้ผู้เล่นเดินเลย (จุดเดินอยู่กับที่)
    [SerializeField] private CanvasGroup eyeOverlayCanvasGroup; // CanvasGroup สีดำของระบบหลับตา

    [Header("Wall UV Scrollers ✨")]
    [Tooltip("ลากกำแพงสองข้างทางที่มีสคริปต์ WallUVScroller มาใส่ในนี้")]
    [SerializeField] private List<WallUVScroller> wallScrollers = new List<WallUVScroller>();

    [Header("Speed Settings")]
    [Tooltip("ความเร็วในการยืดประตูออกไปตอนแรก")]
    [SerializeField] private float stretchSpeed = 8f;
    [Tooltip("ความเร็วในการย่นประตูคืนกลับมาตอนหลับตา")]
    [SerializeField] private float retractSpeed = 3f;

    [Header("Status")]
    [SerializeField] private bool isCurseActive = false; // คำสาปเริ่มทำงานหรือยัง
    [SerializeField] private bool isCurseBroken = false; // ปลดล็อกคำสาปแล้วหรือยัง

    private bool isStretching = false;

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        if (playerTransform == null && playerCamera != null)
            playerTransform = playerCamera.root;

        // ซ่อน Gizmos ตัวช่วยตั้งค่า
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Update()
    {
        if (isCurseBroken || !isCurseActive) return;

        HandleHallwayMovement();
        HandleTreadmill();
    }

    private void OnTriggerEnter(Collider other)
    {
        // เมื่อผู้เล่นเดินก้าวเข้ามาในปากทางเดิน เริ่มทำงานคำสาป
        if (!isCurseBroken && (other.CompareTag("Player") || other.transform.root == playerTransform))
        {
            isCurseActive = true;
            isStretching = true;
        }
    }

    private void HandleHallwayMovement()
    {
        // 1. สถานะยืดออกไปครั้งแรก
        if (isStretching)
        {
            hallwayEnd.position = Vector3.MoveTowards(hallwayEnd.position, stretchedPoint.position, stretchSpeed * Time.deltaTime);

            // เมื่อยืดไปถึงจุดสูงสุดแล้ว หยุดยืด
            if (Vector3.Distance(hallwayEnd.position, stretchedPoint.position) < 0.01f)
            {
                isStretching = false;
            }
        }

        // 2. เช็กว่าผู้เล่นกำลังหลับตา และหันหน้าไปทางประตูหรือไม่
        bool isClosingEyes = eyeOverlayCanvasGroup != null && eyeOverlayCanvasGroup.alpha > 0.3f;

        Vector3 dirToDoor = (hallwayEnd.position - playerCamera.position).normalized;
        float angle = Vector3.Angle(playerCamera.forward, dirToDoor);
        bool isLookingAtDoor = angle < 75f; // มองไปทางประตูในมุมไม่เกิน 75 องศา

        // 3. ถ้าหลับตา + หันมองประตู -> ดึงประตูย่นกลับมาที่เดิม
        if (isClosingEyes && isLookingAtDoor)
        {
            hallwayEnd.position = Vector3.MoveTowards(hallwayEnd.position, originalPoint.position, retractSpeed * Time.deltaTime);

            // เมื่อย่นกลับมาถึงจุดเดิมแล้ว = ปลดล็อกคำสาปสำเร็จ
            if (Vector3.Distance(hallwayEnd.position, originalPoint.position) < 0.05f)
            {
                BreakCurse();
            }
        }
    }

    private void HandleTreadmill()
    {
        // ล็อกไม่ให้ผู้เล่นเดินทะลุจุด treadmillLimitPoint ไปข้างหน้า
        if (treadmillLimitPoint == null || playerTransform == null) return;

        Vector3 hallwayForward = (stretchedPoint.position - originalPoint.position).normalized;
        hallwayForward.y = 0f;

        Vector3 playerOffset = playerTransform.position - treadmillLimitPoint.position;
        playerOffset.y = 0f;

        // เช็กว่าผู้เล่นก้าวเลยจุดลิมิตไปทางประตูหรือไม่
        float dot = Vector3.Dot(playerOffset, hallwayForward);
        if (dot > 0f)
        {
            // ดึงตัวผู้เล่นกลับมาให้อยู่ที่จุดลิมิตเสมอ (เดินอยู่กับที่)
            playerTransform.position -= hallwayForward * dot;

            // ✨ สั่งให้กำแพงเลื่อน UV เมื่อผู้เล่นติดจุด Treadmill และพยายามกดเดินหน้า
            float moveInput = Input.GetAxis("Vertical");
            if (moveInput > 0.1f)
            {
                foreach (WallUVScroller scroller in wallScrollers)
                {
                    if (scroller != null)
                    {
                        scroller.Scroll(moveInput);
                    }
                }
            }
        }
    }

    public void BreakCurse()
    {
        isCurseBroken = true;
        isCurseActive = false;
        hallwayEnd.position = originalPoint.position; // คืนค่าตำแหน่งประตูเป๊ะๆ
        Debug.Log("ปลดล็อกคำสาปสำเร็จ! ทางเดินกลับเป็นปกติแล้ว");
    }

    private void OnDrawGizmos()
    {
        if (originalPoint != null && stretchedPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(originalPoint.position, stretchedPoint.position);

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(originalPoint.position, 0.5f);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(stretchedPoint.position, 0.5f);
        }

        if (treadmillLimitPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(treadmillLimitPoint.position, new Vector3(2f, 2f, 0.1f));
        }
    }
}