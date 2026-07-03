using UnityEngine;
using UnityEngine.Localization; // ✨ จำเป็นต้องใช้สำหรับระบบแปลภาษาของ Unity

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("🔑 Core Settings")]
    public string itemID;          // ไอดีห้ามซ้ำกันเด็ดขาด! (ใช้สำหรับอ้างอิงในระบบเซฟ JSON เช่น "Key_01", "Note_Yandere")
    public ItemType itemType;      // เลือกประเภทไอเทม (Story หรือ Passive)

    [Header("📝 Localized Text (ข้อความแปลภาษา)")]
    public LocalizedString itemName;        // ชื่อของไอเทมฉบับแปลภาษา (ดึงจากตาราง Localization)
    public LocalizedString itemDescription; // คำอธิบายไอเทม / คำอธิบายความสามารถพาสซีฟ

    [Header("🖼️ Visual Settings (รูปภาพ)")]
    public Sprite itemIcon;                 // รูปไอคอนทั่วไปในกระเป๋า (ใช้ร่วมกันได้ทุกภาษา)
    public LocalizedSprite noteBackground;  // รูปพื้นหลังกระดาษบันทึก (แปลภาษาได้ เผื่อมีข้อความลายมืออยู่บนภาพแยกภาษา)

    [Header("📖 Story Settings (ถ้าเป็นไอเทมเนื้อเรื่อง)")]
    public LocalizedString storyText;       // ข้อความไดอารี่/เบาะแส สำหรับกดอ่านเมื่อไหร่ก็ได้ (แปลภาษาได้)

    [Header("⚡ Passive Stats (สเตตัสความสามารถพิเศษ)")]
    public float speedMultiplier = 1f;     // ตัวอย่าง: ถ้าพกชิ้นนี้จะเดินไวขึ้นกี่เท่า (1f คือปกติ, 1.2f คือไวขึ้น 20%)
    public float maxSanityBonus = 0f;      // ตัวอย่าง: พกแล้วเพิ่มพลังสติ (Sanity) สูงสุดให้กับผู้เล่น
}