using System.Collections;
using UnityEngine;

public class SecretPathRevealer : MonoBehaviour
{
    [Header("🔗 References")]
    [SerializeField] private EyeToggleWorldManager eyeManager;
    [SerializeField] private Transform playerCamera;

    [Header("🖼️ Picture & Door Objects")]
    [Tooltip("รูปภาพธรรมดาที่เห็นในโลกลืมตา (ตอนแรก)")]
    [SerializeField] private GameObject normalPictureObject;

    [Tooltip("รูปภาพประตูที่เห็นในโลกหลับตา")]
    [SerializeField] private GameObject closedEyeDoorPictureObject;

    [Header("🧱 Wall & Real Door Objects")]
    [Tooltip("กำแพงทึบเดิมตรงจุดนั้น (จะถูกปิดทิ้งเพื่อไม่ให้ซ้อนกับประตู)")]
    [SerializeField] private GameObject solidWallObject;

    [Tooltip("ประตูจริง / โมเดลกำแพงที่มีช่องเจาะพร้อมประตู ที่จะโผล่มาแทนที่")]
    [SerializeField] private GameObject realDoorObject;

    [Header("👁️ Vision Detection Settings")]
    [Tooltip("ระยะห่างสูงสุดที่ต้องมองวัตถุ (เมตร)")]
    [SerializeField] private float maxLookDistance = 6f;

    [Tooltip("เวลาที่ต้องจ้องมองวัตถุตอนหลับตา (วินาที)")]
    [SerializeField] private float requiredLookTime = 0.4f;

    [Header("🔊 Sound Effects (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip seenSound;      // เสียงตอนจ้องรูปประตูสำเร็จ
    [SerializeField] private AudioClip revealSound;    // เสียงตอนรูปหายไปกลายเป็นประตูจริง

    private bool hasBeenSeen = false;
    private bool isPathRevealed = false;
    private float currentLookTimer = 0f;

    private void Start()
    {
        if (eyeManager == null)
            eyeManager = FindObjectOfType<EyeToggleWorldManager>();

        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        // เริ่มต้น: ซ่อนประตูจริงไว้ก่อน
        if (realDoorObject != null)
            realDoorObject.SetActive(false);
    }

    private void Update()
    {
        if (eyeManager == null || playerCamera == null || isPathRevealed) return;

        // 1. ถ้ามองเห็นรูปประตูตอนหลับตาแล้ว ให้รอจังหวะ "ลืมตา"
        if (hasBeenSeen)
        {
            if (!eyeManager.IsEyesClosed)
            {
                RevealSecretPath();
            }
            return;
        }

        // 2. ตรวจจับการจ้องมองขณะ "หลับตา"
        if (eyeManager.IsEyesClosed)
        {
            CheckIfPlayerIsLooking();
        }
        else
        {
            currentLookTimer = 0f;
        }
    }

    private void CheckIfPlayerIsLooking()
    {
        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxLookDistance))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                currentLookTimer += Time.deltaTime;

                if (currentLookTimer >= requiredLookTime)
                {
                    OnObjectSeen();
                }
            }
            else
            {
                currentLookTimer = 0f;
            }
        }
        else
        {
            currentLookTimer = 0f;
        }
    }

    private void OnObjectSeen()
    {
        hasBeenSeen = true;
        Debug.Log("<color=yellow><b>[Puzzle] จ้องมองรูปประตูในโลกหลับตาเรียบร้อย!</b></color>");

        if (audioSource != null && seenSound != null)
        {
            audioSource.PlayOneShot(seenSound);
        }
    }

    private void RevealSecretPath()
    {
        isPathRevealed = true;

        // 1. ปลดล็อกวัตถุทั้งหมดออกจากระบบ EyeToggle เพื่อไม่ให้โดนสลับไปมาอีก
        if (eyeManager != null)
        {
            eyeManager.UnregisterObject(normalPictureObject);
            eyeManager.UnregisterObject(closedEyeDoorPictureObject);
            eyeManager.UnregisterObject(solidWallObject);
            eyeManager.UnregisterObject(realDoorObject);
        }

        // 2. ซ่อนรูปภาพเดิมและกำแพงทึบออก
        if (normalPictureObject != null) normalPictureObject.SetActive(false);
        if (closedEyeDoorPictureObject != null) closedEyeDoorPictureObject.SetActive(false);
        if (solidWallObject != null) solidWallObject.SetActive(false);

        // 3. แสดงประตูจริง / ช่องทางเดินขึ้นมาแทนที่
        if (realDoorObject != null) realDoorObject.SetActive(true);

        if (audioSource != null && revealSound != null)
        {
            audioSource.PlayOneShot(revealSound);
        }

        Debug.Log("<color=green><b>[Puzzle] ซ่อนกำแพงทึบและเปิดประตูจริงในโลกลืมตาเรียบร้อย!</b></color>");

        enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(playerCamera.position, playerCamera.forward * maxLookDistance);
        }
    }
}