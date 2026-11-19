using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
public class ZoneGhostAI : MonoBehaviour
{
    [Header("🏃 Speed & Movement Mode (ช่องเลือกเดิน/วิ่ง)")]
    [Tooltip("ติ๊กถูก = ผีจะวิ่งเข้าหาผู้เล่น | เอาออก = ผีจะเดินเข้าหาผู้เล่น")]
    public bool isRunner = false;
    [Tooltip("ความเร็วในการเดินปกติ")]
    public float walkSpeed = 2.5f;
    [Tooltip("ความเร็วในการวิ่ง")]
    public float runSpeed = 5.5f;

    [Header("🎯 Target & Catch Settings")]
    public Transform playerTransform;
    [Tooltip("ระยะจับผู้เล่นเพื่อเริ่มเล่น Jumpscare")]
    public float catchDistance = 1.5f;

    [Header("🎞️ 2.5D Sprite & Freeze-Frame Animation")]
    public SpriteRenderer ghostSprite;
    public Animator ghostAnimator;
    [Tooltip("ติ๊กถูกหากไฟล์ภาพ Sprite ต้นฉบับหันหน้าไปทางซ้าย")]
    public bool defaultFacingLeft = false;

    [Header("😱 Jumpscare Settings")]
    public Sprite[] jumpscareFrames;
    public float jumpscareFrameRate = 12f;
    public AudioClip jumpscareScreamClip;
    public float jumpscareDuration = 1.5f;

    [Header("🔊 Ghost Sound Settings")]
    public AudioSource ghostAudioSource;
    public AudioClip[] footstepClips;
    public float baseStepInterval = 0.5f;

    private NavMeshAgent agent;
    private EyeToggleWorldManager eyeManager;
    private bool isPlayerInZone = false;
    private bool isPlayerCaught = false;
    private float stepTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // ดูดตัวผีลงพื้น NavMesh อัตโนมัติหากวางลอยหรือเหลื่อมจากพื้นเล็กน้อย
        if (agent != null && !agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 3.0f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[ZoneGhostAI] {gameObject.name} อยู่นอกพื้นที่ NavMesh ไกลเกินไป กรุณาเช็กการ Bake NavMesh!");
            }
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        eyeManager = FindAnyObjectByType<EyeToggleWorldManager>();

        if (ghostAudioSource == null)
        {
            ghostAudioSource = GetComponent<AudioSource>();
        }

        if (ghostAnimator == null && ghostSprite != null)
        {
            ghostAnimator = ghostSprite.GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (isPlayerCaught || playerTransform == null) return;

        // 1. ตรวจสอบเงื่อนไขหลับตา/ลืมตาและการเคลื่อนที่
        HandleGhostBehavior();

        // 2. อัปเดตการแช่แข็งเฟรมอนิเมชันและการกลับด้าน Sprite 2.5D
        UpdateSpriteFacingAndAnimation();

        // 3. เล่นเสียงเท้าขณะเคลื่อนที่
        HandleFootsteps();
    }

    private void HandleGhostBehavior()
    {
        // ถ้าผู้เล่นไม่ได้อยู่ในโซน ให้ยืนนิ่งค้างไว้
        if (!isPlayerInZone)
        {
            StopGhost();
            return;
        }

        // เช็กสถานะการหลับตาเพียงอย่างเดียว (ไม่สนว่าผู้เล่นจะหันมองผีอยู่หรือไม่)
        bool isEyesClosed = (eyeManager != null) && eyeManager.IsEyesClosed;

        if (isEyesClosed)
        {
            // หลับตาอยู่ -> ผีเดิน/วิ่งเข้าหาผู้เล่นทันที
            if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.speed = isRunner ? runSpeed : walkSpeed;
                agent.SetDestination(playerTransform.position);
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= catchDistance)
            {
                TriggerPlayerCaught();
            }
        }
        else
        {
            // ลืมตาอยู่ -> ผีหยุดนิ่งค้างท่าทันที
            StopGhost();
        }
    }

    private void StopGhost()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// เรียกใช้จาก GhostZoneTrigger เมื่อผู้เล่นเข้าหรือออกจากโซน
    /// </summary>
    public void SetPlayerInZone(bool inZone)
    {
        isPlayerInZone = inZone;
        if (!inZone)
        {
            StopGhost();
        }
    }

    private void TriggerPlayerCaught()
    {
        if (isPlayerCaught) return;
        isPlayerCaught = true;

        StopGhost();

        Debug.Log("<color=red>😱 [ZoneGhostAI] โดนผีจับได้แล้ว! กำลังเริ่มเล่น Jumpscare...</color>");

        StartCoroutine(JumpscareAndRespawnRoutine());
    }

    private IEnumerator JumpscareAndRespawnRoutine()
    {
        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.ShowJumpscareAnimation(jumpscareFrames, jumpscareFrameRate, jumpscareScreamClip);
        }

        yield return new WaitForSeconds(jumpscareDuration);

        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.HideJumpscare();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDiedInDream();
        }
        else
        {
            if (StressManager.Instance != null)
            {
                StressManager.Instance.IncreaseStressOnDeath();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void UpdateSpriteFacingAndAnimation()
    {
        if (agent == null) return;
        float currentSpeed = agent.velocity.magnitude;

        // สลับด้านซ้าย-ขวาตามทิศทางการเดิน
        if (ghostSprite != null)
        {
            if (agent.velocity.x > 0.1f)
            {
                ghostSprite.flipX = defaultFacingLeft;
            }
            else if (agent.velocity.x < -0.1f)
            {
                ghostSprite.flipX = !defaultFacingLeft;
            }
        }

        // วิธีที่ 1: กำลังเดิน = เล่นอนิเมชันปกติ (1) | หยุดเดิน = แช่แข็งเฟรมค้างท่าไว้ (0)
        if (ghostAnimator != null)
        {
            ghostAnimator.speed = (currentSpeed > 0.1f) ? 1f : 0f;
        }
    }

    private void HandleFootsteps()
    {
        if (isPlayerCaught || agent == null || !agent.isActiveAndEnabled || !agent.isOnNavMesh || agent.isStopped) return;

        float currentSpeed = agent.velocity.magnitude;
        if (currentSpeed < 0.1f)
        {
            stepTimer = 0f;
            return;
        }

        float currentInterval = isRunner ? (baseStepInterval * 0.5f) : baseStepInterval;
        stepTimer += Time.deltaTime * (currentSpeed / walkSpeed);

        if (stepTimer >= currentInterval)
        {
            PlayFootstepSound();
            stepTimer = 0f;
        }
    }

    private void PlayFootstepSound()
    {
        if (ghostAudioSource == null || footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip != null)
        {
            ghostAudioSource.pitch = Random.Range(0.85f, 1.15f);
            ghostAudioSource.PlayOneShot(clip, 1.0f);
        }
    }
}