using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NavMeshAgent))]
public class ZoneGhostAI : MonoBehaviour
{
    [Header("🏃 Speed & Movement Mode (ช่องเลือกเดิน/วิ่ง) ✨")]
    [Tooltip("ติ๊กถูก = ผีจะวิ่งเข้าหาผู้เล่น | เอาออก = ผีจะเดินเข้าหาผู้เล่น")]
    public bool isRunner = false;
    [Tooltip("ความเร็วในการเดินปกติ")]
    public float walkSpeed = 2.5f;
    [Tooltip("ความเร็วในการวิ่ง")]
    public float runSpeed = 5.5f;

    [Header("🎯 Target & Detection Settings")]
    public Transform playerTransform;
    public Camera playerCamera;
    [Tooltip("LayerMask ของสิ่งกีดขวาง (เช่น Wall, Obstacle) เพื่อใช้ Raycast ตรวจว่ามีอะไรบังหน้าผีไหม")]
    public LayerMask obstacleMask;
    [Tooltip("ระยะจับผู้เล่นเพื่อเริ่มเล่น Jumpscare")]
    public float catchDistance = 1.5f;

    [Header("2.5D Sprite & Animation")]
    public SpriteRenderer ghostSprite;
    public Animator ghostAnimator;
    public string speedParamName = "Speed";
    public string isMovingParamName = "IsMoving";
    public bool defaultFacingLeft = false;

    [Header("😱 Jumpscare Settings ✨")]
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

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
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

        // ตรวจสอบพฤติกรรมและการเคลื่อนที่
        HandleGhostBehavior();

        // อัปเดตอนิเมชันและการกลับด้าน Sprite 2.5D
        UpdateSpriteFacingAndAnimation();

        // เล่นเสียงเท้าขณะเคลื่อนที่
        HandleFootsteps();
    }
    private void HandleGhostBehavior()
    {
        // 1. ถ้าผู้เล่นไม่ได้อยู่ในโซน ให้ยืนนิ่งๆ
        if (!isPlayerInZone)
        {
            StopGhost();
            return;
        }

        // 2. เช็กสถานะการหลับตาและการมองเห็น
        bool isEyesClosed = (eyeManager != null) && eyeManager.IsEyesClosed;
        bool isPlayerLookingAtGhost = IsPlayerLookingAtGhost();

        // ✨ เงื่อนไขใหม่: ต้อง "หลับตา" AND "หันหลัง/ไม่ได้จ้องผี" พร้อมกันทั้ง 2 อย่างเท่านั้น!
        bool shouldMove = isEyesClosed && !isPlayerLookingAtGhost;

        if (shouldMove)
        {
            // ผีพุ่งเข้าหาผู้เล่น
            agent.isStopped = false;
            agent.speed = isRunner ? runSpeed : walkSpeed;
            agent.SetDestination(playerTransform.position);

            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= catchDistance)
            {
                TriggerPlayerCaught();
            }
        }
        else
        {
            // ทำอย่างใดอย่างหนึ่ง หรือไม่ทำเลย ➔ ยืนนิ่งค้างไว้
            StopGhost();
        }
    }

    private void StopGhost()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// เช็กว่าผีอยู่ในขอบเขตสายตากล้องผู้เล่น และไม่มีสิ่งกีดขวาง (Raycast) บังอยู่หรือไม่
    /// </summary>
    private bool IsPlayerLookingAtGhost()
    {
        if (playerCamera == null) return false;

        // จุดอ้างอิงลำตัวผี (ยกขึ้นเล็กน้อยจากพื้น)
        Vector3 ghostCenter = transform.position + Vector3.up * 1.0f;
        Vector3 viewportPos = playerCamera.WorldToViewportPoint(ghostCenter);

        // เช็ก 1: ตำแหน่งผีต้องอยู่ในขอบเขตหน้าจอผู้เล่น (Z > 0 คืออยู่ด้านหน้ากล้อง)
        bool inViewport = viewportPos.z > 0 &&
                          viewportPos.x >= 0f && viewportPos.x <= 1f &&
                          viewportPos.y >= 0f && viewportPos.y <= 1f;

        if (!inViewport) return false;

        // เช็ก 2: ยิง Raycast จากตาผู้เล่นไปยังตัวผีเพื่อดูว่ามีกำแพง/สิ่งของบังสายตาอยู่ไหม
        Vector3 dirToGhost = (ghostCenter - playerCamera.transform.position).normalized;
        float distToGhost = Vector3.Distance(playerCamera.transform.position, ghostCenter);

        if (Physics.Raycast(playerCamera.transform.position, dirToGhost, out RaycastHit hit, distToGhost, obstacleMask))
        {
            // ถ้าระหว่างทางยิงชน Layer สิ่งกีดขวาง ถือว่าผู้เล่นมองไม่เห็นผี
            return false;
        }

        return true;
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

        if (ghostAnimator != null)
        {
            if (!string.IsNullOrEmpty(speedParamName))
            {
                ghostAnimator.SetFloat(speedParamName, currentSpeed);
            }

            if (!string.IsNullOrEmpty(isMovingParamName))
            {
                ghostAnimator.SetBool(isMovingParamName, currentSpeed > 0.1f);
            }
        }
    }

    private void HandleFootsteps()
    {
        if (isPlayerCaught || agent == null || agent.isStopped) return;

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