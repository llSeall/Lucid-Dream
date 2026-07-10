using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("🔑 Core Settings")]
    public string itemID;
    public ItemType itemType;

    [Header("🌙 Environment Conditions (เงื่อนไขสภาพแวดล้อม)")]
    public bool nighttimeOnly = false;

    [Header("📝 Localized Text (ข้อความในกระเป๋า)")]
    public LocalizedString itemName;
    public LocalizedString itemDescription;

    [Header("📖 First Encounter Text (ข้อความบรรยายแรกพบ)")]
    public LocalizedString firstEncounterText; // ✨ ฟิลด์ใหม่สำหรับข้อความตอนได้รับไอเทมครั้งแรก

    [Header("🖼️ Visual Settings (รูปภาพ)")]
    public Sprite itemIcon;
    public LocalizedSprite noteBackground;

    [Header("📖 Story Settings (ถ้าเป็นไอเทมเนื้อเรื่อง)")]
    public LocalizedString storyText;

    [Header("⚡ Passive Stats (สเตตัสความสามารถพิเศษ)")]
    public float speedMultiplier = 1f;
    public float maxSanityBonus = 0f;
}