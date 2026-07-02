using System.Collections.Generic;
using UnityEngine;

public class RoomProperty : MonoBehaviour
{
    [Header("🚪 Room Links")]
    [Tooltip("ลาก GameObject ที่ชื่อ EntrancePoint ของห้องนี้มาใส่")]
    public Transform entrancePoint;

    [Tooltip("ลากบรรดา ExitPoint ทั้งหมดของห้องนี้มาใส่ใน List")]
    public List<Transform> exitPoints = new List<Transform>();

    [Header("🛡️ Collision Setup")]
    [Tooltip("ลากโฟลเดอร์ 'Colliders' (ศูนย์รวมคอลไลเดอร์ระบบ/พื้น/กำแพง) ของห้องนี้มาใส่")]
    public Transform collidersFolder;
}