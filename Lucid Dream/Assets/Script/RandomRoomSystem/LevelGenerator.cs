using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    [System.Serializable]
    public struct SpecialRoomConfig
    {
        [Tooltip("วันที่จะให้ห้องนี้โผล่")]
        public int targetDay;
        [Tooltip("Prefab ห้องพิเศษของวันนั้นๆ")]
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
    [SerializeField] private int longCorridorChance = 40;

    [Header("🛠️ Layer Optimization")]
    [SerializeField] private LayerMask mapCollisionLayer;

    // ✨ [เพิ่มใหม่] พรีแฟบเตียงนอนสำหรับไปเกิดท้ายแมพ
    [Header("🛏️ Special Spawns Settings")]
    [Tooltip("ลากพรีแฟบเตียงนอน (Bed Interaction) มาใส่ช่องนี้")]
    [SerializeField] private GameObject bedPrefab;

    private List<GameObject> spawnedRoomInstances = new List<GameObject>();
    private Coroutine generationCoroutine;

    // ✨ ตัวแปรภายในสำหรับเก็บอ้างอิงห้องแรก เพื่อใช้วาร์ปผู้เล่น
    private GameObject firstRoomInstance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // ลบออกเพื่อให้ระบบเซฟสั่งการทำงานแทนเรียบร้อย
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) GenerateBranchingMap();
    }

    public string GetMapSeed() => mapSeed;

    public void GenerateMapFromSave(string savedSeed)
    {
        if (!string.IsNullOrEmpty(savedSeed))
        {
            useRandomSeed = false;
            mapSeed = savedSeed;
        }
        else
        {
            useRandomSeed = true;
        }
        GenerateBranchingMap();
    }

    public void GenerateBranchingMap()
    {
        if (generationCoroutine != null) StopCoroutine(generationCoroutine);
        generationCoroutine = StartCoroutine(GenerateBranchingMapRoutine());
        // 📄 เพิ่มบรรทัดนี้ใน LevelGenerator.cs หลังจากแมพเจนสำเร็จ 100%
        Unity.AI.Navigation.NavMeshSurface navSurface = GetComponent<Unity.AI.Navigation.NavMeshSurface>();
        if (navSurface != null)
        {
            navSurface.BuildNavMesh(); // 🔨 อบทางเดิน NavMesh ทันทีตามรูปร่างด่านที่สุ่มได้
            Debug.Log("<color=green>🧠 [NavMesh] สร้างทางเดิน AI สำหรับแมพสุ่มสำเร็จ!</color>");
        }
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

            if (!isGenerationSuccessful)
            {
                yield return null;
            }
        }

        if (isGenerationSuccessful)
        {
            Debug.Log($"<color=lime><b>✅ [LevelGen] สร้างสำเร็จในรอบที่ {currentAttempt} (Seed: {mapSeed})</b></color>");

            // ✨ [จุดเพิ่มระบบ 1] เมื่อสร้างแมพสำเร็จ 100% ไร้การชนกันแล้ว ให้วาร์ปตัวละครผู้เล่นเข้าห้องแรกทันที!
            TeleportPlayerToStart();
        }
        else
        {
            foreach (GameObject room in spawnedRoomInstances) { if (room != null) DestroyImmediate(room); }
            spawnedRoomInstances.Clear();
            Debug.LogError($"❌ [LevelGen] ล้มเหลวหลังพยายามครบ {maxAttempts} รอบ");
        }
    }

    private bool TryGenerateMap()
    {
        foreach (GameObject room in spawnedRoomInstances) { if (room != null) DestroyImmediate(room); }
        spawnedRoomInstances.Clear();

        firstRoomInstance = null; // รีเซ็ตค่าห้องแรกก่อนเริ่มสุ่มใหม่

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

        firstRoomInstance = currentMainRoom; // ✨ บันทึกพิกัดห้องแรกสุดเอาไว้สำหรับการวาร์ป

        for (int i = 0; i < totalMainRooms; i++)
        {
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

            if (activeSpecialRoomForToday != null && i == midPointIndex && !hasSpawnedSpecialRoom)
            {
                GameObject specialRoom = TrySpawnRoomSecure(activeSpecialRoomForToday.roomPrefab, mainExit, currentMainRoom);
                if (specialRoom == null) return false;

                currentMainRoom = specialRoom;
                hasSpawnedSpecialRoom = true;

                RoomProperty specialProp = specialRoom.GetComponent<RoomProperty>();
                if (specialProp == null || specialProp.exitPoints.Count == 0)
                {
                    break;
                }
                continue;
            }

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
                // 🛑 [โซนสร้างห้องสุดท้ายของสายพานหลัก]
                GameObject finalRoom = TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                if (finalRoom == null) return false;

                // ✨ [จุดเพิ่มระบบ 2] สั่งเสกเตียงเข้าสู่ห้องสุดท้ายตรงนี้ทันทีเมื่อสร้างห้องสำเร็จ!
                SpawnBedInRoom(finalRoom);
            }
        }

        if (activeSpecialRoomForToday != null && !hasSpawnedSpecialRoom) return false;
        return true;
    }

    private GameObject TrySpawnRoomSecure(GameObject roomPrefab, Transform targetExit, GameObject parentRoom)
    {
        if (roomPrefab == null) return null;

        GameObject newRoom = Instantiate(roomPrefab, transform);

        RoomProperty roomProp = newRoom.GetComponent<RoomProperty>();
        if (roomProp == null || roomProp.entrancePoint == null)
        {
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

    // ✨ [ฟังก์ชันเพิ่มใหม่] สั่งวาร์ปผู้เล่นไปจุดเริ่มต้นอย่างปลอดภัย
    // ⚡ [โค้ดอัปเดตเวอร์ชันไร้บั๊ก] สั่งค้นหาตัวละครจริงและวาร์ปอย่างแม่นยำ
    // 🎯 [เวอร์ชันล็อกพิกัดจุดเกิด] วาร์ปผู้เล่นไปยังจุดเกิดแมนนวลที่ตั้งไว้ในพรีแฟบ
    private void TeleportPlayerToStart()
    {
        // ค้นหาตัวละครผู้เล่นที่แท้จริงในฉากปัจจุบัน
        if (playerTransform == null || playerTransform.gameObject == null)
        {
            GameObject livingPlayer = GameObject.FindWithTag("Player");
            if (livingPlayer != null)
            {
                playerTransform = livingPlayer.transform;
            }
        }

        if (playerTransform == null || firstRoomInstance == null)
        {
            Debug.LogError("🚨 [LevelGen] ไม่สามารถวาร์ปได้เนื่องจากไม่พบตัวละครผู้เล่น!");
            return;
        }

        // ตั้งค่าพิกัดสำรอง (ถ้าหาจุดเกิดแมนนวลไม่เจอ จะเกิดตรงกลางห้องลอยเหนือพื้น 1 เมตร)
        Vector3 spawnPosition = firstRoomInstance.transform.position + new Vector3(0f, 1f, 0f);
        Quaternion spawnRotation = firstRoomInstance.transform.rotation;

        // 🔍 ใช้คำสั่งฟังก์ชันดักค้นหาวัตถุลูกที่ชื่อ "PlayerSpawnPoint" ภายในห้องแรก
        Transform customSpawnPoint = firstRoomInstance.transform.Find("PlayerSpawnPoint");

        if (customSpawnPoint != null)
        {
            // ดึงค่าพิกัดและมุมหมุนจากจุดเกิดที่เราจัดวางไว้ใน Unity มาใช้งานทันที!
            spawnPosition = customSpawnPoint.position;
            spawnRotation = customSpawnPoint.rotation;
            Debug.Log("<color=green>📍 [LevelGen] เจอจุดเกิดแมนนวล 'PlayerSpawnPoint' แล้ว! กำลังจัดส่งตัวละคร...</color>");
        }
        else
        {
            Debug.LogWarning("⚠️ [LevelGen] ไม่พบวัตถุชื่อ 'PlayerSpawnPoint' ในพรีแฟบห้องแรก ระบบเลยต้องจับเกิดกลางห้องแทน ซึ่งอาจจมพื้นได้!");
        }

        // 🛠️ ล็อกระบบฟิสิกส์เพื่อทำการวาร์ปอย่างปลอดภัย
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        // วาร์ปย้ายตัวละครไปยังพิกัดและทิศทางที่ถูกต้อง
        playerTransform.position = spawnPosition;
        playerTransform.rotation = spawnRotation;

        // ล้างแรงเฉื่อยเก่า (ความเร็วตกค้างจากการเดินฉากก่อนหน้า)
        Rigidbody playerRb = playerTransform.GetComponent<Rigidbody>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
        }

        // บังคับ Unity ซิงค์พิกัดโครงสร้างฟิสิกส์ใหม่ในเฟรมนี้ทันที
        Physics.SyncTransforms();

        // เปิดระบบควบคุมผู้เล่นกลับมาทำงานปกติ
        if (cc != null) cc.enabled = true;

        Debug.Log($"<color=cyan>⚡ [LevelGen] วาร์ปผู้เล่นสำเร็จ! ยืนอยู่ที่จุดเกิดแมนนวลเรียบร้อยแล้ว</color>");
    }

    // ✨ [ฟังก์ชันเพิ่มใหม่] สั่งเสกเตียงเข้าสู่ห้องสุดท้าย
    private void SpawnBedInRoom(GameObject targetRoom)
    {
        if (bedPrefab == null)
        {
            Debug.LogWarning("⚠️ ไม่พบพรีแฟบเตียงนอนใน LevelGenerator กรุณาลากใส่ช่อง Inspector");
            return;
        }

        Transform customSpawnPoint = targetRoom.transform.Find("BedSpawnPoint");

        Vector3 spawnPos = targetRoom.transform.position;
        Quaternion spawnRot = targetRoom.transform.rotation;

        if (customSpawnPoint != null)
        {
            spawnPos = customSpawnPoint.position;
            spawnRot = customSpawnPoint.rotation;
        }
        else
        {
            spawnPos += new Vector3(0f, 0.2f, 0f);
        }

        // 🛠️ วิธีแก้ด่านบี้เตียง: เปลี่ยน Parent จาก targetRoom.transform เป็น transform (ตัว LevelGenerator เองที่มีสเกล 1,1,1 เสมอ)
        GameObject bedInstance = Instantiate(bedPrefab, spawnPos, spawnRot, transform);

        // แอดเข้าลิสต์ เพื่อให้โดนลบอัตโนมัติหากแมพเจนวนลูปซ่อมแซมใหม่ระหว่างสร้าง
        spawnedRoomInstances.Add(bedInstance);

        Debug.Log($"<color=magenta>🛏️ [LevelGen] ติดตั้งเตียงสำเร็จในห้องสุดท้าย: {targetRoom.name}</color>");
    }

    private void AttachOptimizationToRoom(GameObject roomInstance)
    {
        if (playerTransform == null) return;
        RoomVisibility optimization = roomInstance.GetComponent<RoomVisibility>();
        if (optimization == null) optimization = roomInstance.AddComponent<RoomVisibility>();
        optimization.SetupOptimization(playerTransform, safeDistance, maxViewDistance, viewAngle);
    }

    private bool IsOverlappingWithOthers(GameObject targetRoom, GameObject parentRoom)
    {
        RoomProperty targetProp = targetRoom.GetComponent<RoomProperty>();
        if (targetProp == null || targetProp.collidersFolder == null) return false;

        Collider[] systemColliders = targetProp.collidersFolder.GetComponentsInChildren<Collider>();
        if (systemColliders.Length == 0) return false;

        foreach (Collider col in systemColliders)
        {
            if (col.isTrigger) continue;

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

            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, rotation, mapCollisionLayer);

            foreach (Collider hit in hitColliders)
            {
                if (hit.transform == targetRoom.transform || hit.transform.IsChildOf(targetRoom.transform)) continue;
                if (parentRoom != null && (hit.transform == parentRoom.transform || hit.transform.IsChildOf(parentRoom.transform))) continue;

                return true;
            }
        }
        return false;
    }

    public void SetCurrentDay(int day)
    {
        currentDay = day;
    }
    // ➕ เพิ่มฟังก์ชันนี้ลงใน LevelGenerator.cs
    public GameObject GetRandomSpawnedRoom()
    {
        if (spawnedRoomInstances != null && spawnedRoomInstances.Count > 0)
        {
            // สุ่มหยิบห้องในด่านส่งกลับไป
            return spawnedRoomInstances[Random.Range(0, spawnedRoomInstances.Count)];
        }
        return null;
    }
}