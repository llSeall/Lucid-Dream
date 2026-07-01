using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

    [Header("🎯 Optimization (กรวยสายตาผู้เล่น)")]
    [SerializeField] private Transform playerTransform;            // 🏃 ตัวละครผู้เล่น
    [SerializeField] private float safeDistance = 15f;             // ⭕ ระยะวงกลมรอบตัวที่หันไปทางไหนก็ไม่ปิด (แนะนำ 15 เมตร)
    [SerializeField] private float maxViewDistance = 100f;         // 📏 ระยะมองไปข้างหน้าไกลสุดที่ยอมให้โหลด (ตั้งไว้ 100 เมตรก็ไกลลิบตาแล้วครับ)
    [SerializeField] private float viewAngle = 110f;               // 👁️ ความกว้างของสายตาเป็นองศา (แนะนำ 100 - 120)

    [Header("Seed Settings")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private string mapSeed = "MyScaryDungeon";

    [Header("Special Room Configs")]
    [SerializeField] private RoomData startRoomData;
    [SerializeField] private List<RoomData> straightCorridorsPool;
    [SerializeField] private List<RoomData> cornerCorridorsPool;

    [Header("Room Databases")]
    [SerializeField] private List<RoomData> normalRoomsPool = new List<RoomData>();
    [SerializeField] private List<RoomData> deadEndRoomsPool = new List<RoomData>();

    [Header("Generation Settings")]
    [SerializeField] private int totalMainRooms = 5;

    [Range(0, 100)]
    [SerializeField] private int longCorridorChance = 40;

    private List<GameObject> spawnedRoomInstances = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G)) GenerateBranchingMap();
    }

    public void GenerateBranchingMap()
    {
        foreach (GameObject room in spawnedRoomInstances) { if (room != null) Destroy(room); }
        spawnedRoomInstances.Clear();

        if (startRoomData == null || normalRoomsPool.Count == 0 || deadEndRoomsPool.Count == 0) return;

        if (useRandomSeed) mapSeed = System.DateTime.Now.Ticks.ToString().Substring(10);
        Random.InitState(mapSeed.GetHashCode());

        GameObject currentMainRoom = Instantiate(startRoomData.roomPrefab, Vector3.zero, Quaternion.identity, transform);
        AttachOptimizationToRoom(currentMainRoom);
        spawnedRoomInstances.Add(currentMainRoom);

        for (int i = 0; i < totalMainRooms; i++)
        {
            List<Transform> availableExits = GetAllExitsDeep(currentMainRoom);
            if (availableExits.Count == 0) break;

            int mainExitIndex = Random.Range(0, availableExits.Count);
            Transform mainExit = availableExits[mainExitIndex];

            for (int j = 0; j < availableExits.Count; j++)
            {
                if (j == mainExitIndex) continue;
                if (deadEndRoomsPool.Count > 0)
                {
                    TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, availableExits[j], currentMainRoom);
                }
            }

            int diceRoll = Random.Range(0, 100);
            if (diceRoll < longCorridorChance && straightCorridorsPool.Count > 0 && cornerCorridorsPool.Count > 0)
            {
                GameObject straightRoom = TrySpawnRoomSecure(straightCorridorsPool[Random.Range(0, straightCorridorsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                if (straightRoom != null)
                {
                    currentMainRoom = straightRoom;
                    List<Transform> straightExits = GetAllExitsDeep(currentMainRoom);
                    if (straightExits.Count > 0)
                    {
                        GameObject cornerRoom = TrySpawnRoomSecure(cornerCorridorsPool[Random.Range(0, cornerCorridorsPool.Count)].roomPrefab, straightExits[0], currentMainRoom);
                        if (cornerRoom != null)
                        {
                            currentMainRoom = cornerRoom;
                            List<Transform> cornerExits = GetAllExitsDeep(currentMainRoom);
                            if (cornerExits.Count > 0) mainExit = cornerExits[0];
                            else break;
                        }
                    }
                }
            }

            if (i < totalMainRooms - 1)
            {
                GameObject nextRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                if (nextRoom != null) currentMainRoom = nextRoom;
                else
                {
                    TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                    break;
                }
            }
            else
            {
                TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
            }
        }
    }

    private GameObject TrySpawnRoomSecure(GameObject roomPrefab, Transform targetExit, GameObject parentRoom)
    {
        GameObject newRoom = Instantiate(roomPrefab, transform);
        Transform entrance = FindDeepChild(newRoom.transform, "EntrancePoint");
        if (entrance != null && targetExit != null)
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
        RoomVisibility optimization = roomInstance.AddComponent<RoomVisibility>();

        // 🔥 ส่งค่าตัวแปรกรวยสายตาไปให้ห้องทำงาน
        optimization.SetupOptimization(playerTransform, safeDistance, maxViewDistance, viewAngle);
    }

    private bool IsOverlappingWithOthers(GameObject targetRoom, GameObject parentRoom)
    {
        BoxCollider[] allBoxes = targetRoom.GetComponentsInChildren<BoxCollider>();
        if (allBoxes.Length == 0) return false;

        foreach (BoxCollider box in allBoxes)
        {
            Vector3 scaledExtents = box.bounds.extents * 0.9f;
            Collider[] hitColliders = Physics.OverlapBox(box.bounds.center, scaledExtents, box.transform.rotation);

            foreach (Collider hit in hitColliders)
            {
                if (hit.transform.root != targetRoom.transform.root)
                {
                    if (parentRoom != null && hit.transform.root == parentRoom.transform.root) continue;
                    return true;
                }
            }
        }
        return false;
    }

    private List<Transform> GetAllExitsDeep(GameObject roomObject)
    {
        List<Transform> exitList = new List<Transform>();
        Transform[] allTransforms = roomObject.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name.StartsWith("ExitPoint")) exitList.Add(t);
        }
        return exitList;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        Transform[] allTransforms = parent.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in allTransforms)
        {
            if (t.name == name) return t;
        }
        return null;
    }
}