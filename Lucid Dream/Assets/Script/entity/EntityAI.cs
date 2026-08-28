using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

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

    [Header("Catch & Game Over Settings")]
    public float catchDistance = 1.5f;
    public GameObject gameOverUI;
    public KeyCode restartKey = KeyCode.R;

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
    [Tooltip("ไฟล์เสียงเดินของผี (ใส่หลายๆ ไฟล์เพื่อสุ่มได้)")]
    public AudioClip[] footstepClips;
    [Tooltip("ระยะห่างจังหวะก้าวขาปกติ (วินาที)")]
    public float baseStepInterval = 0.5f;
    [Tooltip("ความดังตอนเดินสำรวจ")]
    public float volumeInvestigate = 0.5f;
    [Tooltip("ความดังตอนวิ่งไล่ล่าผู้เล่น")]
    public float volumeChase = 0.85f;

    [Header("Current State")]
    public EntityState currentState = EntityState.Chase;

    private NavMeshAgent agent;
    private Vector3 lastKnownPosition;
    private float searchTimer;
    private float chaseTimer;
    private bool hasReachedLastKnownPos = false;
    private EntityState previousState;
    private bool isGameOver = false;
    private float stepTimer = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        chaseTimer = maxChaseDuration;

        if (ghostAudioSource == null)
        {
            ghostAudioSource = GetComponent<AudioSource>();
        }

        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (gameOverUI == null)
        {
            gameOverUI = GameObject.FindWithTag("GameOverUI");
        }

        if (ghostAnimator == null && ghostSprite != null)
        {
            ghostAnimator = ghostSprite.GetComponent<Animator>();
        }

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
    }

    void Update()
    {
        if (isGameOver)
        {
            if (Input.GetKeyDown(restartKey))
            {
                RestartLevel();
            }
            return;
        }

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
    }

    #region ✨ Ghost Footstep Audio Logic
    void HandleFootsteps()
    {
        if (isGameOver || agent == null) return;

        float currentSpeed = agent.velocity.magnitude;

        // ถ้าผีหยุดนิ่ง ให้รีเซ็ตเวลา
        if (currentSpeed < 0.1f)
        {
            stepTimer = 0f;
            return;
        }

        // กำหนดความดังและจังหวะก้าวตามสถานะ AI
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
    #endregion

    void UpdateSpriteFacingAndAnimation()
    {
        float currentSpeed = agent.velocity.magnitude;

        if (ghostSprite != null)
        {
            if (agent.velocity.x > 0.1f)
            {
                ghostSprite.flipX = defaultFacingLeft ? true : false;
            }
            else if (agent.velocity.x < -0.1f)
            {
                ghostSprite.flipX = defaultFacingLeft ? false : true;
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
                TriggerGameOver();
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

    void TriggerGameOver()
    {
        isGameOver = true;
        agent.isStopped = true;

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        // แสดงและปลดล็อกเมาส์ให้สามารถกดปุ่มบน UI ได้
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 0f;
        Debug.Log("[Game Over] ผู้เล่นถูกผีจับได้แล้ว!");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
        Debug.Log("Entity chase timeout or lost player and despawned.");
        Destroy(gameObject);
    }
}