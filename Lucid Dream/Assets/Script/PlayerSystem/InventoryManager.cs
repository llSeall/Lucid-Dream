using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Item Database")]
    public List<ItemData> itemDatabase = new List<ItemData>();

    [Header("Current Inventory")]
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

    public void AddItem(string id)
    {
        if (!ownedItemIDs.Contains(id))
        {
            ownedItemIDs.Add(id);
            Debug.Log($"<color=green><b>[Inventory] เพิ่มไอเทม {id} เข้ากระเป๋าสำเร็จ! (รอการบันทึกเมื่อจบวัน)</b></color>");

            // ❌ เอา SaveGame ออกแล้วเพื่อให้เป็นเดต้าชั่วคราวระหว่างวัน
        }
    }

    public void RemoveItem(string id)
    {
        if (ownedItemIDs.Contains(id))
        {
            ownedItemIDs.Remove(id);
            Debug.Log($"<color=red><b>[Inventory] ลบไอเทม {id} ออกจากกระเป๋าแล้ว</b></color>");

            // ❌ เอา SaveGame ออกแล้วเพื่อให้เป็นเดต้าชั่วคราวระหว่างวัน
        }
    }

    public bool HasItem(string id)
    {
        return ownedItemIDs.Contains(id);
    }

    public void InspectItem(string id)
    {
        ItemData data = itemDatabase.Find(x => x.itemID == id);
        if (data == null)
        {
            Debug.LogError($"[Inventory] ไม่พบข้อมูลไอเทม ID: {id} ใน Database!");
            return;
        }

        switch (data.itemType)
        {
            case ItemType.StoryArchive:
                Debug.Log($"<color=cyan><b>[📖 เปิดอ่านบันทึก: {data.itemName}]</b></color>\n{data.storyText}");
                break;

            case ItemType.PassiveOrCustom:
                Debug.Log($"[Inventory] ไอเทมประเภทความสามารถติดตัว: {data.itemName}");
                break;
        }
    }

    public void SyncFromSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            ownedItemIDs = new List<string>(SaveManager.Instance.gameData.collectedItems);
            Debug.Log($" [Inventory] โหลดไอเทมจากไฟล์เซฟสำเร็จ ตอนนี้พกของอยู่ {ownedItemIDs.Count} ชิ้น");
        }
    }
}