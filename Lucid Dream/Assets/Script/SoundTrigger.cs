using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(AudioSource))]
public class SoundTrigger : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioClip soundClip;
    [Range(0f, 1f)]
    public float volume = 1.0f;

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

    private AudioSource audioSource;
    private bool isCoolingDown = false;
    private bool hasTriggered = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && oneTimeUse) return;
        if (isCoolingDown) return;

        if (other.CompareTag(playerTag))
        {
            PlaySound();
        }
    }

    void PlaySound()
    {
        if (soundClip != null)
        {
            if (useFadeIn)
            {
                // หากเลือกใช้ Fade In ให้เล่นเสียงผ่าน clip หลักของ AudioSource และเริ่มเล่น Coroutine ค่อยๆ เพิ่มระดับเสียง
                audioSource.clip = soundClip;
                audioSource.volume = 0f;
                audioSource.Play();
                StartCoroutine(FadeInRoutine());
            }
            else
            {
                // เล่นแบบปกติ (ไม่มีการเฟด)
                audioSource.PlayOneShot(soundClip, volume);
            }
        }

        hasTriggered = true;

        if (oneTimeUse)
        {
            // ปิด Collider ทันทีไม่ให้เหยียบซ้ำ แล้วลบวัตถุทิ้งหลังจากเสียงเล่นจบ
            GetComponent<Collider>().enabled = false;
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
            // ค่อยๆ เพิ่มระดับเสียงจาก 0f ไปจนถึงค่า volume ที่ตั้งไว้
            audioSource.volume = Mathf.Lerp(0f, volume, timer / fadeInDuration);
            yield return null;
        }

        audioSource.volume = volume;
    }

    private IEnumerator CooldownRoutine()
    {
        isCoolingDown = true;
        yield return new WaitForSeconds(cooldownTime);
        isCoolingDown = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f); // สีฟ้าโปร่งแสงใน Scene View
        Collider col = GetComponent<Collider>();
        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}