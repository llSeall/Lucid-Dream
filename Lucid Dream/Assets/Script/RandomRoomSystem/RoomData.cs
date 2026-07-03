using UnityEngine;

public enum RoomSize { SmallCorridor, LargePlayground }

[CreateAssetMenu(fileName = "NewRoomData", menuName = "Procedural/Room Data")]
public class RoomData : ScriptableObject
{
    [Header("Visual Config")]
    public string roomID;              // ไอดีอ้างอิงห้อง (ห้ามซ้ำกัน)[cite: 14]
    public GameObject roomPrefab;      // ตัวโมเดลห้องที่เป็น Prefab ยกลูก[cite: 14]
    public RoomSize roomSize;          // ขนาดห้อง (เอาไว้จำประเภท)[cite: 14]

    [Header("Story Conditions")]
    public bool isSecretRoom = false;  // ติ๊กถูกถ้าห้องนี้เป็นห้องลับของ NPC[cite: 14]
    public string requiredNPCKey;      // คีย์ของ NPC (เช่น "Npc_A")[cite: 14]
    public int requiredAffinity = 50;  // ค่าความสัมพันธ์ที่ต้องการในการปลดล็อค[cite: 14]
}