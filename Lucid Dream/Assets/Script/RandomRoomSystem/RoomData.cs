using UnityEngine;

public enum RoomSize { SmallCorridor, LargePlayground }

[CreateAssetMenu(fileName = "NewRoomData", menuName = "Procedural/Room Data")]
public class RoomData : ScriptableObject
{
    [Header("Visual Config")]
    public string roomID;              // ไอดีอ้างอิงห้อง (ห้ามซ้ำกัน)
    public GameObject roomPrefab;      // ตัวโมเดลห้องที่เป็น Prefab ยกลูก
    public RoomSize roomSize;          // ขนาดห้อง (เอาไว้จำประเภท)

    [Header("Story Conditions")]
    public bool isSecretRoom = false;  // ติ๊กถูกถ้าห้องนี้เป็นห้องลับของ NPC
    public string requiredNPCKey;      // คีย์ของ NPC (เช่น "Npc_A")
    public int requiredAffinity = 50;  // ค่าความสัมพันธ์ที่ต้องการในการปลดล็อค
}