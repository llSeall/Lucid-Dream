using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
public class LockedHingeDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public string requiredKeyID = "MainKey";
    public bool isLocked = true;

    [Header("UI Settings ✨")]
    public GameObject lockedIconUI;
    public GameObject interactIconUI;

    [Header("Audio Source ✨")]
    public AudioSource doorAudioSource;

    [Header("One-Shot Sounds ✨")]
    [Tooltip("เสียงพยายามเปิดประตูขณะล็อกอยู่ (เสียงขยับลูกบิด/ประตูติด)")]
    public AudioClip lockedSoundClip;
    [Tooltip("เสียงตอนกดปลดล็อกประตูสำเร็จ (เสียงไขกุญแจ)")]
    public AudioClip unlockSoundClip;

    [Header("Dynamic Real-time Creak Sound ✨")]
    [Tooltip("ไฟล์เสียงเอี๊ยดประตูแบบ Loop (วนลูป)")]
    public AudioClip creakLoopClip;
    [Tooltip("ระดับความดังสูงสุด")]
    [Range(0f, 1f)] public float maxVolume = 0.8f;
    [Tooltip("ความเร็วในการ Fade In ของเสียง")]
    public float fadeInSpeed = 5.0f;
    [Tooltip("ความเร็วในการ Fade Out เมื่อหยุดชน/หยุดเดิน")]
    public float fadeOutSpeed = 4.0f;
    [Tooltip("ความเร็วการหมุนขั้นต่ำของประตูที่จะเริ่มเล่นเสียง")]
    public float minAngularSpeed = 0.08f;
    [Tooltip("ต้องให้ผู้เล่นตัวชนประตูอยู่ด้วยเท่านั้นถึงจะมีเสียงหรือไม่")]
    public bool requirePlayerContact = true;

    private Rigidbody rb;
    private HingeJoint hinge;
    private bool isPlayerNearby = false;
    private bool isPlayerTouching = false; // ตรวจจับว่าผู้เล่นกำลังเอาตัวชนประตูอยู่ไหม

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();

        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
        }

        rb.isKinematic = isLocked;
        HideAllUI();
    }

    void Update()
    {
        if (isPlayerNearby)
        {
            UpdateUI();

            if (Input.GetKeyDown(KeyCode.E) && isLocked)
            {
                if (PlayerKeyHolder.Instance != null && PlayerKeyHolder.Instance.HasKey(requiredKeyID))
                {
                    UnlockDoor();
                }
                else
                {
                    PlayOneShotSound(lockedSoundClip);
                }
            }
        }

        // ประมวลผลเสียงประตูแบบเรียลไทม์ทุกเฟรม ✨
        HandleRealtimeDoorSound();
    }

    #region Real-time Sound Logic ✨
    void HandleRealtimeDoorSound()
    {
        if (isLocked || doorAudioSource == null || creakLoopClip == null) return;

        // เช็กความเร็วการหมุนของประตูจากฟิสิกส์ Rigidbody
        float doorRotationSpeed = rb.angularVelocity.magnitude;
        bool isDoorMoving = doorRotationSpeed > minAngularSpeed;

        // เงื่อนไขในการเล่นเสียง: ประตูกำลังหมุนจริง + (ถ้าเปิดตัวเลือกไว้) ผู้เล่นต้องชนประตูอยู่
        bool shouldPlaySound = isDoorMoving && (!requirePlayerContact || isPlayerTouching);

        if (shouldPlaySound)
        {
            // ถ้ายังไม่ได้เริ่มเล่นเสียง ให้เริ่มเล่นไฟล์แบบ Loop
            if (!doorAudioSource.isPlaying || doorAudioSource.clip != creakLoopClip)
            {
                doorAudioSource.clip = creakLoopClip;
                doorAudioSource.loop = true;
                doorAudioSource.volume = 0f; // เริ่มที่ 0 เพื่อความนุ่มนวล
                doorAudioSource.Play();
            }

            // ปรับ Pitch เล็กน้อยตามความเร็วหมุนของประตู (หมุนเร็วเสียงจะแหลมขึ้นเล็กน้อย)
            float targetPitch = Mathf.Clamp(0.85f + (doorRotationSpeed * 0.15f), 0.85f, 1.25f);
            doorAudioSource.pitch = Mathf.Lerp(doorAudioSource.pitch, targetPitch, Time.deltaTime * 3f);

            // ค่อยๆ Fade In ความดังขึ้นไปจนถึง maxVolume
            doorAudioSource.volume = Mathf.MoveTowards(doorAudioSource.volume, maxVolume, Time.deltaTime * fadeInSpeed);
        }
        else
        {
            // หากผู้เล่นถอยออก หรือหยุดดันจนประตูหยุดหมุน -> Fade Out เสียงดับไป
            if (doorAudioSource.isPlaying && doorAudioSource.clip == creakLoopClip)
            {
                doorAudioSource.volume = Mathf.MoveTowards(doorAudioSource.volume, 0f, Time.deltaTime * fadeOutSpeed);

                // เมื่อความดังเหลือ 0 ให้หยุดเล่น
                if (doorAudioSource.volume <= 0.001f)
                {
                    doorAudioSource.Stop();
                }
            }
        }
    }
    #endregion

    void UpdateUI()
    {
        if (!isLocked)
        {
            HideAllUI();
            return;
        }

        bool hasKey = (PlayerKeyHolder.Instance != null && PlayerKeyHolder.Instance.HasKey(requiredKeyID));

        if (hasKey)
        {
            if (lockedIconUI != null) lockedIconUI.SetActive(false);
            if (interactIconUI != null) interactIconUI.SetActive(true);
        }
        else
        {
            if (lockedIconUI != null) lockedIconUI.SetActive(true);
            if (interactIconUI != null) interactIconUI.SetActive(false);
        }
    }

    void UnlockDoor()
    {
        isLocked = false;
        rb.isKinematic = false;
        HideAllUI();
        PlayOneShotSound(unlockSoundClip);
    }

    void PlayOneShotSound(AudioClip clip)
    {
        if (doorAudioSource != null && clip != null)
        {
            doorAudioSource.pitch = Random.Range(0.95f, 1.05f);
            doorAudioSource.PlayOneShot(clip, maxVolume);
        }
    }

    void HideAllUI()
    {
        if (lockedIconUI != null) lockedIconUI.SetActive(false);
        if (interactIconUI != null) interactIconUI.SetActive(false);
    }

    // ✨ ตรวจจับการเข้าชนและการผละออกจากประตูของผู้เล่น
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerTouching = true;

            if (isLocked)
            {
                PlayOneShotSound(lockedSoundClip);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerTouching = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerTouching = false; // เมื่อเดินถอยออกจากประตู
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = true;
            UpdateUI();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNearby = false;
            isPlayerTouching = false;
            HideAllUI();
        }
    }
}