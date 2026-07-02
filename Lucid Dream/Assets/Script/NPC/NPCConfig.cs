using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCConfig", menuName = "RPG/NPC Config")]
public class NPCConfig : ScriptableObject
{
    [Header("🆔 NPC Identity")]
    public string npcID;
    public string npcNameKey;

    [Header("📅 Spawn Schedule")]
    public List<int> activeDays = new List<int>();

    [Header("💬 Localization Keys")]
    public string localizationTableName = "NPCDialogueTable";
    public string defaultDialogueKey; // ใช้เป็นบทแนะนำตัวครั้งแรกสุดในเกม
    public string dailyLimitDialogueKey;

    [SerializeField] private int maxDailyChats = 5; // เพิ่มโควตารองรับการคุยหลายรอบต่อวัน
    public int MaxDailyChats => maxDailyChats;

    [System.Serializable]
    public struct DayDialogue
    {
        public int day;
        public List<string> dialogueKeys; // ✨ เปลี่ยนเป็น List เพื่อให้วันที่ 1 มีได้หลายบทพูด
    }

    [System.Serializable]
    public struct RelationDialogue
    {
        public int requiredRelationship; // จำนวน "วัน" ที่เคยคุยมา
        public string dialogueKey;       // บทเนื้อเรื่องที่จะโผล่มาครั้งเดียวแล้วหายไป
    }

    [System.Serializable]
    public struct RewardItem { public int requiredRelationship; public string itemID; }

    [Header("🔮 Special Conditions")]
    public List<DayDialogue> daySpecificDialogues = new List<DayDialogue>();
    public List<RelationDialogue> relationshipDialogues = new List<RelationDialogue>();
    public List<RewardItem> relationshipRewards = new List<RewardItem>();
}