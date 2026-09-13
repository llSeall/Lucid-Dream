using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class FlickeringLightTrigger : MonoBehaviour
{
    [System.Serializable]
    public class LightFixture
    {
        public string name = "Light Fixture";
        [Tooltip("คอมโพเนนต์ Light (ดวงไฟ)")]
        public Light lightComponent;
        [Tooltip("Mesh Renderer ของหลอดไฟ 3D (ที่มี Emissive Material)")]
        public Renderer bulbRenderer;
        [Tooltip("ลำดับของ Material เรืองแสงบนโมเดล (ส่วนใหญ่คือ 0)")]
        public int materialIndex = 0;
        [Tooltip("AudioSource 3D ที่ติดอยู่ ณ ตำแหน่งหลอดไฟดวงนั้น")]
        public AudioSource lightAudioSource;
    }

    [Header("🎯 Target Light Fixtures")]
    public List<LightFixture> lightFixtures = new List<LightFixture>();

    [Header("☑️ Trigger Mode Checkboxes")]
    [Tooltip("ติ๊กถูก: เดินชนแล้วไฟดับสนิททันที (ไม่กระพริบ)")]
    public bool instantTurnOff = false;

    [Tooltip("ติ๊กถูก: ให้ไฟกระพริบค้างไว้เรื่อยๆ ตลอดไปไม่ยอมหยุด ✨")]
    public bool endlessFlicker = false;

    [Tooltip("ติ๊กถูก: หลังกระพริบจบตามเวลา ให้ไฟเปิดกลับมาติดปกติ / เอาออก: ให้ไฟดับสนิท (ใช้เฉพาะกรณี endlessFlicker = false)")]
    public bool endStateIsOn = false;

    [Header("⚙️ Flicker Settings")]
    [Tooltip("ระยะเวลาที่ไฟกระพริบ (วินาที) - จะถูกข้ามหากเลือก endlessFlicker")]
    public float flickerDuration = 2.5f;
    public float minIntensity = 0.0f;
    public float maxIntensity = 1.5f;
    public float minFlickerSpeed = 0.03f;
    public float maxFlickerSpeed = 0.12f;

    [Header("🔊 Sound Clips")]
    [Tooltip("เสียงไฟช็อต/กระพริบ (จะวนลูปเล่นไปเรื่อยๆ หากเลือก endlessFlicker)")]
    public AudioClip flickerSoundClip;
    [Tooltip("เสียงไฟดับพรึ่บ (สำหรับโหมดดับทันที)")]
    public AudioClip turnOffSoundClip;

    [Header("🔒 State Settings")]
    public bool triggerOnce = true;

    private bool isProcessing = false;
    private bool hasTriggered = false;
    private Dictionary<Light, float> originalIntensities = new Dictionary<Light, float>();

    void Start()
    {
        GetComponent<BoxCollider>().isTrigger = true;

        foreach (var fixture in lightFixtures)
        {
            if (fixture.lightComponent != null && !originalIntensities.ContainsKey(fixture.lightComponent))
            {
                originalIntensities.Add(fixture.lightComponent, fixture.lightComponent.intensity);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.transform.root.CompareTag("Player"))
        {
            if (triggerOnce && hasTriggered) return;
            if (isProcessing) return;

            hasTriggered = true;

            // ✨ ปิดเฉพาะคอมโพเนนต์ Collider เพื่อไม่ให้เหยียบซ้ำ แต่ปล่อยให้ GameObject และ Coroutine ทำงานต่อ!
            if (triggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }

            if (instantTurnOff)
            {
                TurnOffInstantly();
            }
            else
            {
                StartCoroutine(FlickerRoutine());
            }
        }
    }

    private void TurnOffInstantly()
    {
        foreach (var fixture in lightFixtures)
        {
            SetFixtureState(fixture, false, 0f);

            if (fixture.lightAudioSource != null && turnOffSoundClip != null)
            {
                fixture.lightAudioSource.PlayOneShot(turnOffSoundClip);
            }
        }
    }

    private IEnumerator FlickerRoutine()
    {
        isProcessing = true;
        float timer = 0f;

        // สั่งเล่นเสียงไฟช็อตแบบ Loop ที่ตำแหน่งหลอดไฟทุกดวง
        foreach (var fixture in lightFixtures)
        {
            if (fixture.lightAudioSource != null && flickerSoundClip != null)
            {
                fixture.lightAudioSource.clip = flickerSoundClip;
                fixture.lightAudioSource.loop = true;
                fixture.lightAudioSource.Play();
            }
        }

        // 💡 วนลูปกระพริบไฟ (ถ้าติ๊ก endlessFlicker ไว้ เงื่อนไขจะไม่มีวันจบลูป)
        while (endlessFlicker || timer < flickerDuration)
        {
            float waitTime = Random.Range(minFlickerSpeed, maxFlickerSpeed);
            if (!endlessFlicker) timer += waitTime;

            foreach (var fixture in lightFixtures)
            {
                bool isLightOn = Random.value > 0.35f;
                float targetIntensity = isLightOn ? Random.Range(minIntensity, maxIntensity) : 0f;

                SetFixtureState(fixture, isLightOn, targetIntensity);
            }

            yield return new WaitForSeconds(waitTime);
        }

        // --- ส่วนด้านล่างนี้จะถูกข้ามเมื่อเลือก endlessFlicker = true ---

        // หยุดเสียงกระพริบเมื่อหมดเวลา
        foreach (var fixture in lightFixtures)
        {
            if (fixture.lightAudioSource != null && fixture.lightAudioSource.clip == flickerSoundClip)
            {
                fixture.lightAudioSource.Stop();
            }
        }

        // คืนค่าสถานะสุดท้ายหลังจบเวลา
        foreach (var fixture in lightFixtures)
        {
            float finalIntensity = endStateIsOn && originalIntensities.ContainsKey(fixture.lightComponent)
                ? originalIntensities[fixture.lightComponent]
                : 0f;

            SetFixtureState(fixture, endStateIsOn, finalIntensity);
        }

        isProcessing = false;
    }

    // สั่งเปิด-ปิด แสง 3D และ Emission บน Material พร้อมกัน
    private void SetFixtureState(LightFixture fixture, bool isOn, float intensity)
    {
        if (fixture.lightComponent != null)
        {
            fixture.lightComponent.enabled = isOn;
            fixture.lightComponent.intensity = intensity;
        }

        if (fixture.bulbRenderer != null)
        {
            Material mat = fixture.bulbRenderer.materials[fixture.materialIndex];
            if (isOn)
            {
                mat.EnableKeyword("_EMISSION");
            }
            else
            {
                mat.DisableKeyword("_EMISSION");
            }
        }
    }

    /// <summary>
    /// ฟังก์ชัน public สำหรับสั่งหยุดกระพริบจากสคริปต์ภายนอก (เผื่อกรณีแก้ปริศนาเสร็จแล้วอยากให้ไฟหยุดกระพริบ)
    /// </summary>
    public void StopEndlessFlicker(bool turnLightOn = true)
    {
        endlessFlicker = false;
        flickerDuration = 0f;
        endStateIsOn = turnLightOn;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var fixture in lightFixtures)
        {
            if (fixture.lightComponent != null)
            {
                Gizmos.DrawLine(transform.position, fixture.lightComponent.transform.position);
                Gizmos.DrawWireSphere(fixture.lightComponent.transform.position, 0.25f);
            }
        }
    }
}