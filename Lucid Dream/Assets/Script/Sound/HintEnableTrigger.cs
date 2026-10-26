using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class HintEnableTrigger : MonoBehaviour
{
    [Header("🎯 Target Hint System")]
    [Tooltip("ลาก LostHintTimer ของโซนนี้มาใส่")]
    [SerializeField] private LostHintTimer targetHintTimer;

    [Header("⚙️ Trigger Options")]
    [Tooltip("ทำงานแค่ครั้งแรกที่เหยียบเข้าโซนหรือไม่")]
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

            if (targetHintTimer != null)
            {
                hasTriggered = true;
                targetHintTimer.StartHintTimer();

                if (triggerOnce)
                {
                    Collider col = GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (targetHintTimer != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, targetHintTimer.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}