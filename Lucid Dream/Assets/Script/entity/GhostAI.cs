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
    [Tooltip("ระยะห่างขั้นต่ำจากผู้เล่นตอนผีสปอน/วาร์ปเกิดใหม่ (เพิ่มค่านี่เพื่อให้เกิดห่างผู้เล่นขึ้น)")]
    [SerializeField] private float minSpawnDistance = 25f;

    [Header("🧠 AI Status (Read Only)")]
    [SerializeField] private GhostState currentState = GhostState.Wandering;
    [SerializeField] private bool isGhostActive = false; // สถานะว่าผีเกิดแล้วหรือยัง

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

        // 1. ซ่อนผีและปิดการทำงานชั่วคราว
        SetGhostVisibility(false);

        // 2. เริ่มรันเวลานับถอยหลังก่อนเกิด
        StartCoroutine(SpawnTimerRoutine());
    }

    private void Update()
    {
        // ถ้าผียังไม่เกิด หรือไม่มีตัวผู้เล่น หรือกำลังวาร์ป ให้ข้าม Update ไปเลย
        if (!isGhostActive || playerTransform == null || isTeleporting) return;
        if (!agent.isActiveAndEnabled || !agent.isOnNavMesh) return;

        // 🔍 ระบบตรวจจับสายตาเรียลไทม์
        bool canSeePlayerNow = CanSeePlayer();

        // ⚙️ FSM: การสลับสถานะของ AI
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

    /// <summary>
    /// ⏱️ คอร์รูทีนนับถอยหลังตามระยะเวลาที่ตั้งไว้ก่อนสั่งเสกผีลงด่าน
    /// </summary>
    private IEnumerator SpawnTimerRoutine()
    {
        spawnCountdown = initialSpawnDelay;

        while (spawnCountdown > 0)
        {
            spawnCountdown -= Time.deltaTime;
            yield return null;
        }

        // เมื่อครบเวลา ให้สุ่มวาร์ปผีไปเกิดในห้องที่ห่างจากผู้เล่น
        yield return StartCoroutine(InitialSpawnRoutine());
    }

    /// <summary>
    /// 👻 ทำการสุ่มหาห้องที่อยู่ไกลผู้เล่นตามระยะ minSpawnDistance แล้วเปิดตัวผี
    /// </summary>
    private IEnumerator InitialSpawnRoutine()
    {
        Vector3 spawnPos = Vector3.zero;
        bool validSpotFound = false;
        int attempts = 0;

        while (!validSpotFound && attempts < 30)
        {
            attempts++;
            GameObject farRoom = GetFartherRoomFromPlayer();

            if (farRoom != null)
            {
                if (NavMesh.SamplePosition(farRoom.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    spawnPos = hit.position;
                    validSpotFound = true;
                }
            }
            yield return null;
        }

        if (validSpotFound)
        {
            agent.Warp(spawnPos);
        }

        // เปิดตัวผีให้มองเห็นและเริ่มทำงาน
        SetGhostVisibility(true);
        isGhostActive = true;

        ChangeState(GhostState.Wandering);
        Debug.Log($"<color=red>⚠️ [GhostAI] ถึงเวลาแล้ว! ผีสปอนขึ้นมาในด่านเรียบร้อยแล้วที่ตำแหน่ง: {spawnPos}</color>");
    }

    /// <summary>
    /// 👁️ สั่งเปิด/ปิด การมองเห็น และ Collider ของตัวผี
    /// </summary>
    private void SetGhostVisibility(bool visible)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = visible;

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders) c.enabled = visible;

        if (agent != null) agent.enabled = visible;
    }

    /// <summary>
    /// 📍 ฟังก์ชันคัดกรองหาห้องที่มีระยะห่างจากผู้เล่นมากกว่า minSpawnDistance
    /// </summary>
    private GameObject GetFartherRoomFromPlayer()
    {
        if (LevelGenerator.Instance == null || playerTransform == null) return null;

        List<GameObject> candidateRooms = new List<GameObject>();

        // ดึงห้องทั้งหมดมาเช็คระยะ
        var roomsField = typeof(LevelGenerator).GetField("spawnedRoomInstances", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (roomsField != null)
        {
            List<GameObject> allRooms = roomsField.GetValue(LevelGenerator.Instance) as List<GameObject>;
            if (allRooms != null)
            {
                foreach (GameObject room in allRooms)
                {
                    if (room == null) continue;
                    float dist = Vector3.Distance(room.transform.position, playerTransform.position);

                    // เลือกเฉพาะห้องที่ห่างเกินระยะขั้นต่ำ
                    if (dist >= minSpawnDistance)
                    {
                        candidateRooms.Add(room);
                    }
                }
            }
        }

        if (candidateRooms.Count > 0)
        {
            return candidateRooms[Random.Range(0, candidateRooms.Count)];
        }

        // Fallback: ถ้าหาห้องที่ห่างมากๆ ไม่เจอ ให้ดึงห้องทั่วไปมาแทน
        return LevelGenerator.Instance.GetRandomSpawnedRoom();
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

        Vector3 newSpawnPosition = Vector3.zero;
        bool validPositionFound = false;
        int attempts = 0;

        while (!validPositionFound && attempts < 20)
        {
            attempts++;
            GameObject farRoom = GetFartherRoomFromPlayer();
            if (farRoom != null)
            {
                if (NavMesh.SamplePosition(farRoom.transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    newSpawnPosition = hit.position;
                    validPositionFound = true;
                }
            }
            yield return null;
        }

        if (validPositionFound)
        {
            agent.Warp(newSpawnPosition);
            ChangeState(GhostState.Wandering);
            Debug.Log($"<color=purple>👻 [GhostAI] วาร์ปหนี้ไปเกิดในห้องที่ห่างผู้เล่น: {newSpawnPosition}</color>");
        }

        if (agent.isOnNavMesh) agent.isStopped = false;
        isTeleporting = false;
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