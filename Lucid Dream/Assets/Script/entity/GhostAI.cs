using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum GhostState
{
    Wandering,  // เดินสุ่มตามห้อง
    Chasing,    // เห็นตัวแล้ว! วิ่งไล่ล่า
    Searching   // คลาดสายตา กำลังวิ่งไปดูพิกัดสุดท้ายที่เห็น
}

[RequireComponent(typeof(NavMeshAgent))]
public class GhostAI : MonoBehaviour
{
    [Header("⏱️ Spawn Timer Settings")]
    [Tooltip("ระยะเวลารอ (วินาที) ก่อนผีจะเกิดและเริ่มออกล่า เช่น 120 = 2 นาที")]
    [SerializeField] private float initialSpawnDelay = 120f;
    [Tooltip("เวลานับถอยหลังปัจจุบัน (แสดงใน Inspector ให้ดูง่ายๆ ตอนเทส)")]
    [SerializeField] private float spawnCountdown = 0f;

    [Header("👁️ Perception / Vision Settings")]
    [Tooltip("ระยะที่ผีสามารถมองเห็นผู้เล่นได้")]
    [SerializeField] private float detectionRadius = 12f;
    [Tooltip("องศากรอบสายตาของผี (เช่น 120 องศาด้านหน้า)")]
    [Range(0, 360)]
    [SerializeField] private float fieldOfViewAngle = 120f;
    [Tooltip("Layer ของกำแพง/สิ่งกีดขวางที่ใช้บังสายตาผี")]
    [SerializeField] private LayerMask obstacleMask;

    [Header("🎯 Target & Camera")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Camera playerCamera;

    [Header("⚙️ Despawn & Teleport Settings")]
    [Tooltip("ระยะห่างระหว่างผีกับผู้เล่นที่ผีจะเริ่มเช็คเพื่อการหายตัว")]
    [SerializeField] private float despawnDistance = 22f;

    // ✨ [ปรับเพิ่ม Inspector] ควบคุมระยะการสุ่มเกิดทั่วด่าน
    [Tooltip("ระยะห่างขั้นต่ำจากผู้เล่นตอนผีเกิดใหม่ (ผีจะไม่เกิดใกล้กว่าระยะนี้)")]
    [SerializeField] private float minSpawnDistance = 18f;
    [Tooltip("ระยะห่างสูงสุดจากผู้เล่นในการสุ่มจุดเกิดใหม่")]
    [SerializeField] private float maxSpawnDistance = 50f;

    [Header("🧠 AI Status (Read Only)")]
    [SerializeField] private GhostState currentState = GhostState.Wandering;
    [SerializeField] private bool isGhostActive = false;

    private NavMeshAgent agent;
    private bool isTeleporting = false;
    private Vector3 lastKnownPlayerPosition;
    private float searchTimer = 0f;
    [SerializeField] private float searchDuration = 3f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        FindPlayerReferences();
        SetGhostVisibility(false);
        StartCoroutine(SpawnTimerRoutine());
    }

    private void Update()
    {
        if (!isGhostActive || playerTransform == null || isTeleporting) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        bool canSeePlayerNow = CanSeePlayer();

        switch (currentState)
        {
            case GhostState.Wandering:
                if (canSeePlayerNow)
                {
                    ChangeState(GhostState.Chasing);
                }
                else if (!agent.hasPath || agent.remainingDistance <= 0.8f)
                {
                    SetDestinationToRandomRoom();
                }
                break;

            case GhostState.Chasing:
                if (canSeePlayerNow)
                {
                    lastKnownPlayerPosition = playerTransform.position;
                    agent.SetDestination(playerTransform.position);
                }
                else
                {
                    ChangeState(GhostState.Searching);
                }
                break;

            case GhostState.Searching:
                if (canSeePlayerNow)
                {
                    ChangeState(GhostState.Chasing);
                }
                else
                {
                    agent.SetDestination(lastKnownPlayerPosition);

                    if (agent.remainingDistance <= 1f)
                    {
                        searchTimer += Time.deltaTime;
                        if (searchTimer >= searchDuration)
                        {
                            ChangeState(GhostState.Wandering);
                        }
                    }
                }
                break;
        }

        CheckDespawnCondition();
    }

    private IEnumerator SpawnTimerRoutine()
    {
        spawnCountdown = initialSpawnDelay;

        while (spawnCountdown > 0)
        {
            spawnCountdown -= Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(InitialSpawnRoutine());
    }

    private IEnumerator InitialSpawnRoutine()
    {
        Vector3 spawnPos = Vector3.zero;

        // ✨ ใช้ระบบสุ่มพิกัดบน NavMesh พื้นที่เดินได้จริงทั่วด่าน
        if (TryGetRandomNavMeshPointFarFromPlayer(out spawnPos))
        {
            agent.Warp(spawnPos);
        }
        else
        {
            // สำรอง: กรณีสุ่มไม่เจอ ให้ดึงห้องสุ่มทั่วไป
            GameObject fallbackRoom = GetRandomRoomFromLevel();
            if (fallbackRoom != null && NavMesh.SamplePosition(fallbackRoom.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                spawnPos = hit.position;
                agent.Warp(spawnPos);
            }
        }

        SetGhostVisibility(true);
        isGhostActive = true;

        ChangeState(GhostState.Wandering);
        Debug.Log($"<color=red>⚠️ [GhostAI] ถึงเวลาแล้ว! ผีสปอนขึ้นมาบน NavMesh ที่ตำแหน่ง: {spawnPos}</color>");
        yield break;
    }

    private void SetGhostVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = visible;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = visible;

        if (agent != null) agent.enabled = visible;
    }

    /// <summary>
    /// 🎲 สุ่มพิกัดบน NavMesh ทั่วแมพโดยกระจายตามห้อง/โถงเดิน และเช็กระยะห่างจากผู้เล่น
    /// </summary>
    private bool TryGetRandomNavMeshPointFarFromPlayer(out Vector3 resultPos)
    {
        resultPos = Vector3.zero;
        if (playerTransform == null) return false;

        int attempts = 0;
        int maxAttempts = 35;

        // 🔹 1. สุ่มกระจายบน NavMesh ตามตำแหน่งห้องและทางเดินที่มีในฉาก
        List<GameObject> allRooms = GetRoomsFromLevelGenerator();
        if (allRooms != null && allRooms.Count > 0)
        {
            while (attempts < maxAttempts)
            {
                attempts++;
                GameObject randomRoom = allRooms[Random.Range(0, allRooms.Count)];
                if (randomRoom == null) continue;

                // สุ่มกระจายรัศมีรอบห้องนั้นๆ (ไม่ใช่แค่จุดศูนย์กลาง)
                Vector3 randomOffset = Random.insideUnitSphere * 10f;
                randomOffset.y = 0;
                Vector3 checkPoint = randomRoom.transform.position + randomOffset;

                if (NavMesh.SamplePosition(checkPoint, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    float distToPlayer = Vector3.Distance(hit.position, playerTransform.position);
                    // ตรวจสอบว่าพิกัดที่ได้ ห่างจากผู้เล่นอยู่ในช่วง minSpawnDistance ถึง maxSpawnDistance
                    if (distToPlayer >= minSpawnDistance && distToPlayer <= maxSpawnDistance)
                    {
                        resultPos = hit.position;
                        return true;
                    }
                }
            }
        }

        // 🔹 2. Fallback: สุ่มเป็นวงแหวนรอบตัวผู้เล่นบน NavMesh
        attempts = 0;
        while (attempts < maxAttempts)
        {
            attempts++;
            Vector3 randomDir = Random.onUnitSphere;
            randomDir.y = 0;
            randomDir.Normalize();

            float randomDist = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 targetPoint = playerTransform.position + (randomDir * randomDist);

            if (NavMesh.SamplePosition(targetPoint, out NavMeshHit hit, 8f, NavMesh.AllAreas))
            {
                if (Vector3.Distance(hit.position, playerTransform.position) >= minSpawnDistance)
                {
                    resultPos = hit.position;
                    return true;
                }
            }
        }

        return false;
    }

    private List<GameObject> GetRoomsFromLevelGenerator()
    {
        if (LevelGenerator.Instance == null) return null;
        var roomsField = typeof(LevelGenerator).GetField("spawnedRoomInstances", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (roomsField != null)
        {
            return roomsField.GetValue(LevelGenerator.Instance) as List<GameObject>;
        }
        return null;
    }

    private bool CanSeePlayer()
    {
        if (playerTransform == null) return false;

        Vector3 dirToPlayer = (playerTransform.position - transform.position);
        float distanceToPlayer = dirToPlayer.magnitude;

        if (distanceToPlayer <= detectionRadius)
        {
            float angleToPlayer = Vector3.Angle(transform.forward, dirToPlayer.normalized);
            if (angleToPlayer <= fieldOfViewAngle / 2f)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * 1.5f;
                Vector3 targetPos = playerTransform.position + Vector3.up * 1.2f;
                Vector3 rayDir = (targetPos - rayOrigin).normalized;

                if (!Physics.Raycast(rayOrigin, rayDir, distanceToPlayer, obstacleMask))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ChangeState(GhostState newState)
    {
        currentState = newState;
        searchTimer = 0f;

        if (newState == GhostState.Wandering)
        {
            SetDestinationToRandomRoom();
        }
    }

    private void SetDestinationToRandomRoom()
    {
        GameObject randomRoom = GetRandomRoomFromLevel();
        if (randomRoom != null)
        {
            if (NavMesh.SamplePosition(randomRoom.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
    }

    private void FindPlayerReferences()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                playerCamera = player.GetComponentInChildren<Camera>();
            }
        }
    }

    private void CheckDespawnCondition()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);

        if (distanceToPlayer >= despawnDistance)
        {
            if (!IsPlayerLookingAtMe())
            {
                TeleportToRandomRoom();
            }
        }
    }

    private bool IsPlayerLookingAtMe()
    {
        if (playerCamera == null) return false;

        Vector3 screenPoint = playerCamera.WorldToViewportPoint(transform.position);
        bool inViewport = screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1;

        if (!inViewport) return false;

        Vector3 dirToGhost = transform.position - playerCamera.transform.position;
        float distanceToGhost = dirToGhost.magnitude;

        if (Physics.Raycast(playerCamera.transform.position, dirToGhost.normalized, out RaycastHit hit, distanceToGhost, obstacleMask))
        {
            if (!hit.transform.IsChildOf(transform))
            {
                return false;
            }
        }

        return true;
    }

    public void TeleportToRandomRoom()
    {
        if (LevelGenerator.Instance == null) return;
        StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        isTeleporting = true;
        if (agent.isOnNavMesh) agent.isStopped = true;

        // ✨ วาร์ปไปยังจุดบน NavMesh ที่สุ่มได้
        if (TryGetRandomNavMeshPointFarFromPlayer(out Vector3 newSpawnPosition))
        {
            agent.Warp(newSpawnPosition);
            ChangeState(GhostState.Wandering);
            Debug.Log($"<color=purple>👻 [GhostAI] วาร์ปหนีไปเกิดบน NavMesh ในระยะปลอดภัย: {newSpawnPosition}</color>");
        }
        else
        {
            GameObject fallbackRoom = GetRandomRoomFromLevel();
            if (fallbackRoom != null && NavMesh.SamplePosition(fallbackRoom.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                ChangeState(GhostState.Wandering);
            }
        }

        if (agent.isOnNavMesh) agent.isStopped = false;
        isTeleporting = false;
        yield break;
    }

    private GameObject GetRandomRoomFromLevel()
    {
        if (LevelGenerator.Instance != null)
        {
            return LevelGenerator.Instance.GetRandomSpawnedRoom();
        }
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Vector3 fovLine1 = Quaternion.AngleAxis(fieldOfViewAngle / 2, Vector3.up) * transform.forward * detectionRadius;
        Vector3 fovLine2 = Quaternion.AngleAxis(-fieldOfViewAngle / 2, Vector3.up) * transform.forward * detectionRadius;

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, fovLine1);
        Gizmos.DrawRay(transform.position, fovLine2);
    }
}