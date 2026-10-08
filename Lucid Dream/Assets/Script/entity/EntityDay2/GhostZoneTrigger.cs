using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GhostZoneTrigger : MonoBehaviour
{
    [Header("🎯 Target Ghost AI")]
    [Tooltip("ลากผีตัวที่ควบคุมพื้นที่นี้มาใส่")]
    [SerializeField] private ZoneGhostAI targetGhost;

    [Header("🏷️ Player Tag")]
    [SerializeField] private string playerTag = "Player";

    private void Start()
    {
        // บังคับให้ Collider ของโซนเป็น Trigger
        GetComponent<Collider>().isTrigger = true;

        if (targetGhost == null)
        {
            targetGhost = GetComponentInChildren<ZoneGhostAI>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            if (targetGhost != null)
            {
                targetGhost.SetPlayerInZone(true);
                Debug.Log($"👻 [GhostZoneTrigger] ผู้เล่นเข้าโซน: {gameObject.name}");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            if (targetGhost != null)
            {
                targetGhost.SetPlayerInZone(false);
                Debug.Log($"👻 [GhostZoneTrigger] ผู้เล่นออกจากโซน: {gameObject.name}");
            }
        }
    }
}