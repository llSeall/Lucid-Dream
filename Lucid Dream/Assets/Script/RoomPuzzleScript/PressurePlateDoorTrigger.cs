using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PressurePlateDoorTrigger : MonoBehaviour
{
    [Header("🎯 Target Door")]
    [Tooltip("ลาก GameObject ประตูที่มีสคริปต์ LockedHingeDoor มาใส่")]
    [SerializeField] private LockedHingeDoor targetDoor;

    [Header("🔊 Optional Sound Settings")]
    [Tooltip("ไฟล์เสียงคลิกปลดล็อก (ถ้าลากใส่ เสียงนี้จะไปเล่นที่ AudioSource ของประตู 3D)")]
    [SerializeField] private AudioClip doorClickSoundClip;

    [Header("🔒 State Settings")]
    [Tooltip("ทำงานแค่ครั้งเดียวหรือไม่")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered = false;

    private void Start()
    {
        // ตั้งค่า Collider บนวัตถุนี้ให้เป็น Trigger อัตโนมัติ
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

                // 1. ปิด Collider ของจุดเหยียบ เพื่อไม่ให้เหยียบซ้ำ (และไม่ทำลาย GameObject)
                if (triggerOnce)
                {
                    Collider col = GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                }

                // 2. ถ้ามีไฟล์เสียงคลิกเฉพาะ ให้เล่นเสียงที่ AudioSource ของตัวประตู
                if (doorClickSoundClip != null)
                {
                    targetDoor.PlayOneShotSound(doorClickSoundClip);
                }

                // 3. สั่งปลดล็อกประตู (ปลด isKinematic ทำให้เดินดันประตูเปิดได้ทันที)
                targetDoor.UnlockDoor();

                Debug.Log($"[Pressure Plate] ผู้เล่นเหยียบทริกเกอร์ ปลดล็อกประตู '{targetDoor.name}' เรียบร้อยแล้ว");
            }
            else
            {
                Debug.LogWarning("[Pressure Plate] ยังไม่ได้ใส่ Target Door ใน Inspector!");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วาดเส้นสีเขียวเชื่อมจากจุดเหยียบไปหาประตูใน Scene View
        if (targetDoor != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetDoor.transform.position);
            Gizmos.DrawWireSphere(targetDoor.transform.position, 0.4f);
        }
    }
}