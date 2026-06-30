using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Core Settings")]
    public string itemID;          // ไอดีห้ามซ้ำกันเด็ดขาด! (ใช้สำหรับอ้างอิงในระบบเซฟ JSON เช่น "Key_01", "Note_Yandere")
    public string itemName;        // ชื่อของไอเทมที่ต้องการให้โชว์ในเกม
    public ItemType itemType;      // เลือกประเภทไอเทมจากตรงนี้ได้เลย (Story หรือ Passive)

    [Header("Story Settings (ถ้าเป็นไอเทมเนื้อเรื่อง)")]
    [TextArea(5, 15)]
    public string storyText;       // ข้อความไดอารี่/เบาะแส สำหรับกดอ่านเมื่อไหร่ก็ได้

    //  [พื้นที่ว่าง] ในอนาคตถ้าคิดออกว่า Item ชิ้นนี้ทำอะไรได้ 
    // ค่อยมาเพิ่มตัวแปรตรงนี้ได้เลยครับ เช่น public float speedBoost; หรือ public int extraLive;
}