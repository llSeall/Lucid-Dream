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
    [Tooltip("เสียงตอนปลดล็อกประตูสำเร็จ (เสียงไขกุญแจ/เสียงคลิกปลดล็อก)")]
    public AudioClip unlockSoundClip;

    [Header("Dynamic Real-time Creak Sound ✨")]
    public AudioClip creakLoopClip;
    [Range(0f, 1f)] public float maxVolume = 0.8f;
    public float fadeInSpeed = 5.0f;
    public float fadeOutSpeed = 4.0f;
    public float minAngularSpeed = 0.08f;
    public bool requirePlayerContact = true;

    private Rigidbody rb;
    private HingeJoint hinge;
    private Quaternion initialRotation; // ✨ จำมุมหมุนเริ่มต้นของประตู
    private bool isPlayerNearby = false;
    private bool isPlayerTouching = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        hinge = GetComponent<HingeJoint>();
        initialRotation = transform.rotation; // บันทึกตำแหน่ง/มุมปิดประตูเดิม

        if (doorAudioSource == null)
        {
            doorAudioSource = GetComponent<AudioSource>();
        }

        rb.isKinematic = isLocked;
        HideAllUI();
    }

    void Update()
    {
        if (isPlayerNearby && isLocked)
        {
            UpdateUI();

            if (Input.GetKeyDown(KeyCode.E))
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

        HandleRealtimeDoorSound();
    }

    #region Real-time Sound Logic ✨
    void HandleRealtimeDoorSound()
    {
        if (isLocked || doorAudioSource == null || creakLoopClip == null) return;

        float doorRotationSpeed = rb.angularVelocity.magnitude;
        bool isDoorMoving = doorRotationSpeed > minAngularSpeed;
        bool shouldPlaySound = isDoorMoving && (!requirePlayerContact || isPlayerTouching);

        if (shouldPlaySound)
        {
            if (!doorAudioSource.isPlaying || doorAudioSource.clip != creakLoopClip)
            {
                doorAudioSource.clip = creakLoopClip;
                doorAudioSource.loop = true;
                doorAudioSource.volume = 0f;
                doorAudioSource.Play();
            }

            float targetPitch = Mathf.Clamp(0.85f + (doorRotationSpeed * 0.15f), 0.85f, 1.25f);
            doorAudioSource.pitch = Mathf.Lerp(doorAudioSource.pitch, targetPitch, Time.deltaTime * 3f);
            doorAudioSource.volume = Mathf.MoveTowards(doorAudioSource.volume, maxVolume, Time.deltaTime * fadeInSpeed);
        }
        else
        {
            if (doorAudioSource.isPlaying && doorAudioSource.clip == creakLoopClip)
            {
                doorAudioSource.volume = Mathf.MoveTowards(doorAudioSource.volume, 0f, Time.deltaTime * fadeOutSpeed);

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

    /// <summary>
    /// ✨ ฟังก์ชันสั่งล็อกประตูจากสคริปต์ภายนอก
    /// </summary>
    public void LockDoor(bool closeDoorFirst = true)
    {
        isLocked = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // ดึงประตูกลับมาปิดสนิทก่อนล็อก (ถ้าเปิดตัวเลือกไว้)
        if (closeDoorFirst)
        {
            transform.rotation = initialRotation;
        }

        rb.isKinematic = true; // ล็อกฟิสิกส์ไม่ให้ประตูขยับได้อีก
        HideAllUI();
        PlayOneShotSound(lockedSoundClip);
    }

    public void UnlockDoor()
    {
        if (!isLocked) return;

        isLocked = false;
        rb.isKinematic = false;
        HideAllUI();
        PlayOneShotSound(unlockSoundClip);
    }

    public void PlayOneShotSound(AudioClip clip)
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

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerTouching = true;

            if (isLocked)
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
            isPlayerTouching = false;
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