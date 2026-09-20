using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EntityState { Chase, Investigate, Despawn }

[RequireComponent(typeof(NavMeshAgent))]
public class EntityAI : MonoBehaviour
{
    [Header("2.5D Sprite & Animation Settings")]
    public SpriteRenderer ghostSprite;
    public Animator ghostAnimator;
    public string speedParamName = "Speed";
    public string isMovingParamName = "IsMoving";
    public bool defaultFacingLeft = false;

    [Header("Catch & Respawn Settings")]
    public float catchDistance = 1.5f;

    [Header("😱 Jumpscare Settings ✨")]
    [Tooltip("รูปภาพจั๊มสแกร์เฉพาะของผีตัวนี้")]
    public Sprite jumpscareSprite;
    [Tooltip("ไฟล์เสียงตกใจ/เสียงกรี๊ดเฉพาะของผีตัวนี้")]
    public AudioClip jumpscareScreamClip;
    [Tooltip("ระยะเวลาที่แสดงจั๊มสแกร์ (วินาที) ก่อนรีโหลดวนลูป")]
    public float jumpscareDuration = 1.5f;
    [Tooltip("ลากไฟล์ Sprite อนิเมชันจั๊มสแกร์ทุกเฟรมมาใส่ตรงนี้")]
    public Sprite[] jumpscareFrames;
    [Tooltip("ความเร็วอนิเมชัน (เช่น 12 หรือ 24 เฟรมต่อวินาที)")]
    public float jumpscareFrameRate = 12f;
    [Tooltip("ไฟล์เสียงตกใจ/เสียงกรี๊ดเฉพาะของผีตัวนี้")]

    [Header("Chase Timeout Settings")]
    public float maxChaseDuration = 15f;

    [Header("Target & Hiding Settings")]
    public Transform playerTransform;
    public LayerMask obstacleMask;
    public bool isPlayerHiding = false;

    [Header("AI Speeds")]
    public float chaseSpeed = 5.5f;
    public float investSpeed = 3.0f;
    public float sightDistance = 12f;

    [Header("Investigate Delay & Wander Settings")]
    public float investigateDuration = 5f;
    public float wanderRadius = 4f;

    [Header("Ghost Footstep Sounds ✨")]
    public AudioSource ghostAudioSource;
    public AudioClip[] footstepClips;
    public float baseStepInterval = 0.5f;
    [Range(0f, 2f)] public float volumeInvestigate = 0.8f;
    [Range(0f, 2f)] public float volumeChase = 1.5f;

    [Header("👻 Proximity Glitch/Static Effect Settings")]
    public float staticEffectMaxDistance = 15f;

    [Header("Current State")]
    public EntityState currentState = EntityState.Chase;

    private NavMeshAgent agent;
    private Vector3 lastKnownPosition;
    private float searchTimer;
    private float chaseTimer;
    private bool hasReachedLastKnownPos = false;
    private EntityState previousState;
    private bool isPlayerCaught = false;
    private float stepTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        chaseTimer = maxChaseDuration;

        if (ghostAudioSource == null)
        {
            ghostAudioSource = GetComponent<AudioSource>();
        }

        if (ghostAudioSource != null)
        {
            ghostAudioSource.spatialBlend = 1.0f;
            ghostAudioSource.minDistance = 2f;
            ghostAudioSource.maxDistance = 20f;
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (ghostAnimator == null && ghostSprite != null)
        {
            ghostAnimator = ghostSprite.GetComponent<Animator>();
        }
    }

    void Update()
    {
        if (isPlayerCaught) return;

        bool canSee = CanSeePlayer();

        if (currentState == EntityState.Chase && previousState != EntityState.Chase)
        {
            chaseTimer = maxChaseDuration;
        }
        previousState = currentState;

        switch (currentState)
        {
            case EntityState.Chase:
                HandleChase(canSee);
                break;
            case EntityState.Investigate:
                HandleInvestigate(canSee);
                break;
            case EntityState.Despawn:
                HandleDespawn();
                break;
        }

        UpdateSpriteFacingAndAnimation();
        HandleFootsteps();
        UpdateProximityStatic();
    }

    void UpdateProximityStatic()
    {
        if (playerTransform == null || GhostStaticEffectUI.Instance == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        if (distance <= staticEffectMaxDistance)
        {
            float intensity = 1f - Mathf.Clamp01(distance / staticEffectMaxDistance);
            GhostStaticEffectUI.Instance.ReportGhostDistance(intensity);
        }
    }

    void HandleFootsteps()
    {
        if (isPlayerCaught || agent == null) return;

        float currentSpeed = agent.velocity.magnitude;

        if (currentSpeed < 0.1f)
        {
            stepTimer = 0f;
            return;
        }

        float volume = (currentState == EntityState.Chase) ? volumeChase : volumeInvestigate;
        float currentInterval = (currentState == EntityState.Chase) ? (baseStepInterval * 0.65f) : baseStepInterval;

        stepTimer += Time.deltaTime * (currentSpeed / investSpeed);

        if (stepTimer >= currentInterval)
        {
            PlayFootstepSound(volume);
            stepTimer = 0f;
        }
    }

    void PlayFootstepSound(float volume)
    {
        if (ghostAudioSource == null || footstepClips == null || footstepClips.Length == 0) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip != null)
        {
            ghostAudioSource.pitch = Random.Range(0.85f, 1.15f);
            ghostAudioSource.PlayOneShot(clip, volume);
        }
    }

    void UpdateSpriteFacingAndAnimation()
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

    bool CanSeePlayer()
    {
        if (isPlayerHiding || playerTransform == null) return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position).normalized;
        float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distToPlayer <= sightDistance)
        {
            if (!Physics.Raycast(transform.position + Vector3.up, dirToPlayer, distToPlayer, obstacleMask))
            {
                return true;
            }
        }
        return false;
    }

    void HandleChase(bool canSee)
    {
        agent.speed = chaseSpeed;

        if (playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distanceToPlayer <= catchDistance)
            {
                TriggerPlayerCaught();
                return;
            }
        }

        if (canSee)
        {
            agent.SetDestination(playerTransform.position);
            lastKnownPosition = playerTransform.position;

            chaseTimer -= Time.deltaTime;
            if (chaseTimer <= 0f)
            {
                currentState = EntityState.Despawn;
                return;
            }
        }
        else
        {
            currentState = EntityState.Investigate;
            hasReachedLastKnownPos = false;
            searchTimer = investigateDuration;
            agent.SetDestination(lastKnownPosition);
        }
    }

    void TriggerPlayerCaught()
    {
        if (isPlayerCaught) return;
        isPlayerCaught = true;

        if (agent != null) agent.isStopped = true;

        if (GhostStaticEffectUI.Instance != null)
        {
            GhostStaticEffectUI.Instance.ReportGhostDistance(0f);
        }

        Debug.Log("<color=red>😱 [EntityAI] ผีจับผู้เล่นได้แล้ว! กำลังเริ่มเล่น Jumpscare...</color>");

        StartCoroutine(JumpscareAndRespawnRoutine());
    }

    /// <summary>
    /// ✨ Coroutine เรียกใช้งาน JumpscareUI ผ่าน Singleton
    /// </summary>
    private IEnumerator JumpscareAndRespawnRoutine()
    {
        // 1. ส่งชุดภาพอนิเมชัน + ความเร็วเฟรม ไปให้ JumpscareUI
        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.ShowJumpscareAnimation(jumpscareFrames, jumpscareFrameRate, jumpscareScreamClip);
        }

        // 2. รอเวลา Jumpscare ที่กำหนด
        yield return new WaitForSeconds(jumpscareDuration);

        // 3. ปิดหน้า Jumpscare
        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.HideJumpscare();
        }

        // 4. วนลูปฉากใหม่ / รีโหลด
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
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
    }

    void HandleInvestigate(bool canSee)
    {
        agent.speed = investSpeed;

        if (canSee)
        {
            currentState = EntityState.Chase;
            return;
        }

        if (!hasReachedLastKnownPos)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                hasReachedLastKnownPos = true;
                SetNextWanderDestination();
            }
        }
        else
        {
            searchTimer -= Time.deltaTime;

            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                SetNextWanderDestination();
            }

            if (searchTimer <= 0f)
            {
                currentState = EntityState.Despawn;
            }
        }
    }

    void SetNextWanderDestination()
    {
        Vector3 randomDirection = Random.insideUnitSphere * wanderRadius;
        randomDirection += lastKnownPosition;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, wanderRadius, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    void HandleDespawn()
    {
        if (GhostStaticEffectUI.Instance != null)
        {
            GhostStaticEffectUI.Instance.ReportGhostDistance(0f);
        }
        Debug.Log("Entity chase timeout or lost player and despawned.");
        Destroy(gameObject);
    }
}