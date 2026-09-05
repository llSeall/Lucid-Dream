using System.Collections;
using UnityEngine;

public class PlayerWakeUpEffect : MonoBehaviour
{
    public static PlayerWakeUpEffect Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private CanvasGroup fadeCanvasGroup; // UI CanvasGroup จอดำสำหรับทำฟีลกระพริบตา/ลืมตา
    [SerializeField] private PlayerController3D_InputAction playerController;

    [Header("Wake Up Animation Settings")]
    [Tooltip("ระยะเวลาทั้งหมดในการลุกขึ้น (วินาที)")]
    [SerializeField] private float wakeUpDuration = 3.0f;

    [Tooltip("ตำแหน่งกล้องตอนนอน (Offset จากจุดสายตาปกติ)")]
    [SerializeField] Vector3 lyingPosOffset = new Vector3(0f, -0.9f, 0f);

    [Tooltip("มุมหมุนของกล้องตอนนอน (Pitch, Yaw, Roll) เช่น เงยหน้ามองเพดาน + ตะแคงหัว")]
    [SerializeField] Vector3 lyingRotationOffset = new Vector3(-50f, 0f, 40f);

    [Header("Optional Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wakeUpSound; // เสียงขยับผ้าห่ม หรือเสียงถอนหายใจ/บิดตัว

    private Vector3 originalCamLocalPos;
    private Quaternion originalCamLocalRot;
    private bool isWakingUp = false;

    public bool IsWakingUp => isWakingUp;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (playerController == null) playerController = GetComponent<PlayerController3D_InputAction>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// เรียกใช้ฟังก์ชันนี้เมื่อโหลดเซฟเสร็จ
    /// </summary>
    public void PlayWakeUpAnimation()
    {
    expansion:
        StopAllCoroutines();
        StartCoroutine(WakeUpRoutine());
    }

    private IEnumerator WakeUpRoutine()
    {
        isWakingUp = true;

        // บันทึกตำแหน่งและมุมกล้องดั้งเดิมไว้
        if (playerCamera != null)
        {
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;
        }

        // เล่นเสียงประกอบตอนตื่น (ถ้ามี)
        if (audioSource != null && wakeUpSound != null)
        {
            audioSource.PlayOneShot(wakeUpSound);
        }

        // ปิดการควบคุมผู้เล่น และตั้งจอดำสนิท (ปิดตา)
        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;

        float timer = 0f;

        // ตั้งตำแหน่งกล้องเริ่มต้นให้อยู่นอนอยู่บนเตียง
        Vector3 startPos = originalCamLocalPos + lyingPosOffset;
        Quaternion startRot = originalCamLocalRot * Quaternion.Euler(lyingRotationOffset);

        if (playerCamera != null)
        {
            playerCamera.localPosition = startPos;
            playerCamera.localRotation = startRot;
        }

        yield return new WaitForSeconds(0.3f); // นอนนิ่งๆ ในจอดำสักพัก

        // 🌟 เริ่มอนิเมชั่นลุกขึ้น + ลืมตา
        while (timer < wakeUpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / wakeUpDuration;

            // ใช้ SmoothStep เพื่อให้การเคลื่อนไหว นุ่มนวล สมจริง ไม่กระตุก
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // 1. ย้ายตำแหน่งและหมุนมุมกล้องกลับมาจุดยืนปกติ
            if (playerCamera != null)
            {
                playerCamera.localPosition = Vector3.Lerp(startPos, originalCamLocalPos, smoothProgress);
                playerCamera.localRotation = Quaternion.Slerp(startRot, originalCamLocalRot, smoothProgress);
            }

            // 2. เอฟเฟกต์ลืมตา (Fade In จอดำ -> ภาพใส) + กระพริบตาเบาๆ ช่วงแรก
            if (fadeCanvasGroup != null)
            {
                // จอดำค่อยๆ สว่างขึ้น
                fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothProgress * 1.2f);
            }

            yield return null;
        }

        // คืนค่าตำแหน่งกล้องให้สมบูรณ์
        if (playerCamera != null)
        {
            playerCamera.localPosition = originalCamLocalPos;
            playerCamera.localRotation = originalCamLocalRot;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;

        isWakingUp = false;
    }
}