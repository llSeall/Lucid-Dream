using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MoveObjectOnTrigger : MonoBehaviour
{
    [Header("References")]
    [Tooltip("วัตถุหรือสิ่งของที่ต้องการให้เลื่อน")]
    [SerializeField] private Transform objectToMove;
    [Tooltip("จุดปลายทางที่ต้องการให้สิ่งของเลื่อนไปหยุด")]
    [SerializeField] private Transform targetPosition;

    [Header("Settings")]
    [Tooltip("ความเร็วในการเลื่อนสิ่งของ")]
    [SerializeField] private float moveSpeed = 5f;
    [Tooltip("ทำงานแค่ครั้งเดียวหรือไม่ (หากไม่ติ๊ก ผู้เล่นเดินชนซ้ำแล้วจะทำงานใหม่ได้)")]
    [SerializeField] private bool triggerOnce = true;

    private bool isMoving = false;
    private bool hasTriggered = false;

    void Start()
    {
        // ตั้งค่า Collider บนวัตถุนี้ให้เป็น Trigger อัตโนมัติ
        GetComponent<BoxCollider>().isTrigger = true;
    }

    void Update()
    {
        if (isMoving && objectToMove != null && targetPosition != null)
        {
            // เลื่อนวัตถุเข้าหาจุดเป้าหมายด้วยความเร็วคงที่
            objectToMove.position = Vector3.MoveTowards(
                objectToMove.position,
                targetPosition.position,
                moveSpeed * Time.deltaTime
            );

            // เมื่อเลื่อนไปถึงจุดเป้าหมายแล้ว ให้หยุด
            if (Vector3.Distance(objectToMove.position, targetPosition.position) < 0.001f)
            {
                isMoving = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // ตรวจจับว่าวัตถุที่เดินมาชนคือ Player หรือไม่
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggered) return;

            isMoving = true;
            hasTriggered = true;
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วาดเส้นช่วยเล็งจุดเริ่มต้นและจุดปลายทางในหน้าต่าง Scene View
        if (objectToMove != null && targetPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(objectToMove.position, targetPosition.position);
            Gizmos.DrawWireSphere(targetPosition.position, 0.3f);
        }
    }
}