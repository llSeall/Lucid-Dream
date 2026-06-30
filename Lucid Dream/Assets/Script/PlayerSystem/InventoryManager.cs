using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Item Database")]
    //  วิธีใช้: นำไฟล์ ItemData (Item 1, 2, 3) ที่คุณสร้างทั้งหมดในโปรเจกต์ มาลากใส่ช่องนี้ใน Inspector เพื่อให้ระบบรู้จักของทั้งหมดในเกม
    public List<ItemData> itemDatabase = new List<ItemData>();

    [Header("Current Inventory")]
    // รายชื่อ ID ของไอเทมที่ผู้เล่นถืออยู่ ณ ปัจจุบัน (ตัวแปรนี้จะซิงก์กับ SaveManager อัตโนมัติ)
    public List<string> ownedItemIDs = new List<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ฟังก์ชันเพิ่มไอเทมเข้ากระเป๋า (สั่งเซฟเกมลง JSON อัตโนมัติทันทีที่ได้ของ)
    public void AddItem(string id)
    {
        if (!ownedItemIDs.Contains(id))
        {
            ownedItemIDs.Add(id);
            Debug.Log($"<color=green><b>[Inventory] เพิ่มไอเทม {id} เข้ากระเป๋าสำเร็จ!</b></color>");

            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        }
    }

    // ฟังก์ชันลบไอเทมออกจากกระเป๋า (เผื่อใช้ในกรณีส่งเควสต์แล้วของหาย)
    public void RemoveItem(string id)
    {
        if (ownedItemIDs.Contains(id))
        {
            ownedItemIDs.Remove(id);
            Debug.Log($"<color=red><b>[Inventory] ลบไอเทม {id} ออกจากกระเป๋าแล้ว</b></color>");

            if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();
        }
    }

    //  ฟังก์ชันสำคัญ: เอาไว้ให้ระบบอื่น (เช่น หลอดเลือด/หลอดสติ/ประตู) แอบมาเช็คว่าผู้เล่นพกไอเทมนี้อยู่ไหม
    public bool HasItem(string id)
    {
        return ownedItemIDs.Contains(id);
    }

    // ฟังก์ชันเมื่อผู้เล่นกดคลิกดูของชิ้นนี้จากหน้าต่าง UI
    public void InspectItem(string id)
    {
        ItemData data = itemDatabase.Find(x => x.itemID == id);
        if (data == null)
        {
            Debug.LogError($"[Inventory] ไม่พบข้อมูลไอเทม ID: {id} ใน Database! กรุณาลากใส่ช่อง itemDatabase ด้วยครับ");
            return;
        }

        switch (data.itemType)
        {
            case ItemType.StoryArchive:
                // ไอเทมเนื้อเรื่อง: จะพ่นข้อความออกมาให้อ่านซ้ำกี่รอบก็ได้ ของไม่หายไปไหน
                Debug.Log($"<color=cyan><b>[📖 เปิดอ่านบันทึก: {data.itemName}]</b></color>\n{data.storyText}");
                // TODO: ในอนาคตเมื่อคุณทำหน้าต่าง UI ข้อความ ค่อยสั่งเปิด UI ตรงนี้ได้เลยครับ
                break;

            case ItemType.PassiveOrCustom:
                // ไอเทมความสามารถ: ตอนนี้ทำระบบ Log บอกเฉยๆ ว่าพกอยู่นะ 
                Debug.Log($"[Inventory] ไอเทมประเภทความสามารถติดตัว: {data.itemName} (ทำงานอัตโนมัติเมื่อพกไว้)");
                //  เกร็ด: ในอนาคตหากคุณคิดความสามารถออก คุณสามารถมาเขียนเงื่อนไขเชื่อมต่อตรงนี้ได้เลยครับ
                break;
        }
    }

    // ฟังก์ชันซิงก์ข้อมูลดึงรายชื่อของจาก SaveManager (เรียกใช้งานอัตโนมัติเมื่อกดโหลดเซฟ)
    public void SyncFromSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            ownedItemIDs = new List<string>(SaveManager.Instance.gameData.collectedItems);
            Debug.Log($" [Inventory] โหลดไอเทมจากไฟล์เซฟสำเร็จ ตอนนี้พกของอยู่ {ownedItemIDs.Count} ชิ้น");
        }
    }
}