using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class GhostDespawnTrigger : MonoBehaviour
{
    [Header("🎯 Target Ghost Settings")]
    [Tooltip("ลาก GameObject ของผีที่ต้องการให้หายไปมาใส่ (สามารถใส่ได้หลายตัวพร้อมกัน)")]
    [SerializeField] private GameObject[] targetGhosts;

    [Header("🏷️ Trigger Settings")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("ทำงานแค่ครั้งเดียวหรือไม่")]
    [SerializeField] private bool triggerOnce = true;

    [Header("🔊 Sound Settings (Optional)")]
    [Tooltip("เสียงประกอบตอนผีหายไป (เช่น เสียงถอนหายใจ, เสียงสวบสวาบ หรือเสียงจางหายไป)")]
    [SerializeField] private AudioClip despawnSoundClip;
    [SerializeField] private AudioSource audioSource;

    private bool hasTriggered = false;

    private void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            hasTriggered = true;

            // 1. เล่นเสียงจางหายไป (ถ้ามี)
            if (audioSource != null && despawnSoundClip != null)
            {
                audioSource.PlayOneShot(despawnSoundClip);
            }

            // 2. คืนค่าระบบสเตมิน่าลดตามปกติ และล้างเอฟเฟกต์กล้องสั่น/ซูมให้ผู้เล่นทันที
            PlayerController3D_InputAction player = other.GetComponentInParent<PlayerController3D_InputAction>();
            if (player == null) player = PlayerController3D_InputAction.Instance;
            if (player != null)
            {
                player.SetChaseInfiniteStamina(false);
                player.ClearExternalCameraEffects();
            }

            // 3. สั่งทำลาย/ลบ GameObject ผีเป้าหมายทั้งหมดออกจากฉากทันที
            if (targetGhosts != null && targetGhosts.Length > 0)
            {
                foreach (GameObject ghost in targetGhosts)
                {
                    if (ghost != null)
                    {
                        Debug.Log($"👻 [GhostDespawnTrigger] ผู้เล่นเหยียบทริกเกอร์: ผี '{ghost.name}' หายไปแล้ว!");
                        Destroy(ghost);
                    }
                }
            }

            // 4. ปิดการทำงานของทริกเกอร์กันการทำงานซ้ำ
            if (triggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (targetGhosts != null)
        {
            Gizmos.color = Color.cyan;
            foreach (GameObject ghost in targetGhosts)
            {
                if (ghost != null)
                {
                    Gizmos.DrawLine(transform.position, ghost.transform.position);
                    Gizmos.DrawWireSphere(ghost.transform.position, 0.5f);
                }
            }
        }
    }
}