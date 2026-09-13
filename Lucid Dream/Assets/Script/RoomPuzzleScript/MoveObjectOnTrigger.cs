using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class MoveObjectOnTrigger : MonoBehaviour
{
    [System.Serializable]
    public struct MovePair
    {
        [Tooltip("ตั้งชื่อให้ไม่งงใน Inspector (เช่น ประตูซ้าย, ตู้หนังสือ)")]
        public string pairName;
        [Tooltip("วัตถุหรือสิ่งของที่ต้องการให้เลื่อน")]
        public Transform objectToMove;
        [Tooltip("จุดปลายทางที่ต้องการให้สิ่งของเลื่อนไปหยุด")]
        public Transform targetPosition;
    }

    [Header("References ✨")]
    [Tooltip("ใส่รายการคู่วัตถุและจุดเป้าหมายที่ต้องการให้เลื่อนพร้อมกัน")]
    [SerializeField] private List<MovePair> movePairs = new List<MovePair>();

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
        if (!isMoving) return;

        bool allReached = true;

        foreach (var pair in movePairs)
        {
            if (pair.objectToMove == null || pair.targetPosition == null) continue;

            // เลื่อนวัตถุแต่ละชิ้นเข้าหาจุดเป้าหมาย
            pair.objectToMove.position = Vector3.MoveTowards(
                pair.objectToMove.position,
                pair.targetPosition.position,
                moveSpeed * Time.deltaTime
            );

            // เช็กว่ายังมีวัตถุชิ้นไหนยังไปไม่ถึงจุดเป้าหมายหรือไม่
            if (Vector3.Distance(pair.objectToMove.position, pair.targetPosition.position) >= 0.001f)
            {
                allReached = false;
            }
        }

        // เมื่อวัตถุทุกชิ้นเลื่อนไปถึงจุดเป้าหมายแล้ว ให้หยุด
        if (allReached)
        {
            isMoving = false;
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
        // วาดเส้นช่วยเล็งจุดเริ่มต้นและจุดปลายทางทุกคู่ในหน้าต่าง Scene View
        Gizmos.color = Color.yellow;
        foreach (var pair in movePairs)
        {
            if (pair.objectToMove != null && pair.targetPosition != null)
            {
                Gizmos.DrawLine(pair.objectToMove.position, pair.targetPosition.position);
                Gizmos.DrawWireSphere(pair.targetPosition.position, 0.3f);
            }
        }
    }
}