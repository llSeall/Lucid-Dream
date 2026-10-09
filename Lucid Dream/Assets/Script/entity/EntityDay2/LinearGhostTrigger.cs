using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LinearGhostTrigger : MonoBehaviour
{
    [Header("🎯 Target Ghost")]
    [Tooltip("ลาก GameObject ผีตัวใหญ่มาใส่")]
    [SerializeField] private LinearChaserGhost targetGhost;

    [Header("🏷️ Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private bool hasTriggered = false;

    private void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;

        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            hasTriggered = true;

            if (targetGhost != null)
            {
                targetGhost.ActivateGhost();
            }
            else
            {
                Debug.LogWarning("⚠️ [LinearGhostTrigger] ยังไม่ได้ใส่ Target Ghost ใน Inspector!");
            }

            // ปิดการทำงานของ Collider ทริกเกอร์ทันที เพื่อไม่ให้ทำงานซ้ำอีก
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }
}