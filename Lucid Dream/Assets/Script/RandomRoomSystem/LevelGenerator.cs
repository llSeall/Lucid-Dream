using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; } //

    [System.Serializable]
    public struct SpecialRoomConfig
    {
        [Tooltip("วันที่จะให้ห้องนี้โผล่")]
        public int targetDay; //
        [Tooltip("Prefab ห้องพิเศษของวันนั้นๆ")]
        public RoomData specialRoom; //
    }

    [Header("🎯 Optimization (กรวยสายตาผู้เล่น)")]
    [SerializeField] private Transform playerTransform; //[cite: 13]
    [SerializeField] private float safeDistance = 15f; //[cite: 13]
    [SerializeField] private float maxViewDistance = 100f; //[cite: 13]
    [SerializeField] private float viewAngle = 110f; //[cite: 13]

    [Header("📅 Day & Special Room Database")]
    [SerializeField] private int currentDay = 1; //[cite: 13]
    [SerializeField] private List<SpecialRoomConfig> specialRoomsDatabase = new List<SpecialRoomConfig>(); //[cite: 13]

    [Header("Seed Settings")]
    [SerializeField] private bool useRandomSeed = true; //[cite: 13]
    [SerializeField] private string mapSeed = "MyScaryDungeon"; //[cite: 13]

    [Header("🌿 3-Way Split Settings")]
    [SerializeField] private List<RoomData> threeWayRoomsPool = new List<RoomData>(); //[cite: 13]
    [Range(0, 100)][SerializeField] private int threeWayBranchChance = 20; //[cite: 13]
    [SerializeField] private int deadEndBranchLength = 1; //[cite: 13]

    [Header("🏢 Large Room Settings")]
    [SerializeField] private List<RoomData> largeRoomsPool = new List<RoomData>(); //[cite: 13]
    [Range(0, 100)][SerializeField] private int largeRoomChance = 15; //[cite: 13]
    [Range(0, 10)][SerializeField] private int maxLargeRoomsPerNight = 2; //[cite: 13]

    [Header("Special Room Configs")]
    [SerializeField] private RoomData startRoomData; //[cite: 13]
    [SerializeField] private List<RoomData> straightCorridorsPool; //[cite: 13]
    [SerializeField] private List<RoomData> cornerCorridorsPool; //[cite: 13]

    [Header("Room Databases")]
    [SerializeField] private List<RoomData> normalRoomsPool = new List<RoomData>(); //[cite: 13]
    [SerializeField] private List<RoomData> deadEndRoomsPool = new List<RoomData>(); //[cite: 13]

    [Header("Generation Settings")]
    [SerializeField] private int totalMainRooms = 6; //[cite: 13]
    [SerializeField] private int longCorridorChance = 40; //[cite: 13]

    [Header("🛠️ Layer Optimization")]
    [SerializeField] private LayerMask mapCollisionLayer; //[cite: 13]

    private List<GameObject> spawnedRoomInstances = new List<GameObject>(); //[cite: 13]
    private Coroutine generationCoroutine; //[cite: 13]

    private void Awake()
    {
        if (Instance == null) Instance = this; //[cite: 13]
        else Destroy(gameObject); //[cite: 13]
    }

    private void Start()
    {
        // 🛠️ [แก้ไขบิ๊ก] ลบการเรียก GenerateBranchingMap() ออกจากตรงนี้ 
        // เพื่อให้ SaveManager เป็นคนสั่งรันระบบด่านหลังจากโหลดข้อมูลเซฟเสร็จสิ้น
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) GenerateBranchingMap(); //[cite: 13]
    }

    // ✨ [เพิ่มใหม่] ฟังก์ชันส่งออกค่า Seed เพื่อให้ระบบเซฟบันทึกเก็บไว้[cite: 13, 15]
    public string GetMapSeed() => mapSeed;

    // ✨ [เพิ่มใหม่] ฟังก์ชันสร้างด่านผ่านไฟล์เซฟ ยับยั้งการสุ่ม Seed ใหม่มั่วซั่ว[cite: 13, 15]
    public void GenerateMapFromSave(string savedSeed)
    {
        if (!string.IsNullOrEmpty(savedSeed)) //[cite: 13, 15]
        {
            useRandomSeed = false; //[cite: 13]
            mapSeed = savedSeed; //[cite: 13]
        }
        else
        {
            useRandomSeed = true; //[cite: 13]
        }
        GenerateBranchingMap(); //[cite: 13]
    }

    public void GenerateBranchingMap()
    {
        if (generationCoroutine != null) StopCoroutine(generationCoroutine); //[cite: 13]
        generationCoroutine = StartCoroutine(GenerateBranchingMapRoutine()); //[cite: 13]
    }

    private System.Collections.IEnumerator GenerateBranchingMapRoutine()
    {
        int maxAttempts = 50; //[cite: 13]
        int currentAttempt = 0; //[cite: 13]
        bool isGenerationSuccessful = false; //[cite: 13]

        if (TimeManager.Instance != null) currentDay = TimeManager.Instance.currentDay; //[cite: 13, 16]

        while (!isGenerationSuccessful && currentAttempt < maxAttempts) //[cite: 13]
        {
            currentAttempt++; //[cite: 13]

            if (useRandomSeed || currentAttempt > 1) //[cite: 13]
            {
                mapSeed = (System.DateTime.Now.Ticks + currentAttempt).ToString(); //[cite: 13]
                if (mapSeed.Length > 10) mapSeed = mapSeed.Substring(mapSeed.Length - 10); //[cite: 13]
            }

            isGenerationSuccessful = TryGenerateMap(); //[cite: 13]

            if (!isGenerationSuccessful) //[cite: 13]
            {
                yield return null; // คืนเฟรมให้เกมลื่นไหล[cite: 13]
            }
        }

        if (isGenerationSuccessful) //[cite: 13]
        {
            Debug.Log($"<color=lime><b>✅ [LevelGen] สร้างสำเร็จในรอบที่ {currentAttempt} (Seed: {mapSeed})</b></color>"); //[cite: 13]
        }
        else
        {
            foreach (GameObject room in spawnedRoomInstances) { if (room != null) DestroyImmediate(room); } //[cite: 13]
            spawnedRoomInstances.Clear(); //[cite: 13]
            Debug.LogError($"❌ [LevelGen] ล้มเหลวหลังพยายามครบ {maxAttempts} รอบ"); //[cite: 13]
        }
    }

    private bool TryGenerateMap()
    {
        foreach (GameObject room in spawnedRoomInstances) { if (room != null) DestroyImmediate(room); } //[cite: 13]
        spawnedRoomInstances.Clear(); //[cite: 13]

        Random.InitState(mapSeed.GetHashCode()); //[cite: 13]

        int midPointIndex = totalMainRooms / 2; //[cite: 13]
        bool hasSpawnedSpecialRoom = false; //[cite: 13]
        int spawnedLargeRoomsCount = 0; //[cite: 13]

        RoomData activeSpecialRoomForToday = null; //[cite: 13]
        foreach (SpecialRoomConfig config in specialRoomsDatabase) //[cite: 13]
        {
            if (config.targetDay == currentDay) { activeSpecialRoomForToday = config.specialRoom; break; } //[cite: 13]
        }

        GameObject currentMainRoom = Instantiate(startRoomData.roomPrefab, Vector3.zero, Quaternion.identity, transform); //[cite: 13]
        AttachOptimizationToRoom(currentMainRoom); //[cite: 13]
        spawnedRoomInstances.Add(currentMainRoom); //[cite: 13]

        for (int i = 0; i < totalMainRooms; i++) //[cite: 13]
        {
            RoomProperty currentRoomProp = currentMainRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
            if (currentRoomProp == null || currentRoomProp.exitPoints.Count == 0) return false; //[cite: 11, 13]

            List<Transform> availableExits = currentRoomProp.exitPoints; //[cite: 11, 13]
            int mainExitIndex = Random.Range(0, availableExits.Count); //[cite: 13]
            Transform mainExit = availableExits[mainExitIndex]; //[cite: 13]

            for (int j = 0; j < availableExits.Count; j++) //[cite: 13]
            {
                if (j == mainExitIndex) continue; //[cite: 13]
                if (deadEndRoomsPool.Count > 0) //[cite: 13]
                {
                    GameObject sideDeadEnd = TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, availableExits[j], currentMainRoom); //[cite: 13]
                    if (sideDeadEnd == null) continue; //[cite: 13]
                }
            }

            if (activeSpecialRoomForToday != null && i == midPointIndex && !hasSpawnedSpecialRoom) //[cite: 13]
            {
                GameObject specialRoom = TrySpawnRoomSecure(activeSpecialRoomForToday.roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                if (specialRoom == null) return false; //[cite: 13]

                currentMainRoom = specialRoom; //[cite: 13]
                hasSpawnedSpecialRoom = true; //[cite: 13]

                RoomProperty specialProp = specialRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
                if (specialProp == null || specialProp.exitPoints.Count == 0) //[cite: 11, 13]
                {
                    break; //[cite: 13]
                }
                continue; //[cite: 13]
            }

            int threeWayRoll = Random.Range(0, 100); //[cite: 13]
            if (threeWayRoll < threeWayBranchChance && threeWayRoomsPool.Count > 0 && i < totalMainRooms - 1) //[cite: 13]
            {
                GameObject threeWayRoom = TrySpawnRoomSecure(threeWayRoomsPool[Random.Range(0, threeWayRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                bool threeWayBuildSuccess = false; //[cite: 13]
                GameObject nextRoomFor3Way = null; //[cite: 13]
                List<GameObject> temporaryBranchRooms = new List<GameObject>(); //[cite: 13]

                if (threeWayRoom != null) //[cite: 13]
                {
                    RoomProperty threeWayProp = threeWayRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
                    if (threeWayProp != null && threeWayProp.exitPoints.Count >= 2) //[cite: 11, 13]
                    {
                        int deadEndExitIndex = Random.Range(0, threeWayProp.exitPoints.Count); //[cite: 11, 13]
                        int mainExitIndexFor3Way = (deadEndExitIndex == 0) ? 1 : 0; //[cite: 13]

                        Transform deadEndBranchExit = threeWayProp.exitPoints[deadEndExitIndex]; //[cite: 11, 13]
                        Transform nextMainExit = threeWayProp.exitPoints[mainExitIndexFor3Way]; //[cite: 11, 13]

                        GameObject currentDeadEndBranchRoom = threeWayRoom; //[cite: 13]
                        Transform currentSubExit = deadEndBranchExit; //[cite: 13]
                        bool branchRouteSuccess = true; //[cite: 13]

                        for (int k = 0; k < deadEndBranchLength; k++) //[cite: 13]
                        {
                            if (straightCorridorsPool.Count > 0) //[cite: 13]
                            {
                                GameObject branchStraight = TrySpawnRoomSecure(straightCorridorsPool[Random.Range(0, straightCorridorsPool.Count)].roomPrefab, currentSubExit, currentDeadEndBranchRoom); //[cite: 13]
                                if (branchStraight == null) { branchRouteSuccess = false; break; } //[cite: 13]

                                temporaryBranchRooms.Add(branchStraight); //[cite: 13]
                                currentDeadEndBranchRoom = branchStraight; //[cite: 13]

                                RoomProperty subProp = currentDeadEndBranchRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
                                if (subProp != null && subProp.exitPoints.Count > 0) currentSubExit = subProp.exitPoints[0]; //[cite: 11, 13]
                                else { branchRouteSuccess = false; break; } //[cite: 13]
                            }
                        }

                        if (branchRouteSuccess && deadEndRoomsPool.Count > 0) //[cite: 13]
                        {
                            GameObject branchDeadEnd = TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, currentSubExit, currentDeadEndBranchRoom); //[cite: 13]
                            if (branchDeadEnd == null) branchRouteSuccess = false; //[cite: 13]
                            else temporaryBranchRooms.Add(branchDeadEnd); //[cite: 13]
                        }

                        if (branchRouteSuccess) //[cite: 13]
                        {
                            nextRoomFor3Way = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, nextMainExit, threeWayRoom); //[cite: 13]
                            if (nextRoomFor3Way == null) branchRouteSuccess = false; //[cite: 13]
                        }

                        if (branchRouteSuccess) //[cite: 13]
                        {
                            threeWayBuildSuccess = true; //[cite: 13]
                            currentMainRoom = nextRoomFor3Way; //[cite: 13]
                        }
                    }

                    if (!threeWayBuildSuccess) //[cite: 13]
                    {
                        foreach (GameObject tmp in temporaryBranchRooms) { if (tmp != null) { spawnedRoomInstances.Remove(tmp); DestroyImmediate(tmp); } } //[cite: 13]
                        if (nextRoomFor3Way != null) { spawnedRoomInstances.Remove(nextRoomFor3Way); DestroyImmediate(nextRoomFor3Way); } //[cite: 13]
                        spawnedRoomInstances.Remove(threeWayRoom); //[cite: 13]
                        DestroyImmediate(threeWayRoom); //[cite: 13]
                        threeWayRoom = null; //[cite: 13]
                    }
                }

                if (threeWayRoom == null) //[cite: 13]
                {
                    GameObject fallbackRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                    if (fallbackRoom == null) return false; //[cite: 13]
                    currentMainRoom = fallbackRoom; //[cite: 13]
                }
                continue; //[cite: 13]
            }

            int diceRoll = Random.Range(0, 100); //[cite: 13]
            if (diceRoll < longCorridorChance && straightCorridorsPool.Count > 0 && cornerCorridorsPool.Count > 0 && i < totalMainRooms - 1) //[cite: 13]
            {
                bool longCorridorSuccess = false; //[cite: 13]
                GameObject straightRoom = TrySpawnRoomSecure(straightCorridorsPool[Random.Range(0, straightCorridorsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                GameObject cornerRoom = null; //[cite: 13]
                GameObject nextRoomForLong = null; //[cite: 13]

                if (straightRoom != null) //[cite: 13]
                {
                    RoomProperty straightProp = straightRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
                    if (straightProp != null && straightProp.exitPoints.Count > 0) //[cite: 11, 13]
                    {
                        cornerRoom = TrySpawnRoomSecure(cornerCorridorsPool[Random.Range(0, cornerCorridorsPool.Count)].roomPrefab, straightProp.exitPoints[0], straightRoom); //[cite: 11, 13]
                        if (cornerRoom != null) //[cite: 13]
                        {
                            RoomProperty cornerProp = cornerRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
                            if (cornerProp != null && cornerProp.exitPoints.Count > 0) //[cite: 11, 13]
                            {
                                nextRoomForLong = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, cornerProp.exitPoints[0], cornerRoom); //[cite: 11, 13]
                                if (nextRoomForLong != null) //[cite: 13]
                                {
                                    longCorridorSuccess = true; //[cite: 13]
                                    currentMainRoom = nextRoomForLong; //[cite: 13]
                                }
                            }
                        }
                    }

                    if (!longCorridorSuccess) //[cite: 13]
                    {
                        if (nextRoomForLong != null) { spawnedRoomInstances.Remove(nextRoomForLong); DestroyImmediate(nextRoomForLong); } //[cite: 13]
                        if (cornerRoom != null) { spawnedRoomInstances.Remove(cornerRoom); DestroyImmediate(cornerRoom); } //[cite: 13]
                        if (straightRoom != null) { spawnedRoomInstances.Remove(straightRoom); DestroyImmediate(straightRoom); } //[cite: 13]
                        straightRoom = null; //[cite: 13]
                    }
                }

                if (straightRoom == null) //[cite: 13]
                {
                    GameObject fallbackRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                    if (fallbackRoom == null) return false; //[cite: 13]
                    currentMainRoom = fallbackRoom; //[cite: 13]
                }
                continue; //[cite: 13]
            }

            if (i < totalMainRooms - 1) //[cite: 13]
            {
                GameObject nextRoom = null; //[cite: 13]
                int largeRoomRoll = Random.Range(0, 100); //[cite: 13]

                if (largeRoomRoll < largeRoomChance && largeRoomsPool.Count > 0 && spawnedLargeRoomsCount < maxLargeRoomsPerNight) //[cite: 13]
                {
                    nextRoom = TrySpawnRoomSecure(largeRoomsPool[Random.Range(0, largeRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                    if (nextRoom != null) spawnedLargeRoomsCount++; //[cite: 13]
                    else if (normalRoomsPool.Count > 0) //[cite: 13]
                    {
                        nextRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                    }
                }
                else //[cite: 13]
                {
                    nextRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                }

                if (nextRoom == null) return false; //[cite: 13]
                currentMainRoom = nextRoom; //[cite: 13]
            }
            else //[cite: 13]
            {
                GameObject finalRoom = TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom); //[cite: 13]
                if (finalRoom == null) return false; //[cite: 13]
            }
        }

        if (activeSpecialRoomForToday != null && !hasSpawnedSpecialRoom) return false; //[cite: 13]
        return true; //[cite: 13]
    }

    private GameObject TrySpawnRoomSecure(GameObject roomPrefab, Transform targetExit, GameObject parentRoom)
    {
        if (roomPrefab == null) return null; //[cite: 13]

        GameObject newRoom = Instantiate(roomPrefab, transform); //[cite: 13]

        RoomProperty roomProp = newRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
        if (roomProp == null || roomProp.entrancePoint == null) //[cite: 11, 13]
        {
            DestroyImmediate(newRoom); //[cite: 13]
            return null; //[cite: 13]
        }

        Transform entrance = roomProp.entrancePoint; //[cite: 11, 13]

        if (targetExit != null) //[cite: 13]
        {
            newRoom.transform.rotation = targetExit.rotation * Quaternion.Inverse(entrance.localRotation); //[cite: 13]
            Vector3 gap = targetExit.position - entrance.position; //[cite: 13]
            newRoom.transform.position += gap; //[cite: 13]
        }

        Physics.SyncTransforms(); //[cite: 13]

        if (IsOverlappingWithOthers(newRoom, parentRoom)) //[cite: 13]
        {
            DestroyImmediate(newRoom); //[cite: 13]
            return null; //[cite: 13]
        }

        AttachOptimizationToRoom(newRoom); //[cite: 13]
        spawnedRoomInstances.Add(newRoom); //[cite: 13]
        return newRoom; //[cite: 13]
    }

    private void AttachOptimizationToRoom(GameObject roomInstance)
    {
        if (playerTransform == null) return; //[cite: 13]
        RoomVisibility optimization = roomInstance.GetComponent<RoomVisibility>(); //[cite: 12, 13]
        if (optimization == null) optimization = roomInstance.AddComponent<RoomVisibility>(); //[cite: 12, 13]
        optimization.SetupOptimization(playerTransform, safeDistance, maxViewDistance, viewAngle); //[cite: 12, 13]
    }

    private bool IsOverlappingWithOthers(GameObject targetRoom, GameObject parentRoom)
    {
        RoomProperty targetProp = targetRoom.GetComponent<RoomProperty>(); //[cite: 11, 13]
        if (targetProp == null || targetProp.collidersFolder == null) return false; //[cite: 11, 13]

        Collider[] systemColliders = targetProp.collidersFolder.GetComponentsInChildren<Collider>(); //[cite: 11, 13]
        if (systemColliders.Length == 0) return false; //[cite: 13]

        foreach (Collider col in systemColliders) //[cite: 13]
        {
            if (col.isTrigger) continue; //[cite: 13]

            Vector3 center; //[cite: 13]
            Vector3 halfExtents; //[cite: 13]
            Quaternion rotation = col.transform.rotation; //[cite: 13]

            if (col is BoxCollider) //[cite: 13]
            {
                BoxCollider box = (BoxCollider)col; //[cite: 13]
                center = box.transform.TransformPoint(box.center); //[cite: 13]

                Vector3 lossyScale = box.transform.lossyScale; //[cite: 13]
                halfExtents = new Vector3(
                    Mathf.Abs(box.size.x * lossyScale.x),
                    Mathf.Abs(box.size.y * lossyScale.y),
                    Mathf.Abs(box.size.z * lossyScale.z)
                ) * 0.5f * 0.95f; //[cite: 13]
            }
            else //[cite: 13]
            {
                center = col.bounds.center; //[cite: 13]
                halfExtents = col.bounds.extents * 0.95f; //[cite: 13]
            }

            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, rotation, mapCollisionLayer); //[cite: 13]

            foreach (Collider hit in hitColliders) //[cite: 13]
            {
                if (hit.transform == targetRoom.transform || hit.transform.IsChildOf(targetRoom.transform)) continue; //[cite: 13]
                if (parentRoom != null && (hit.transform == parentRoom.transform || hit.transform.IsChildOf(parentRoom.transform))) continue; //[cite: 13]

                return true; //[cite: 13]
            }
        }
        return false; //[cite: 13]
    }

    public void SetCurrentDay(int day)
    {
        currentDay = day; //[cite: 13]
    }
}