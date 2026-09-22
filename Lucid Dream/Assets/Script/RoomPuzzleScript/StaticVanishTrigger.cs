using System.Collections;
using UnityEngine;
using UnityEngine.Localization;

public class StaticVanishTrigger : MonoBehaviour
{
    public enum DetectionMode
    {
        Distance,               // คำนวณระยะห่างจากจุด Pivot กลางวัตถุ
        ClosestPointOnCollider, // คำนวณระยะห่างจากขอบ Collider ที่ใกล้ผู้เล่นที่สุด (เหมาะกับวัตถุใหญ่ๆ)
        TriggerColliderEnter    // ทำงานทันทีเมื่อผู้เล่นเดินชน Trigger Collider
    }

    [Header("🎯 Detection Mode Settings")]
    [Tooltip("เลือกรูปแบบการตรวจจับผู้เล่น")]
    public DetectionMode detectionMode = DetectionMode.Distance;

    [Tooltip("ใส่ Collider ของวัตถุนี้ (หากปล่อยว่าง ระบบจะดึง Collider บน GameObject นี้ให้อัตโนมัติ)")]
    public Collider targetCollider;

    [Header("⚡ Glitch Flash Settings")]
    [Tooltip("ระยะห่างที่ผู้เล่นเดินเข้าใกล้แล้วจะเกิดจอซ่า (เมตร) (ใช้กับโหมด Distance / ClosestPointOnCollider)")]
    public float triggerDistance = 3.0f;

    [Tooltip("ความแรงของจอซ่าที่โผล่ขึ้นมาแว๊บแรก (0.1 - 1.0)")]
    [Range(0.1f, 1f)]
    public float glitchIntensity = 1.0f;

    [Tooltip("ระยะเวลา (วินาที) ที่จอสว่างซ่าแว๊บหนึ่งก่อนไอเท็มจะหายไป (แนะนำ 0.15 - 0.3)")]
    public float flashDuration = 0.2f;

    [Header("💬 Custom Localized Message ✨")]
    [Tooltip("ติ๊กถูกหากต้องการให้แสดงข้อความประจำไอเท็มชิ้นนี้")]
    public bool showCustomMessage = true;

    [Tooltip("เลือก String Table และ Entry ข้อความแปลภาษาเฉพาะสำหรับไอเท็มชิ้นนี้")]
    public LocalizedString customMessage;

    [Header("🔊 Sound & FX (Optional)")]
    [Tooltip("เสียงที่จะเล่นในจังหวะที่จอซ่าและไอเท็มหายไป")]
    public AudioClip vanishSound;

    [Tooltip("เอฟเฟกต์ Particle ที่จะโผล่มาตอนหายตัวไป")]
    public GameObject vanishEffectPrefab;

    [Header("🔗 Player Target Settings")]
    public string playerTag = "Player";
    public Transform playerTransform;

    private bool isTriggered = false;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag(playerTag);
            if (p != null)
            {
                playerTransform = p.transform;
            }
        }

        if (targetCollider == null)
        {
            targetCollider = GetComponent<Collider>();
        }
    }

    private void Update()
    {
        if (isTriggered || playerTransform == null) return;

        if (detectionMode == DetectionMode.Distance)
        {
            float distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance <= triggerDistance)
            {
                TriggerGlitchAndVanish();
            }
        }
        else if (detectionMode == DetectionMode.ClosestPointOnCollider)
        {
            if (targetCollider != null)
            {
                // คำนวณหาจุดบนขอบ Collider ที่อยู่ใกล้ตัวผู้เล่นมากที่สุด
                Vector3 closestPoint = targetCollider.ClosestPoint(playerTransform.position);
                float distance = Vector3.Distance(closestPoint, playerTransform.position);

                if (distance <= triggerDistance)
                {
                    TriggerGlitchAndVanish();
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isTriggered) return;

        if (detectionMode == DetectionMode.TriggerColliderEnter)
        {
            if (other.CompareTag(playerTag) || (playerTransform != null && other.transform == playerTransform))
            {
                TriggerGlitchAndVanish();
            }
        }
    }

    private void TriggerGlitchAndVanish()
    {
        isTriggered = true;
        StartCoroutine(GlitchVanishRoutine());
    }

    private IEnumerator GlitchVanishRoutine()
    {
        string localizedText = "";
        if (showCustomMessage && customMessage != null && !customMessage.IsEmpty)
        {
            localizedText = customMessage.GetLocalizedString();
        }

        // 1. เรียกใช้คำสั่ง TriggerCustomGlitch ของ GhostStaticEffectUI
        if (GhostStaticEffectUI.Instance != null)
        {
            GhostStaticEffectUI.Instance.TriggerCustomGlitch(glitchIntensity, localizedText, flashDuration);
        }

        // 2. เล่นเสียงเอฟเฟกต์ (ถ้ามี)
        if (vanishSound != null)
        {
            AudioSource.PlayClipAtPoint(vanishSound, transform.position);
        }

        // 3. หน่วงเวลาตามระยะเวลาที่ตั้งไว้
        if (flashDuration > 0f)
        {
            yield return new WaitForSeconds(flashDuration);
        }

        // 4. แสดง Particle เอฟเฟกต์ (ถ้ามี)
        if (vanishEffectPrefab != null)
        {
            Instantiate(vanishEffectPrefab, transform.position, transform.rotation);
        }

        // 5. ล้างข้อความและปิดเอฟเฟกต์จอซ่าออก
        if (GhostStaticEffectUI.Instance != null)
        {
            GhostStaticEffectUI.Instance.ClearCustomGlitch();
        }

        // 6. ทำลายไอเท็มชิ้นนี้ทิ้ง
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (GhostStaticEffectUI.Instance != null)
        {
            GhostStaticEffectUI.Instance.ClearCustomGlitch();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (detectionMode == DetectionMode.Distance)
        {
            Gizmos.color = new Color(1f, 0.4f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, triggerDistance);
        }
        else if (detectionMode == DetectionMode.ClosestPointOnCollider && targetCollider != null)
        {
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(targetCollider.bounds.center, targetCollider.bounds.size + Vector3.one * (triggerDistance * 2f));
        }
    }
}