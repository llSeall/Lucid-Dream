using System.Collections;
using UnityEngine;

public class SightJumpscareProp : MonoBehaviour
{
    [Header("🎯 Detection Settings (การตรวจจับสายตา)")]
    [Tooltip("กล้องของผู้เล่น (หากเว้นว่างไว้ ระบบจะหา MainCamera ให้อัตโนมัติ)")]
    [SerializeField] private Transform playerCamera;

    [Tooltip("ระยะห่างสูงสุดที่มองเห็นวัตถุชิ้นนี้")]
    [SerializeField] private float maxSightDistance = 12f;

    [Tooltip("องศาความกว้างของสายตากล้อง (ยิ่งน้อยต้องหันจ้องตรงกลางจอ ยิ่งมากแค่มองเห็นขอบจอก็ติด)")]
    [SerializeField] private float viewAngleThreshold = 45f;

    [Tooltip("Layer ของสิ่งกีดขวาง (กำแพง/สิ่งของ) เพื่อไม่ให้สคริปต์ทำงานทะลุกำแพง")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("💨 Slide & Despawn Settings (การเลื่อนและหายไป)")]
    [Tooltip("ทิศทางการเลื่อนหนี (อ้างอิงตามแกนของวัตถุ เช่น Z -3 คือสไลด์ถอยหลัง)")]
    [SerializeField] private Vector3 slideOffset = new Vector3(0f, 0f, -3f);

    [Tooltip("ระยะเวลาในการสไลด์เลื่อนหนี (วินาที)")]
    [SerializeField] private float slideDuration = 0.5f;

    [Tooltip("ติ๊กถูก: สั่งทำลาย GameObject ทิ้งเลย / ติ๊กออก: แค่สั่ง SetActive(false)")]
    [SerializeField] private bool destroyOnDespawn = true;

    [Header("🔊 Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip jumpscareClip;
    [Range(0f, 1f)][SerializeField] private float soundVolume = 1f;

    private bool hasTriggered = false;

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void Update()
    {
        if (hasTriggered || playerCamera == null) return;

        if (IsPlayerLookingAtProp())
        {
            TriggerJumpscare();
        }
    }

    private bool IsPlayerLookingAtProp()
    {
        Vector3 dirToProp = (transform.position - playerCamera.position).normalized;
        float distToProp = Vector3.Distance(playerCamera.position, transform.position);

        // 1. เช็กระยะห่าง
        if (distToProp > maxSightDistance) return false;

        // 2. เช็กมุมมองสายตากล้อง (Dot Product / Angle)
        float angle = Vector3.Angle(playerCamera.forward, dirToProp);
        if (angle <= viewAngleThreshold)
        {
            // 3. ยิง Raycast เช็กว่ามีกำแพงหรือสิ่งกีดขวางบังอยู่หรือไม่
            if (!Physics.Raycast(playerCamera.position, dirToProp, distToProp, obstacleMask))
            {
                return true;
            }
        }

        return false;
    }

    private void TriggerJumpscare()
    {
        hasTriggered = true;

        // เล่นเสียงจั๊มสแกร์
        if (audioSource != null && jumpscareClip != null)
        {
            audioSource.PlayOneShot(jumpscareClip, soundVolume);
        }

        // เริ่มอนิเมชันสไลด์เลื่อนหนีแล้วหายไป
        StartCoroutine(SlideAndDespawnRoutine());
    }

    private IEnumerator SlideAndDespawnRoutine()
    {
        Vector3 startPosition = transform.position;
        // คำนวณจุดหมายตามทิศทาง Local Vector ของวัตถุ
        Vector3 targetPosition = startPosition + transform.TransformDirection(slideOffset);

        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;

            // ใช้ SmoothStep เพื่อให้เคลื่อนที่ได้นุ่มนวลและรวดเร็ว
            transform.position = Vector3.Lerp(startPosition, targetPosition, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        // หายไปทันทีหลังสไลด์เสร็จ
        if (destroyOnDespawn)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // วาดเส้นทิศทางการเลื่อนหนีในหน้า Scene View เพื่อให้เห็นภาพง่ายขึ้น
        Gizmos.color = Color.red;
        Vector3 targetPos = transform.position + transform.TransformDirection(slideOffset);
        Gizmos.DrawLine(transform.position, targetPos);
        Gizmos.DrawWireSphere(targetPos, 0.2f);
    }
}