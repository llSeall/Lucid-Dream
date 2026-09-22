using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // รองรับ TextMeshPro

public class ProximityStaticTrigger : MonoBehaviour
{
    public enum VanishCondition
    {
        OnGetTooClose,      // หายตัวทันทีเมื่อผู้เล่นเดินเข้าใกล้มากเกินไป
        AfterStaticDuration // หายตัวหลังจากอยู่ในระยะจอซ่าครบตามเวลา
    }

    [Header("👻 Static/Glitch Settings")]
    [Tooltip("ระยะห่างสูงสุดที่จอจะเริ่มเกิดอาการซ่า (เมตร)")]
    public float maxStaticDistance = 10f;

    [Tooltip("ความแรงสูงสุดของจอซ่าเมื่อเข้าใกล้ที่สุด (0.0 ถึง 1.0)")]
    [Range(0f, 1f)]
    public float maxIntensityMultiplier = 1.0f;

    [Header("💬 Whispering Text Settings ✨")]
    [Tooltip("ติ๊กถูกหากต้องการให้มีข้อความขึ้นบนหน้าจอ")]
    public bool useMessageText = true;

    [TextArea(2, 4)]
    [Tooltip("ข้อความที่จะให้แสดงบนหน้าจอเมื่อผู้เล่นเดินเข้าใกล้")]
    public string messageText = "DONT LOOK AT IT...";

    [Tooltip("ใส่ UI TextMeshPro ที่ต้องการให้แสดงข้อความ (ถ้าใช้ TMP)")]
    public TextMeshProUGUI tmpText;

    [Tooltip("หรือจะใส่ UI Text แบบธรรมดาตรงนี้ก็ได้ (เลือกใส่อย่างใดอย่างหนึ่ง)")]
    public Text legacyText;

    [Header("✨ Vanish / Despawn Settings")]
    [Tooltip("เงื่อนไขการหายตัว")]
    public VanishCondition vanishCondition = VanishCondition.OnGetTooClose;

    [Tooltip("ระยะใกล้ที่สุดที่จะทำให้วัตถุหายตัวไป (ใช้เมื่อเลือก OnGetTooClose)")]
    public float vanishDistance = 2.0f;

    [Tooltip("ระยะเวลาที่จอซ่าก่อนที่วัตถุจะหายไปเป็นหน่วยวินาที (ใช้เมื่อเลือก AfterStaticDuration)")]
    public float staticDurationToVanish = 1.5f;

    [Header("🔊 Sound & Effects (Optional)")]
    [Tooltip("เสียงที่จะเล่นตอนวัตถุหายตัวไป")]
    public AudioClip vanishSound;

    [Tooltip("เอฟเฟกต์ Particle ที่จะโผล่ขึ้นมาตอนวัตถุหายตัวไป")]
    public GameObject vanishEffectPrefab;

    [Header("🔗 References")]
    [Tooltip("ผู้เล่น (หากปล่อยว่าง ระบบจะหา Object ที่มี Tag 'Player' ให้อัตโนมัติ)")]
    public Transform playerTransform;

    private float staticTimer = 0f;
    private bool isVanished = false;
    private AudioSource audioSource;

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null)
            {
                playerTransform = p.transform;
            }
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && vanishSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // ซ่อนตัวอักษรไว้ก่อนตอนเริ่มเกม
        SetTextAlpha(0f);
    }

    private void Update()
    {
        if (isVanished || playerTransform == null || GhostStaticEffectUI.Instance == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= maxStaticDistance)
        {
            // 1. คำนวณความแรงของจอซ่า
            float intensity = (1f - Mathf.Clamp01(distance / maxStaticDistance)) * maxIntensityMultiplier;
            GhostStaticEffectUI.Instance.ReportGhostDistance(intensity);

            // 2. ปรับการแสดงผลข้อความตามระดับความใกล้
            if (useMessageText)
            {
                UpdateTextMessage(intensity);
            }

            // 3. เช็กเงื่อนไขการหายตัว
            if (vanishCondition == VanishCondition.OnGetTooClose)
            {
                if (distance <= vanishDistance)
                {
                    Vanish();
                }
            }
            else if (vanishCondition == VanishCondition.AfterStaticDuration)
            {
                staticTimer += Time.deltaTime;
                if (staticTimer >= staticDurationToVanish)
                {
                    Vanish();
                }
            }
        }
        else
        {
            // ถ้าระยะห่างเกิน ให้ซ่อนข้อความและรีเซ็ตเวลา
            if (useMessageText) SetTextAlpha(0f);
            staticTimer = 0f;
        }
    }

    private void UpdateTextMessage(float intensity)
    {
        if (tmpText != null)
        {
            tmpText.text = messageText;
            Color c = tmpText.color;
            c.a = intensity; // ยิ่งใกล้ ตัวอักษรยิ่งชัด
            tmpText.color = c;
        }

        if (legacyText != null)
        {
            legacyText.text = messageText;
            Color c = legacyText.color;
            c.a = intensity;
            legacyText.color = c;
        }
    }

    private void SetTextAlpha(float alpha)
    {
        if (tmpText != null)
        {
            Color c = tmpText.color;
            c.a = alpha;
            tmpText.color = c;
        }

        if (legacyText != null)
        {
            Color c = legacyText.color;
            c.a = alpha;
            legacyText.color = c;
        }
    }

    private void Vanish()
    {
        if (isVanished) return;
        isVanished = true;

        // 1. ปิดจอซ่าและซ่อนข้อความทันที
        if (GhostStaticEffectUI.Instance != null)
        {
            GhostStaticEffectUI.Instance.ReportGhostDistance(0f);
        }
        SetTextAlpha(0f);

        // 2. เล่นเสียง/เอฟเฟกต์ตอนหายตัว
        if (vanishEffectPrefab != null)
        {
            Instantiate(vanishEffectPrefab, transform.position, transform.rotation);
        }

        if (vanishSound != null)
        {
            AudioSource.PlayClipAtPoint(vanishSound, transform.position);
        }

        Debug.Log($"<color=cyan>✨ [{gameObject.name}] หายตัวไปพร้อมข้อความแล้ว!</color>");

        // 3. ทำลายวัตถุทิ้ง
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        // ป้องกันไม่ให้ข้อความหรือจอซ่าค้างเมื่อวัตถุถูกซ่อนหรือทำลาย
        if (GhostStaticEffectUI.Instance != null)
        {
            GhostStaticEffectUI.Instance.ReportGhostDistance(0f);
        }
        SetTextAlpha(0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxStaticDistance);

        if (vanishCondition == VanishCondition.OnGetTooClose)
        {
            Gizmos.color = new Color(0f, 1f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, vanishDistance);
        }
    }
}