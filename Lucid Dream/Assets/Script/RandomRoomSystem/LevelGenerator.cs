using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public static LevelGenerator Instance { get; private set; }

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
        if (Input.GetKeyDown(KeyCode.G))
        {
            GenerateBranchingMap();
        }
    }

    public void GenerateBranchingMap()
    {
        foreach (GameObject room in spawnedRoomInstances) { if (room != null) Destroy(room); }
        spawnedRoomInstances.Clear();

        if (startRoomData == null || normalRoomsPool.Count == 0 || deadEndRoomsPool.Count == 0)
        {
            Debug.LogError("🚨 [LevelGen] ข้อมูลไม่ครบ!");
            return;
        }

        // 1. เสกห้องจุดเกิด
        GameObject currentMainRoom = Instantiate(startRoomData.roomPrefab, Vector3.zero, Quaternion.identity, transform);
        spawnedRoomInstances.Add(currentMainRoom);

        // 2. ลูปสร้างเส้นทางหลัก
        for (int i = 0; i < totalMainRooms; i++)
        {
            List<Transform> availableExits = GetAllExitsDeep(currentMainRoom);
            if (availableExits.Count == 0) break;

            int mainExitIndex = Random.Range(0, availableExits.Count);
            Transform mainExit = availableExits[mainExitIndex];

            // สร้างทางแยกตันในประตูที่เหลือ
            for (int j = 0; j < availableExits.Count; j++)
            {
                if (j == mainExitIndex) continue;
                if (deadEndRoomsPool.Count > 0)
                {
                    TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, availableExits[j], currentMainRoom);
                }
            }

            // 3. ระบบสุ่มเปอร์เซ็นต์สร้างทางเดินยาว
            int diceRoll = Random.Range(0, 100);
            if (diceRoll < longCorridorChance && straightCorridorsPool.Count > 0 && cornerCorridorsPool.Count > 0)
            {
                // ลองเสกทางเดินตรง (ส่ง currentMainRoom ไปเป็นห้องแม่เพื่อไม่ให้เช็คชนกันเอง)
                GameObject straightRoom = TrySpawnRoomSecure(straightCorridorsPool[Random.Range(0, straightCorridorsPool.Count)].roomPrefab, mainExit, currentMainRoom);

                if (straightRoom != null)
                {
                    currentMainRoom = straightRoom;
                    List<Transform> straightExits = GetAllExitsDeep(currentMainRoom);
                    if (straightExits.Count > 0)
                    {
                        // ลองเสกทางเลี้ยวต่อท้าย
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

            // 4. เสกห้องปกติอันถัดไปมาต่อ
            if (i < totalMainRooms - 1)
            {
                GameObject nextRoom = TrySpawnRoomSecure(normalRoomsPool[Random.Range(0, normalRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                if (nextRoom != null)
                {
                    currentMainRoom = nextRoom;
                }
                else
                {
                    // ถ้าห้องปกติชน บังคับเสกห้องปิดตายชิ้นเล็กแปะตัดจบ
                    TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
                    break;
                }
            }
            else
            {
                // ห้องสุดท้ายของด่าน ปิดด้วยห้องตัน
                TrySpawnRoomSecure(deadEndRoomsPool[Random.Range(0, deadEndRoomsPool.Count)].roomPrefab, mainExit, currentMainRoom);
            }
        }

        Debug.Log($"<color=green><b>[LevelGen] เจนด่านสำเร็จ! ระบบเคลียร์ห้องซ้อนเรียบร้อย</b></color>");
    }

    // 🌟 อัปเดตรับค่า parentRoom เพิ่มเข้ามา
    // 🔥 แก้ไขฟังก์ชันนี้ในสคริปต์ของคุณได้เลยครับ
    private GameObject TrySpawnRoomSecure(GameObject roomPrefab, Transform targetExit, GameObject parentRoom)
    {
        GameObject newRoom = Instantiate(roomPrefab, transform);

        Transform entrance = FindDeepChild(newRoom.transform, "EntrancePoint");
        if (entrance != null && targetExit != null)
        {
            // 1. หมุนห้องให้ตรงทิศก่อนเหมือนเดิม
            newRoom.transform.rotation = targetExit.rotation * Quaternion.Inverse(entrance.localRotation);

            // 🔥 2. [สูตรใหม่แก้ช่องว่าง] หาช่องว่างระหว่างประตูตรงๆ
            Vector3 gap = targetExit.position - entrance.position;

            // 🔥 3. ดึงประกบให้สนิทเป๊ะ ไร้รอยต่อมิลลิเมตร
            newRoom.transform.position += gap;
        }

        Physics.SyncTransforms();

        if (IsOverlappingWithOthers(newRoom, parentRoom))
        {
            DestroyImmediate(newRoom);
            return null;
        }

        spawnedRoomInstances.Add(newRoom);
        return newRoom;
    }

    // 🕵️‍♂️ ฟังก์ชันเรดาร์เวอร์ชันใหม่ สแกนได้หลายกล่องพร้อมกัน และเมินห้องแม่
    private bool IsOverlappingWithOthers(GameObject targetRoom, GameObject parentRoom)
    {
        // 🔥 ดึง BoxCollider ทั้งหมดที่มีในห้องนี้ (รองรับทั้งกล่องเดี่ยว และกล่องลูกผสมรูปตัว L)
        BoxCollider[] allBoxes = targetRoom.GetComponentsInChildren<BoxCollider>();

        if (allBoxes.Length == 0) return false;

        foreach (BoxCollider box in allBoxes)
        {
            // หดขนาดกล่องลงเหลือ 90% (0.9f) เพื่อป้องกันไม่ให้ขอบกำแพงเฉียดกันแล้วคิดว่าชน
            Vector3 scaledExtents = box.bounds.extents * 0.9f;

            Collider[] hitColliders = Physics.OverlapBox(box.bounds.center, scaledExtents, box.transform.rotation);

            foreach (Collider hit in hitColliders)
            {
                // ตรวจสอบว่าสิ่งที่ชน ต้องไม่ใช่ตัวเอง
                if (hit.transform.root != targetRoom.transform.root)
                {
                    // 🔥 [ไม้ตาย] ถ้าสิ่งที่ชน คืน "ห้องแม่" ที่เราเพิ่งเดินผ่านมา... ให้ปล่อยผ่าน ไม่นับว่าชน!
                    if (parentRoom != null && hit.transform.root == parentRoom.transform.root)
                    {
                        continue;
                    }

                    // แต่ถ้าไปชนห้องอื่นที่ไม่ใช่ห้องแม่ แปลว่าซ้อนทับกันจริงๆ จังๆ แล้ว!
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