using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EntityFloorTrigger : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("ระยะเวลาหน่วงก่อนผีจะเกิดหลังเหยียบ (วินาที)")]
    public float delayBeforeSpawn = 2.0f;
    [Tooltip("ให้ทำงานครั้งเดียวแล้วหายไป หรือเหยียบซ้ำได้")]
    public bool oneTimeUse = false; // ปรับ default เป็น false เพื่อให้เหยียบซ้ำได้

    [Header("Entity Detection Settings ✨")]
    [Tooltip("Tag ของ Prefab ผีในฉาก (ใช้ตรวจสอบว่ายังมีผีอยู่ในฉากหรือไม่)")]
    public string entityTag = "Enemy";

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
        if (!other.CompareTag(playerTag)) return;

        // 1. ถ้าเป็นโหมดใช้ครั้งเดียวแล้วเคยทำงานไปแล้ว ให้ข้าม
        if (hasTriggered && oneTimeUse) return;

        // 2. ถ้าเคยเหยียบไปแล้วในรอบนี้ หรือยังมีผีตัวเก่าอยู่ในฉาก จะไม่สปอว์นซ้ำ
        if (hasTriggered || IsEntityAlive()) return;

        hasTriggered = true;
        StartCoroutine(SpawnDelayRoutine());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        // เมื่อผู้เล่นเดินออกจากพื้นที่ ให้รีเซ็ตสถานะ เพื่อเปิดโอกาสให้ "เข้ามาเหยียบใหม่อีกรอบ" ได้
        hasTriggered = false;
    }

    private IEnumerator SpawnDelayRoutine()
    {
        yield return new WaitForSeconds(delayBeforeSpawn);

        // ตรวจสอบอีกครั้งก่อนสปอว์น เผื่อผีถูกสร้างขึ้นมาจากช่องทางอื่นก่อนหน้านี้
        if (!IsEntityAlive() && entitySpawner != null)
        {
            entitySpawner.SpawnEntity();
        }

        if (oneTimeUse)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// ฟังก์ชันตรวจสอบว่ายังมีผีอยู่ในฉากหรือไม่
    /// </summary>
    private bool IsEntityAlive()
    {
        // ค้นหา GameObject ในฉากที่มี Tag ตรงกับที่ตั้งไว้ (เช่น "Enemy")
        if (!string.IsNullOrEmpty(entityTag))
        {
            return GameObject.FindWithTag(entityTag) != null;
        }

        return false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}