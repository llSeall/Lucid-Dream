using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AntiCampZone : MonoBehaviour
{
    [Header("Anti-Camp Settings")]
    [Tooltip("ระยะเวลาสูงสุดที่ยอมให้อยู่ในห้องนี้ได้ก่อน Entity จะสปอว์น (วินาที)")]
    public float maxStayTime = 90f; // เช่น 90 วินาที (1.5 นาที)

    [Tooltip("เวลาคูลดาวน์หลังสปอว์นไปแล้ว ก่อนจะเริ่มนับเวลาใหม่ (ป้องกันสปอว์นติดๆ กัน)")]
    public float cooldownTime = 30f;

    [Header("References")]
    public EntitySpawner entitySpawner;
    public string playerTag = "Player";

    private float timer = 0f;
    private bool isPlayerInside = false;
    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;

    void Start()
    {
        if (entitySpawner == null)
        {
            entitySpawner = FindObjectOfType<EntitySpawner>();
        }

        // บังคับให้ Collider เป็น Trigger เสมอ
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    void Update()
    {
        // จัดการสถานะคูลดาวน์หลังผีเกิดไปแล้ว
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                timer = 0f;
            }
            return;
        }

        // ถ้านักเลงแคมป์อยู่ในห้อง ให้เริ่มนับเวลาถอยหลัง/เดินหน้า
        if (isPlayerInside)
        {
            timer += Time.deltaTime;

            if (timer >= maxStayTime)
            {
                TriggerAntiCampSpawn();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = true;
            timer = 0f; // เริ่มนับเวลาใหม่ทันทีที่เข้าห้อง
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInside = false;
            timer = 0f; // รีเซ็ตเวลาเมื่อผู้เล่นยอมออกจากห้อง
        }
    }

    void TriggerAntiCampSpawn()
    {
        Debug.Log($"[Anti-Camp] Player stayed in {gameObject.name} for too long! Spawning Entity.");

        if (entitySpawner != null)
        {
            entitySpawner.SpawnEntity();
        }

        // เข้าสู่ช่วงคูลดาวน์ ให้เวลาผู้เล่นวิ่งหนีออกจากห้อง
        isOnCooldown = true;
        cooldownTimer = cooldownTime;
    }

    // แสดงขอบเขตโซนห้องเป็นกล่องสีแดงโปร่งแสงใน Scene View
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.15f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}