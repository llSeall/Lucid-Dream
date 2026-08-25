using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EntityFloorTrigger : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("ระยะเวลาหน่วงก่อนผีจะเกิดหลังเหยียบ (วินาที)")]
    public float delayBeforeSpawn = 2.0f;
    [Tooltip("ให้ทำงานครั้งเดียวแล้วหายไป หรือเหยียบซ้ำได้")]
    public bool oneTimeUse = true;

    [Header("References")]
    public EntitySpawner entitySpawner;
    public string playerTag = "Player";

    private bool hasTriggered = false;

    void Start()
    {
        if (entitySpawner == null)
            entitySpawner = FindObjectOfType<EntitySpawner>();

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && oneTimeUse) return;

        if (other.CompareTag(playerTag))
        {
            hasTriggered = true;
            StartCoroutine(SpawnDelayRoutine());
        }
    }

    private IEnumerator SpawnDelayRoutine()
    {
        // หน่วงเวลาตามที่ตั้งค่า
        yield return new WaitForSeconds(delayBeforeSpawn);

        if (entitySpawner != null)
        {
            // สั่งสปอว์นผี (สคริปต์ EntityAI เดิมจะเริ่มในสถานะ Chase และล็อคเป้า playerTransform ทันที)
            entitySpawner.SpawnEntity();
        }

        if (oneTimeUse)
        {
            gameObject.SetActive(false);
        }
        else
        {
            hasTriggered = false; // รีเซ็ตให้เหยียบซ้ำได้
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // สีส้มโปร่งแสงใน Scene View
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}