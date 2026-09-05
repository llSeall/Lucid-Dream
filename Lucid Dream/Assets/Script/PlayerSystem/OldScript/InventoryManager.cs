using System.Collections; //
using System.Collections.Generic; //[cite: 10]
using UnityEngine; //[cite: 10]

public class InventoryManager : MonoBehaviour //[cite: 10]
{
    public static InventoryManager Instance { get; private set; } //[cite: 10]

    [Header("Item Database")] //[cite: 10]
    public List<ItemData> itemDatabase = new List<ItemData>(); //[cite: 10]

    [Header("Current Inventory")] //[cite: 10]
    public List<string> ownedItemIDs = new List<string>(); //[cite: 10]

    private void Awake() //[cite: 10]
    {
        if (Instance == null) //[cite: 10]
        {
            Instance = this; //[cite: 10]
            DontDestroyOnLoad(gameObject); //[cite: 10]
        }
        else //[cite: 10]
        {
            Destroy(gameObject); //[cite: 10]
        }
    }

    /// <summary>
    /// ✨ ฟังก์ชันใหม่: คำนวณบัฟความเร็วเดิน/วิ่งรวมจากไอเทมทั้งหมดในกระเป๋า โดยตรวจสอบช่วงเวลาในเกมด้วย
    /// </summary>
    public float GetTotalSpeedMultiplier()
    {
        float totalMultiplier = 1f;

        // ความปลอดภัย: ถ้าไม่มี GameManager หรือไม่มีของในกระเป๋า ให้คืนค่าความเร็วปกติ (1 เท่า)
        if (GameManager.Instance == null || ownedItemIDs.Count == 0) return totalMultiplier;

        // เช็คว่าปัจจุบันเป็นเวลากลางคืน/โลกความฝันหรือไม่
        bool isNighttime = (GameManager.Instance.currentState == GameState.Nighttime);

        foreach (string id in ownedItemIDs)
        {
            ItemData data = itemDatabase.Find(x => x.itemID == id);
            if (data != null && data.itemType == ItemType.PassiveOrCustom)
            {
                // 🚨 เงื่อนไขเด็ด: ถ้าไอเทมนี้ระบุว่าต้องเป็นกลางคืนเท่านั้น แต่ปัจจุบันเป็นกลางวัน -> ข้ามชิ้นนี้ไป ไม่นำมาบวกพลัง
                if (data.nighttimeOnly && !isNighttime)
                {
                    continue;
                }

                // สะสมพลังบัฟความเร็ว (คิดแบบส่วนต่าง เช่น สปีด 1.5f จะกลายเป็นบวกเพิ่ม 0.5f)
                totalMultiplier += (data.speedMultiplier - 1f);
            }
        }

        return totalMultiplier;
    }

    public void AddItem(string id) //[cite: 10]
    {
        if (!ownedItemIDs.Contains(id)) //[cite: 10]
        {
            ownedItemIDs.Add(id); //[cite: 10]
            Debug.Log($"<color=green><b>[Inventory] เพิ่มไอเทม {id} เข้ากระเป๋าสำเร็จ!</b></color>"); //[cite: 10]
        }
    }

    public void RemoveItem(string id) //[cite: 10]
    {
        if (ownedItemIDs.Contains(id)) //[cite: 10]
        {
            ownedItemIDs.Remove(id); //[cite: 10]
        }
    }

    public bool HasItem(string id) //[cite: 10]
    {
        return ownedItemIDs.Contains(id); //[cite: 10]
    }

    public void InspectItem(string id) //[cite: 10]
    {
        ItemData data = itemDatabase.Find(x => x.itemID == id); //[cite: 10]
        if (data == null) return; //[cite: 10]

        string localizedName = data.itemName.GetLocalizedString(); //[cite: 10]
        string localizedDesc = data.itemDescription.GetLocalizedString(); //[cite: 10]

        switch (data.itemType) //[cite: 10]
        {
            case ItemType.StoryArchive: //[cite: 10]
                string localizedStory = data.storyText.GetLocalizedString(); //[cite: 10]
                Debug.Log($"<color=cyan><b>[📖 เปิดอ่านบันทึก: {localizedName}]</b></color>\n{localizedStory}"); //[cite: 10]
                break; //[cite: 10]

            case ItemType.PassiveOrCustom: //[cite: 10]
                Debug.Log($"<color=yellow><b>[🎒 ไอเทมพาสซีฟ: {localizedName}]</b></color>\n{localizedDesc}"); //[cite: 10]
                break; //[cite: 10]
        }
    }

    public void PackageDataForSave(ref GameData data) //[cite: 10]
    {
        if (data != null) data.collectedItems = new List<string>(ownedItemIDs); //[cite: 10]
    }

    public void SyncFromSaveManager() //[cite: 10]
    {
        if (SaveManager.Instance != null && SaveManager.Instance.gameData != null) //[cite: 10]
        {
            ownedItemIDs = new List<string>(SaveManager.Instance.gameData.collectedItems); //[cite: 10]
        }
    }
}