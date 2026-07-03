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
        }
    }

    public void RemoveItem(string id)
    {
        if (ownedItemIDs.Contains(id))
        {
            ownedItemIDs.Remove(id);
            Debug.Log($"<color=red><b>[Inventory] ลบไอเทม {id} ออกจากกระเป๋าแล้ว</b></color>");
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

        // ✨ ดึงข้อความที่ผ่านการแปลภาษาตามภาษาของเกมในปัจจุบันมาเก็บไว้ในตัวแปร
        string localizedName = data.itemName.GetLocalizedString();
        string localizedDesc = data.itemDescription.GetLocalizedString();

        switch (data.itemType)
        {
            case ItemType.StoryArchive:
                // ดึงเนื้อหาบันทึกฉบับแปลภาษาออกมา
                string localizedStory = data.storyText.GetLocalizedString();
                Debug.Log($"<color=cyan><b>[📖 เปิดอ่านบันทึก: {localizedName}]</b></color>\n{localizedStory}");

                // 💡 [คำแนะนำการต่อยอดระบบ UI ในอนาคต] สามารถโยนตัวแปรส่งไปแสดงบนหน้าจอจริงได้เลย เช่น:
                // InventoryUIWindow.Instance.OpenReadingWindow(localizedName, localizedStory, data.noteBackground);
                break;

            case ItemType.PassiveOrCustom:
                Debug.Log($"<color=yellow><b>[🎒 ไอเทมพาสซีฟ: {localizedName}]</b></color>\n{localizedDesc}");
                break;
        }
    }

    // ✨ [ฟังก์ชันใหม่] แพ็คข้อมูลไอเทมปัจจุบันส่งกลับคืนให้ SaveManager ไปเขียนบันทึกลงไฟล์ JSON ตอนจบวัน
    public void PackageDataForSave(ref GameData data)
    {
        if (data != null)
        {
            data.collectedItems = new List<string>(ownedItemIDs);
            Debug.Log($"📦 [Inventory] แพ็คของในกระเป๋า {ownedItemIDs.Count} ชิ้น เตรียมบันทึกลง Checkpoint");
        }
    }

    // 🔄 โหลดข้อมูลไอเทมจากสมุดเซฟกลับเข้ากระเป๋าเป้ตอนเปิดเกม
    public void SyncFromSaveManager()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null)
        {
            ownedItemIDs = new List<string>(SaveManager.Instance.gameData.collectedItems);
            Debug.Log($"🎒 [Inventory] โหลดไอเทมจากไฟล์เซฟสำเร็จ ตอนนี้พกของอยู่ {ownedItemIDs.Count} ชิ้น");
        }
    }
}