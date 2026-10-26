using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SoundTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip soundClip;
    [Range(0f, 1f)]
    public float volume = 1.0f;

    [Header("🎯 Sound Location Settings ✨")]
    [Tooltip("ลาก AudioSource ของจุดที่ต้องการให้เสียงเกิดมาใส่ (หากเว้นว่างไว้ เสียงจะเกิดที่ตัวทริกเกอร์เอง)")]
    public AudioSource targetAudioSource;

    [Header("Fade Settings ✨")]
    [Tooltip("ติ๊กถูกหากต้องการให้เสียงค่อยๆ เฟดดังขึ้นมาจาก 0")]
    public bool useFadeIn = false;
    [Tooltip("ระยะเวลา (วินาที) ที่จะให้เสียงเฟดจาก 0 ขึ้นไปจนถึงค่า Volume หลัก")]
    public float fadeInDuration = 1.0f;

    [Header("Trigger Behavior")]
    [Tooltip("ติ๊กถูกหากต้องการให้เหยียบติดเสียงครั้งเดียวแล้วทำลายวัตถุทิ้ง")]
    public bool oneTimeUse = true;
    [Tooltip("ระยะเวลาคูลดาวน์ก่อนจะเหยียบเกิดเสียงซ้ำได้อีกครั้ง (ใช้กรณี oneTimeUse = false)")]
    public float cooldownTime = 3.0f;

    [Header("References")]
    public string playerTag = "Player";

    private AudioSource activeAudioSource;
    private bool isCoolingDown = false;
    private bool hasTriggered = false;

    void Start()
    {
        // หากมีการใส่ targetAudioSource ให้ใช้จุดนั้น หากไม่มีให้ใช้ AudioSource บนตัวเอง
        if (targetAudioSource != null)
        {
            activeAudioSource = targetAudioSource;
        }
        else
        {
            activeAudioSource = GetComponent<AudioSource>();
            if (activeAudioSource == null)
            {
                activeAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        if (activeAudioSource != null)
        {
            activeAudioSource.playOnAwake = false;
        }

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && oneTimeUse) return;
        if (isCoolingDown) return;

        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            PlaySound();
        }
    }

    void PlaySound()
    {
        if (soundClip != null && activeAudioSource != null)
        {
            if (useFadeIn)
            {
                activeAudioSource.clip = soundClip;
                activeAudioSource.volume = 0f;
                activeAudioSource.Play();
                StartCoroutine(FadeInRoutine());
            }
            else
            {
                activeAudioSource.PlayOneShot(soundClip, volume);
            }
        }

        hasTriggered = true;

        if (oneTimeUse)
        {
            // ปิด Collider ทันทีไม่ให้เหยียบซ้ำ
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            float soundDuration = soundClip != null ? soundClip.length : 1.0f;
            Destroy(gameObject, soundDuration);
        }
        else
        {
            StartCoroutine(CooldownRoutine());
        }
    }

    private IEnumerator FadeInRoutine()
    {
        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            activeAudioSource.volume = Mathf.Lerp(0f, volume, timer / fadeInDuration);
            yield return null;
        }

        activeAudioSource.volume = volume;
    }

    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        yield return new WaitForSeconds(cooldownTime);
        isCoolingDown = false;
    }

    private void OnDrawGizmos()
    {
        // วาดกล่องทริกเกอร์สีฟ้า
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }

        // วาดเส้นสีเหลืองเชื่อมไปยังจุดกำเนิดเสียงใน Scene View
        if (targetAudioSource != null)
        {
            Gizmos.matrix = Matrix4x4.identity;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetAudioSource.transform.position);
            Gizmos.DrawWireSphere(targetAudioSource.transform.position, 0.4f);
        }
    }
}