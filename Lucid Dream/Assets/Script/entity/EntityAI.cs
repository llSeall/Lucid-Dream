using UnityEngine;
using UnityEngine.AI;

public enum EntityState { Chase, Investigate, Despawn }

[RequireComponent(typeof(NavMeshAgent))]
public class EntityAI : MonoBehaviour
{
    [Header("2.5D Sprite & Animation Settings ✨")]
    [Tooltip("ใส่ Sprite Renderer ของตัวผี")]
    public SpriteRenderer ghostSprite;
    [Tooltip("ใส่ Animator ที่อยู่ในออบเจกต์รูปผี")]
    public Animator ghostAnimator;
    [Tooltip("ชื่อ Parameter ประเภท Float ใน Animator (เช่น Speed)")]
    public string speedParamName = "Speed";
    [Tooltip("ชื่อ Parameter ประเภท Bool ใน Animator (เช่น IsMoving)")]
    public string isMovingParamName = "IsMoving";
    [Tooltip("ติ๊กถูกถ้ารูปต้นฉบับของคุณหันหน้าไปทางซ้าย")]
    public bool defaultFacingLeft = false;

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

    [Header("Current State")]
    public EntityState currentState = EntityState.Chase;

    private NavMeshAgent agent;
    private Vector3 lastKnownPosition;
    private float searchTimer;
    private bool hasReachedLastKnownPos = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // ถ้าลืมลาก Animator ใส่ระบบจะดึงจากออบเจกต์ลูกให้อัตโนมัติ
        if (ghostAnimator == null && ghostSprite != null)
        {
            ghostAnimator = ghostSprite.GetComponent<Animator>();
        }
    }

    void Update()
    {
        bool canSee = CanSeePlayer();

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
    }

    void UpdateSpriteFacingAndAnimation()
    {
        float currentSpeed = agent.velocity.magnitude;

        // --- พลิกสไปรต์ซ้าย-ขวา ---
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

        // --- ส่งค่าเข้า Animator เพื่อสั่งเล่นแอนิเมชัน ---
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
        if (isPlayerHiding) return false;

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

        if (canSee)
        {
            agent.SetDestination(playerTransform.position);
            lastKnownPosition = playerTransform.position;
        }
        else
        {
            currentState = EntityState.Investigate;
            hasReachedLastKnownPos = false;
            searchTimer = investigateDuration;
            agent.SetDestination(lastKnownPosition);
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
        Debug.Log("Entity lost the player and despawned.");
        Destroy(gameObject);
    }
}