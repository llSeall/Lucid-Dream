using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LockDoorTrigger : MonoBehaviour
{
    [Header("🎯 Target Door")]
    [Tooltip("ลาก GameObject ประตูที่ต้องการให้ล็อกมาใส่")]
    [SerializeField] private LockedHingeDoor targetDoor;

    [Header("⚙️ Door Options")]
    [Tooltip("ติ๊กถูก: ดึงประตูกลับมาปิดสนิทก่อนล็อก / เอาออก: ล็อกประตูกลางคันในตำแหน่งที่มันเปิดอยู่")]
    [SerializeField] private bool closeDoorBeforeLock = true;

    [Tooltip("ติ๊กถูก: ลบกุญแจสำหรับประตูบานนี้ออกจากตัวผู้เล่นด้วยเมื่อประตูถูกล็อก ✨")]
    [SerializeField] private bool removeRequiredKey = true;

    [Header("🔊 Sound Settings")]
    [Tooltip("เสียงประตูถูกล็อก/เสียงสลักกระแทก (ถ้าใส่ไว้ จะไปเล่นที่ตำแหน่ง AudioSource 3D ของตัวประตู)")]
    [SerializeField] private AudioClip doorLockSoundClip;

    [Header("🔒 State Settings")]
    [Tooltip("ทำงานแค่ครั้งเดียวหรือไม่")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered = false;

    private void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggered) return;

            if (targetDoor != null)
            {
                hasTriggered = true;

                // 1. ปิด Collider ของจุดเหยียบเพื่อกันการเหยียบซ้ำ
                if (triggerOnce)
                {
                    Collider col = GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                }

                // 2. สั่งเล่นเสียงล็อก 3D ที่ประตู
                if (doorLockSoundClip != null)
                {
                    targetDoor.PlayOneShotSound(doorLockSoundClip);
                }

                // 3. สั่งล็อกประตู
                targetDoor.LockDoor(closeDoorBeforeLock);

                // 4. ✨ ริบกุญแจสำหรับประตูนี้ออกจากกระเป๋าผู้เล่น (ลบเฉพาะ targetDoor.requiredKeyID)
                if (removeRequiredKey && PlayerKeyHolder.Instance != null && !string.IsNullOrEmpty(targetDoor.requiredKeyID))
                {
                    PlayerKeyHolder.Instance.RemoveKey(targetDoor.requiredKeyID);
                }

                Debug.Log($"[Lock Door Trigger] ผู้เล่นเหยียบทริกเกอร์: ล็อกประตู '{targetDoor.name}' เรียบร้อยแล้ว");
            }
            else
            {
                Debug.LogWarning("[Lock Door Trigger] ยังไม่ได้ใส่ Target Door ใน Inspector!");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (targetDoor != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetDoor.transform.position);
            Gizmos.DrawWireSphere(targetDoor.transform.position, 0.4f);
        }
    }
}