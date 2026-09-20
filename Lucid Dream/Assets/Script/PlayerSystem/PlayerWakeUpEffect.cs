using System.Collections;
using UnityEngine;

public class PlayerWakeUpEffect : MonoBehaviour
{
    public static PlayerWakeUpEffect Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private PlayerController3D_InputAction playerController;

    [Header("Wake Up Animation Settings")]
    [Tooltip("ระยะเวลาในการลุกขึ้นตอนเช้าหรือตอนสะดุ้งตื่น (วินาที)")]
    [SerializeField] private float wakeUpDuration = 3.0f;

    [Tooltip("ระยะเวลาสะดุ้งตื่นเมื่อโดนผีจับได้ (แนะนำให้เร็วกว่าปกติเพื่อความสมจริง)")]
    [SerializeField] private float gaspWakeUpDuration = 1.8f;

    [Tooltip("ระยะเวลาค่อยๆ เลือนจอดำหายไปตอนกลางคืนปกติ (วินาที)")]
    [SerializeField] private float nightFadeDuration = 1.5f;

    [Tooltip("ตำแหน่งกล้องตอนนอน (Offset จากจุดสายตาปกติ)")]
    [SerializeField] Vector3 lyingPosOffset = new Vector3(0f, -0.9f, 0f);

    [Tooltip("มุมหมุนของกล้องตอนนอน (Pitch, Yaw, Roll) เช่น เงยหน้ามองเพดาน + ตะแคงหัว")]
    [SerializeField] Vector3 lyingRotationOffset = new Vector3(-50f, 0f, 40f);

    [Header("Optional Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wakeUpSound;     // เสียงบิดตัว/ขยับผ้าห่ม
    [SerializeField] private AudioClip gaspStartleSound; // เสียงสะดุ้งเฮือก

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

        // สั่งให้ ScreenFader ปิดจอดำสนิททันทีเมื่อ Scene โหลดขึ้นมา
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.SetBlackInstant();
        }
    }

    private void Start()
    {
        PlayWakeUpAnimation();
    }

    public void PlayWakeUpAnimation()
    {
        StopAllCoroutines();

        bool wasGhostCatch = false;
        bool isMorning = false;

        if (GameManager.Instance != null)
        {
            wasGhostCatch = GameManager.Instance.wasCaughtByGhost;
            isMorning = (GameManager.Instance.currentState == GameState.Daytime);
        }

        if (wasGhostCatch)
        {
            GameManager.Instance.wasCaughtByGhost = false;
            StartCoroutine(GaspWakeUpRoutine());
        }
        else if (isMorning)
        {
            StartCoroutine(MorningWakeUpRoutine());
        }
        else
        {
            StartCoroutine(NightWakeUpRoutine());
        }
    }

    /// <summary>
    /// 😱 อนิเมชันสะดุ้งตื่นลุกจากเตียง (เมื่อโดนผีจับได้)
    /// </summary>
    private IEnumerator GaspWakeUpRoutine()
    {
        isWakingUp = true;

        if (playerCamera != null)
        {
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;
        }

        if (audioSource != null)
        {
            if (gaspStartleSound != null) audioSource.PlayOneShot(gaspStartleSound);
            else if (wakeUpSound != null) audioSource.PlayOneShot(wakeUpSound);
        }

        if (ScreenFader.Instance != null) ScreenFader.Instance.SetBlackInstant();

        Vector3 startPos = originalCamLocalPos + lyingPosOffset;
        Quaternion startRot = originalCamLocalRot * Quaternion.Euler(lyingRotationOffset);

        if (playerCamera != null)
        {
            playerCamera.localPosition = startPos;
            playerCamera.localRotation = startRot;
        }

        yield return new WaitForSeconds(0.15f);

        // ย้ายกล้องขนานไปกับการ Fade จอดำ
        float timer = 0f;
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToClear(gaspWakeUpDuration);
        }

        while (timer < gaspWakeUpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / gaspWakeUpDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (playerCamera != null)
            {
                playerCamera.localPosition = Vector3.Lerp(startPos, originalCamLocalPos, smoothProgress);
                playerCamera.localRotation = Quaternion.Slerp(startRot, originalCamLocalRot, smoothProgress);
            }

            yield return null;
        }

        if (playerCamera != null)
        {
            playerCamera.localPosition = originalCamLocalPos;
            playerCamera.localRotation = originalCamLocalRot;
        }

        isWakingUp = false;
    }

    /// <summary>
    /// ☀️ อนิเมชันลุกจากเตียงแบบปกติ (ตอนเช้า)
    /// </summary>
    private IEnumerator MorningWakeUpRoutine()
    {
        isWakingUp = true;

        if (playerCamera != null)
        {
            originalCamLocalPos = playerCamera.localPosition;
            originalCamLocalRot = playerCamera.localRotation;
        }

        if (audioSource != null && wakeUpSound != null)
        {
            audioSource.PlayOneShot(wakeUpSound);
        }

        if (ScreenFader.Instance != null) ScreenFader.Instance.SetBlackInstant();

        Vector3 startPos = originalCamLocalPos + lyingPosOffset;
        Quaternion startRot = originalCamLocalRot * Quaternion.Euler(lyingRotationOffset);

        if (playerCamera != null)
        {
            playerCamera.localPosition = startPos;
            playerCamera.localRotation = startRot;
        }

        yield return new WaitForSeconds(0.3f);

        float timer = 0f;
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToClear(wakeUpDuration);
        }

        while (timer < wakeUpDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / wakeUpDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            if (playerCamera != null)
            {
                playerCamera.localPosition = Vector3.Lerp(startPos, originalCamLocalPos, smoothProgress);
                playerCamera.localRotation = Quaternion.Slerp(startRot, originalCamLocalRot, smoothProgress);
            }

            yield return null;
        }

        if (playerCamera != null)
        {
            playerCamera.localPosition = originalCamLocalPos;
            playerCamera.localRotation = originalCamLocalRot;
        }

        isWakingUp = false;
    }

    /// <summary>
    /// 🌙 ย้ายฉากกลางคืนปกติ / โหลดเกม
    /// </summary>
    private IEnumerator NightWakeUpRoutine()
    {
        isWakingUp = true;

        if (ScreenFader.Instance != null) ScreenFader.Instance.SetBlackInstant();

        yield return new WaitForSeconds(0.3f);

        if (ScreenFader.Instance != null)
        {
            yield return ScreenFader.Instance.FadeToClear(nightFadeDuration);
        }

        isWakingUp = false;
    }
}