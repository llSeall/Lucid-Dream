using System.Collections.Generic;
using UnityEngine;
//using System.Collections.Empty; // ซ่อนไว้เผื่อใช้ระบบอื่น

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    [System.Serializable]
    public struct SpecialRoomConfig
    {
        [Tooltip("วันที่จะให้ห้องนี้โผล่")]
        public int targetDay;
        [Tooltip("Prefab ห้องพิเศษของวันนั้นๆ (ต้องมี RoomProperty แปะอยู่ด้วย)")]
        public RoomData specialRoom;
    }

    [Header("🎯 Optimization (กรวยสายตาผู้เล่น)")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private float safeDistance = 15f;
    [SerializeField] private float maxViewDistance = 100f;
    [SerializeField] private float viewAngle = 110f;

    [Header("📅 Day & Special Room Database")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private List<SpecialRoomConfig> specialRoomsDatabase = new List<SpecialRoomConfig>();

    [Header("Seed Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private string mapSeed = "MyScaryDungeon";

    [Header("🌿 3-Way Split Settings")]
    [SerializeField] private List<RoomData> threeWayRoomsPool = new List<RoomData>();
    [Range(0, 100)][SerializeField] private int threeWayBranchChance = 20;
    [SerializeField] private int deadEndBranchLength = 1;

    [Header("🏢 Large Room Settings")]
    [SerializeField] private List<RoomData> largeRoomsPool = new List<RoomData>();
    [Range(0, 100)][SerializeField] private int largeRoomChance = 15;
    [Range(0, 10)][SerializeField] private int maxLargeRoomsPerNight = 2;

    [Header("Special Room Configs")]
    [SerializeField] private RoomData startRoomData;
    [SerializeField] private List<RoomData> straightCorridorsPool;
    [SerializeField] private List<RoomData> cornerCorridorsPool;

    [Header("Room Databases")]
    [SerializeField] private List<RoomData> normalRoomsPool = new List<RoomData>();
    [SerializeField] private List<RoomData> deadEndRoomsPool = new List<RoomData>();

    [Header("Generation Settings")]
    [SerializeField] private int totalMainRooms = 6;
    [Range(0, 100)][SerializeField] private int longCorridorChance = 40;

    // 🛡️ [แก้ข้อ 3] เพิ่ม LayerMask เพื่อให้สุ่มเช็คชนเฉพาะ Layer ขอบเขตแผนที่เท่านั้น
    [Header("🛠️ Layer Optimization")]
    [Tooltip("เลือก Layer ที่ใช้กับพวก Colliders ระบบ (เช่น ตั้งชื่อ Layer ว่า MapCollision)")]
    [SerializeField] private LayerMask mapCollisionLayer;

    private List<GameObject> spawnedRoomInstances = new List<GameObject>();
    private Coroutine generationCoroutine; // ตัวจำคิว Coroutine

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        GenerateBranchingMap();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) GenerateBranchingMap();
    }

    // ⏳ [แก้ข้อ 4] เปลี่ยนทางเข้าหลักให้ไปเรียก Coroutine แทน เพื่อไม่ให้หน้าจอเกมล็อกค้าง
    public void GenerateBranchingMap()
    {
        if (generationCoroutine != null) StopCoroutine(generationCoroutine);
        generationCoroutine = StartCoroutine(GenerateBranchingMapRoutine());
    }

    private System.Collections.IEnumerator GenerateBranchingMapRoutine()
    {
        int maxAttempts = 50;
        int currentAttempt = 0;
        bool isGenerationSuccessful = false;

        if (TimeManager.Instance != null) currentDay = TimeManager.Instance.currentDay;

        while (!isGenerationSuccessful && currentAttempt < maxAttempts)
        {
            currentAttempt++;

            if (useRandomSeed || currentAttempt > 1)
            {
                mapSeed = (System.DateTime.Now.Ticks + currentAttempt).ToString();
                if (mapSeed.Length > 10) mapSeed = mapSeed.Substring(mapSeed.Length - 10);
            }

            isGenerationSuccessful = TryGenerateMap();

            // ⏳ [แก้ข้อ 4] ถ้าสุ่มรอบนี้แล้วชนกันพัง (ไม่สำเร็จ) ให้คืนเฟรมให้ Unity ไปวาดหน้าจอ 1 เฟรม 
            // แล้วค่อยมาสุ่มใหม่ในเฟรมถัดไป หน้าจอเกมจะไม่ค้าง (Freeze) อีกต่อไป
            if (!isGenerationSuccessful)
            {
                yield return null;
            }
        }

        if (isGenerationSuccessful)
        {
            Debug.Log($"<color=lime><b>✅ [LevelGen] สร้างสำเร็จในรอบที่ {currentAttempt} (Seed ที่ใช้จริง: {mapSeed})</b></color>");
        }
        else
        {
            foreach (GameObject room in spawnedRoomInstances) { if (room != null) DestroyImmediate(room); }
            spawnedRoomInstances.Clear();
            Debug.LogError($"❌ [LevelGen] ไม่สามารถสร้างด่านได้หลังพยายาม {maxAttempts} รอบ ระบบสั่งล้างวัตถุทั้งหมด");
        }
    }

    private bool TryGenerateMap()
    {
        foreach (GameObject room in spawnedRoomInstances) { if (room != null) DestroyImmediate(room); }
        spawnedRoomInstances.Clear();

        Random.InitState(mapSeed.GetHashCode());

        int midPointIndex = totalMainRooms / 2;
        bool hasSpawnedSpecialRoom = false;
        int spawnedLargeRoomsCount = 0;

        RoomData activeSpecialRoomForToday = null;
        foreach (SpecialRoomConfig config in specialRoomsDatabase)
        {
            if (config.targetDay == currentDay) { activeSpecialRoomForToday = config.specialRoom; break; }
        }

        GameObject currentMainRoom = Instantiate(startRoomData.roomPrefab, Vector3.zero, Quaternion.identity, transform);
        AttachOptimizationToRoom(currentMainRoom);
        spawnedRoomInstances.Add(currentMainRoom);

        for (int i = 0; i < totalMainRooms; i++)
        {
            // 🚪 [แก้ข้อ 1] เปลี่ยนมาดึงค่าจาก RoomProperty โดยตรง ไม่ต้องวนลูปหา String ชื่อ "ExitPoint" แล้ว
            RoomProperty currentRoomProp = currentMainRoom.GetComponent<RoomProperty>();
            if (currentRoomProp == null || currentRoomProp.exitPoints.Count == 0) return false;

            List<Transform> availableExits = currentRoomProp.exitPoints;
            int mainExitIndex = Random.Range(0, availableExits.Count);
            Transform mainExit = availableExits[mainExitIndex];

            for (int j = 0; j < availableExits.Count; j++)
            {
                if (j == mainExitIndex) continue;
                if (deadEndRoomsPool.Count > 0)
                {
                    GameObject sideDeadEnd = TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, availableExits[j], currentMainRoom);
                    if (sideDeadEnd == null) continue;
                }
            }

            // ⭐ ทางเลือกที่ 1: ห้องพิเศษประจำวัน
            if (activeSpecialRoomForToday != null && i == midPointIndex && !hasSpawnedSpecialRoom)
            {
                GameObject specialRoom = TrySpawnRoomSecure(activeSpecialRoomForToday.roomPrefab, mainExit, currentMainRoom);
                if (specialRoom == null) return false;

                currentMainRoom = specialRoom;
                hasSpawnedSpecialRoom = true;

                RoomProperty specialProp = specialRoom.GetComponent<RoomProperty>();
                if (specialProp == null || specialProp.exitPoints.Count == 0)
                {
                    Debug.Log($"<color=cyan>ℹ️ [LevelGen] ห้องพิเศษเป็นทางตัน จบการสร้างเส้นทางหลัก</color>");
                    break;
                }
                continue;
            }

            // 🌿 ทางเลือกที่ 2: สุ่มสร้างทางแยก 3 ทาง
            int threeWayRoll = Random.Range(0, 100);
            if (threeWayRoll < threeWayBranchChance && threeWayRoomsPool.Count > 0 && i < totalMainRooms - 1)
            {
                GameObject threeWayRoom = TrySpawnRoomSecure(threeWayRoomsPool[Random.Range(0, threeWayRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                bool threeWayBuildSuccess = false;
                GameObject nextRoomFor3Way = null;
                List<GameObject> temporaryBranchRooms = new List<GameObject>();

                if (threeWayRoom != null)
                {
                    RoomProperty threeWayProp = threeWayRoom.GetComponent<RoomProperty>();
                    if (threeWayProp != null && threeWayProp.exitPoints.Count >= 2)
                    {
                        int deadEndExitIndex = Random.Range(0, threeWayProp.exitPoints.Count);
                        int mainExitIndexFor3Way = (deadEndExitIndex == 0) ? 1 : 0;

                        Transform deadEndBranchExit = threeWayProp.exitPoints[deadEndExitIndex];
                        Transform nextMainExit = threeWayProp.exitPoints[mainExitIndexFor3Way];

                        GameObject currentDeadEndBranchRoom = threeWayRoom;
                        Transform currentSubExit = deadEndBranchExit;
                        bool branchRouteSuccess = true;

                        for (int k = 0; k < deadEndBranchLength; k++)
                        {
                            if (straightCorridorsPool.Count > 0)
                            {
                                GameObject branchStraight = TrySpawnRoomSecure(straightCorridorsPool[Random.Range(0, straightCorridorsPool.Count)].roomPrefab, currentSubExit, currentDeadEndBranchRoom);
                                if (branchStraight == null) { branchRouteSuccess = false; break; }

                                temporaryBranchRooms.Add(branchStraight);
                                currentDeadEndBranchRoom = branchStraight;

                                RoomProperty subProp = currentDeadEndBranchRoom.GetComponent<RoomProperty>();
                                if (subProp != null && subProp.exitPoints.Count > 0) currentSubExit = subProp.exitPoints[0];
                                else { branchRouteSuccess = false; break; }
                            }
                        }

                        if (branchRouteSuccess && deadEndRoomsPool.Count > 0)
                        {
                            GameObject branchDeadEnd = TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, currentSubExit, currentDeadEndBranchRoom);
                            if (branchDeadEnd == null) branchRouteSuccess = false;
                            else temporaryBranchRooms.Add(branchDeadEnd);
                        }

                        if (branchRouteSuccess)
                        {
                            nextRoomFor3Way = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, nextMainExit, threeWayRoom);
                            if (nextRoomFor3Way == null) branchRouteSuccess = false;
                        }

                        if (branchRouteSuccess)
                        {
                            threeWayBuildSuccess = true;
                            currentMainRoom = nextRoomFor3Way;
                        }
                    }

                    if (!threeWayBuildSuccess)
                    {
                        foreach (GameObject tmp in temporaryBranchRooms) { if (tmp != null) { spawnedRoomInstances.Remove(tmp); DestroyImmediate(tmp); } }
                        if (nextRoomFor3Way != null) { spawnedRoomInstances.Remove(nextRoomFor3Way); DestroyImmediate(nextRoomFor3Way); }
                        spawnedRoomInstances.Remove(threeWayRoom);
                        DestroyImmediate(threeWayRoom);
                        threeWayRoom = null;
                    }
                }

                if (threeWayRoom == null)
                {
                    GameObject fallbackRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                    if (fallbackRoom == null) return false;
                    currentMainRoom = fallbackRoom;
                }
                continue;
            }

            // ⚡ ทางเลือกที่ 3: สุ่มสร้างชุดทางเดินยาว
            int diceRoll = Random.Range(0, 100);
            if (diceRoll < longCorridorChance && straightCorridorsPool.Count > 0 && cornerCorridorsPool.Count > 0 && i < totalMainRooms - 1)
            {
                bool longCorridorSuccess = false;
                GameObject straightRoom = TrySpawnRoomSecure(straightCorridorsPool[Random.Range(0, straightCorridorsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                GameObject cornerRoom = null;
                GameObject nextRoomForLong = null;

                if (straightRoom != null)
                {
                    RoomProperty straightProp = straightRoom.GetComponent<RoomProperty>();
                    if (straightProp != null && straightProp.exitPoints.Count > 0)
                    {
                        cornerRoom = TrySpawnRoomSecure(cornerCorridorsPool[Random.Range(0, cornerCorridorsPool.Count)].roomPrefab, straightProp.exitPoints[0], straightRoom);
                        if (cornerRoom != null)
                        {
                            RoomProperty cornerProp = cornerRoom.GetComponent<RoomProperty>();
                            if (cornerProp != null && cornerProp.exitPoints.Count > 0)
                            {
                                nextRoomForLong = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, cornerProp.exitPoints[0], cornerRoom);
                                if (nextRoomForLong != null)
                                {
                                    longCorridorSuccess = true;
                                    currentMainRoom = nextRoomForLong;
                                }
                            }
                        }
                    }

                    if (!longCorridorSuccess)
                    {
                        if (nextRoomForLong != null) { spawnedRoomInstances.Remove(nextRoomForLong); DestroyImmediate(nextRoomForLong); }
                        if (cornerRoom != null) { spawnedRoomInstances.Remove(cornerRoom); DestroyImmediate(cornerRoom); }
                        if (straightRoom != null) { spawnedRoomInstances.Remove(straightRoom); DestroyImmediate(straightRoom); }
                        straightRoom = null;
                    }
                }

                if (straightRoom == null)
                {
                    GameObject fallbackRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                    if (fallbackRoom == null) return false;
                    currentMainRoom = fallbackRoom;
                }
                continue;
            }

            // 🏠 ทางเลือกที่ 4: สร้างห้องปกติทั่วไป + โดนคุมโควตาห้องใหญ่
            if (i < totalMainRooms - 1)
            {
                GameObject nextRoom = null;
                int largeRoomRoll = Random.Range(0, 100);

                if (largeRoomRoll < largeRoomChance && largeRoomsPool.Count > 0 && spawnedLargeRoomsCount < maxLargeRoomsPerNight)
                {
                    nextRoom = TrySpawnRoomSecure(largeRoomsPool[Random.Range(0, largeRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                    if (nextRoom != null) spawnedLargeRoomsCount++;
                    else if (normalRoomsPool.Count > 0)
                    {
                        nextRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                    }
                }
                else
                {
                    nextRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                }

                if (nextRoom == null) return false;
                currentMainRoom = nextRoom;
            }
            else
            {
                GameObject finalRoom = TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                if (finalRoom == null) return false;
            }
        }

        if (activeSpecialRoomForToday != null && !hasSpawnedSpecialRoom) return false;

        return true;
    }

    private GameObject TrySpawnRoomSecure(GameObject roomPrefab, Transform targetExit, GameObject parentRoom)
    {
        if (roomPrefab == null) return null;

        GameObject newRoom = Instantiate(roomPrefab, transform);

        // 🚪 [แก้ข้อ 1] เรียกดึงข้อมูลตำแหน่งตรงๆ จากสคริปต์ประจำห้อง ไม่ค้นหาด้วยชื่อ String แล้ว
        RoomProperty roomProp = newRoom.GetComponent<RoomProperty>();
        if (roomProp == null || roomProp.entrancePoint == null)
        {
            Debug.LogError($"❌ [LevelGen] Prefab '{roomPrefab.name}' ลืมใส่คอมโพเนนต์ 'RoomProperty' หรือลืมผูกมัด EntrancePoint!");
            DestroyImmediate(newRoom);
            return null;
        }

        Transform entrance = roomProp.entrancePoint;

        if (targetExit != null)
        {
            newRoom.transform.rotation = targetExit.rotation * Quaternion.Inverse(entrance.localRotation);
            Vector3 gap = targetExit.position - entrance.position;
            newRoom.transform.position += gap;
        }

        Physics.SyncTransforms();

        if (IsOverlappingWithOthers(newRoom, parentRoom))
        {
            DestroyImmediate(newRoom);
            return null;
        }

        AttachOptimizationToRoom(newRoom);
        spawnedRoomInstances.Add(newRoom);
        return newRoom;
    }

    private void AttachOptimizationToRoom(GameObject roomInstance)
    {
        if (playerTransform == null) return;
        RoomVisibility optimization = roomInstance.GetComponent<RoomVisibility>();
        if (optimization == null) optimization = roomInstance.AddComponent<RoomVisibility>();
        optimization.SetupOptimization(playerTransform, safeDistance, maxViewDistance, viewAngle);
    }

    // 🛡️ [แก้ข้อ 3] ฟังก์ชันตรวจเช็คการซ้อนทับ ปรับให้ดึงข้อมูลเฉพาะโฟลเดอร์ Colliders และใช้ Layer บังคับ
    private bool IsOverlappingWithOthers(GameObject targetRoom, GameObject parentRoom)
    {
        RoomProperty targetProp = targetRoom.GetComponent<RoomProperty>();

        // ความปลอดภัย: ถ้าห้องนั้นไม่มีโฟลเดอร์คอลไลเดอร์ ให้ข้ามไปเลยเพื่อไม่ให้เอเรอร์
        if (targetProp == null || targetProp.collidersFolder == null) return false;

        // ✨ ดึงเฉพาะคอลไลเดอร์ที่อยู่ภายในโฟลเดอร์ "Colliders" เท่านั้น (ข้ามโฟลเดอร์ Graphics ไปโดยสิ้นเชิง!)
        Collider[] systemColliders = targetProp.collidersFolder.GetComponentsInChildren<Collider>();
        if (systemColliders.Length == 0) return false;

        foreach (Collider col in systemColliders)
        {
            if (col.isTrigger) continue; // ไม่ตรวจเช็คพื้นที่ที่เป็น Trigger ระบบ

            Vector3 center;
            Vector3 halfExtents;
            Quaternion rotation = col.transform.rotation;

            if (col is BoxCollider)
            {
                BoxCollider box = (BoxCollider)col;
                center = box.transform.TransformPoint(box.center);

                Vector3 lossyScale = box.transform.lossyScale;
                halfExtents = new Vector3(
                    Mathf.Abs(box.size.x * lossyScale.x),
                    Mathf.Abs(box.size.y * lossyScale.y),
                    Mathf.Abs(box.size.z * lossyScale.z)
                ) * 0.5f * 0.95f;
            }
            else
            {
                center = col.bounds.center;
                halfExtents = col.bounds.extents * 0.95f;
            }

            // ✨ เพิ่มการกรองข้อมูลด้วย mapCollisionLayer ทำให้กล่องทำนายการชนจะไม่สนใจของตกแต่งใดๆ จากห้องข้างเคียงเด็ดขาด
            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, rotation, mapCollisionLayer);

            foreach (Collider hit in hitColliders)
            {
                if (hit.transform == targetRoom.transform || hit.transform.IsChildOf(targetRoom.transform)) continue;
                if (parentRoom != null && (hit.transform == parentRoom.transform || hit.transform.IsChildOf(parentRoom.transform))) continue;

                return true; // ชนกับขอบเขตของห้องอื่นจริงๆ
            }
        }
        return false;
    }

    public void SetCurrentDay(int day)
    {
        currentDay = day;
    }
}