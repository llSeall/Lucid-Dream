using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class HintDisableTrigger : MonoBehaviour
{
    [Header("🎯 Target Hint System")]
    [Tooltip("ลาก LostHintTimer ของโซนนี้มาใส่")]
    [SerializeField] private LostHintTimer targetHintTimer;

    private void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (targetHintTimer != null)
            {
                targetHintTimer.DisableHintPermanently();

                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (targetHintTimer != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, targetHintTimer.transform.position);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}