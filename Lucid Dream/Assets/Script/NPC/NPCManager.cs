using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    private Dictionary<string, int> npcRelationshipData = new Dictionary<string, int>();
    private Dictionary<string, int> npcLastTalkedDay = new Dictionary<string, int>();
    private Dictionary<string, int> npcDailyChatCount = new Dictionary<string, int>();
    private Dictionary<string, bool> npcHasIntroduced = new Dictionary<string, bool>();
    private Dictionary<string, HashSet<string>> npcPlayedStoryKeys = new Dictionary<string, HashSet<string>>();
    private HashSet<string> unlockedRewards = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public bool IsNPCLockedToday(string npcID, int currentDay)
    {
        if (!npcLastTalkedDay.ContainsKey(npcID)) return false;
        if (npcLastTalkedDay[npcID] != currentDay) return false;

        return npcDailyChatCount.ContainsKey(npcID) && npcDailyChatCount[npcID] >= 2;
    }

    // ✨ เปลี่ยนชนิดข้อมูลที่คืนค่าเป็น List<string>
    public List<string> InteractWithNPC(NPCConfig config, int currentDay)
    {
        string id = config.npcID;

        if (!npcRelationshipData.ContainsKey(id)) npcRelationshipData[id] = 0;
        if (!npcLastTalkedDay.ContainsKey(id)) npcLastTalkedDay[id] = -1;
        if (!npcDailyChatCount.ContainsKey(id)) npcDailyChatCount[id] = 0;
        if (!npcHasIntroduced.ContainsKey(id)) npcHasIntroduced[id] = false;
        if (!npcPlayedStoryKeys.ContainsKey(id)) npcPlayedStoryKeys[id] = new HashSet<string>();

        bool isNewDayClick = (npcLastTalkedDay[id] != currentDay);

        if (isNewDayClick)
        {
            npcDailyChatCount[id] = 0;
        }

        // 🌟 1. เจอหน้าครั้งแรกสุด (แนะนำตัว) -> ฟรี ไม่เสีย AP
        if (!npcHasIntroduced[id])
        {
            npcHasIntroduced[id] = true;
            return new List<string> { GetLocalizedText(config.localizationTableName, config.defaultDialogueKey) };
        }

        // 🛑 2. คุยซ้ำในวันเดิม -> พูดปฏิเสธ (ไม่เสีย AP)
        if (npcDailyChatCount[id] == 1 && !isNewDayClick)
        {
            npcDailyChatCount[id]++;
            return new List<string> { GetLocalizedText(config.localizationTableName, config.dailyLimitDialogueKey) };
        }

        // ⚡ 3. คุยประจำวัน -> หัก 1 AP[cite: 13, 18]
        if (TimeManager.Instance != null)
        {
            bool hasAP = TimeManager.Instance.UseAP(1);
            if (!hasAP)
            {
                Debug.LogWarning($"⚠️ [NPCManager] AP ไม่พอสำหรับคุยกับ {id}");
                return null;
            }
        }

        npcDailyChatCount[id] = 1;
        npcLastTalkedDay[id] = currentDay;
        npcRelationshipData[id]++;

        // 👑 ลำดับ A: บทพูดเนื้อเรื่องพิเศษ
        string storyKeyToPlay = null;
        int pastRelationDays = npcRelationshipData[id];

        foreach (var relDiag in config.relationshipDialogues)
        {
            if (pastRelationDays == relDiag.requiredRelationship)
            {
                string uniqueStoryID = $"{id}_story_day_{relDiag.requiredRelationship}";
                if (!npcPlayedStoryKeys[id].Contains(uniqueStoryID))
                {
                    storyKeyToPlay = relDiag.dialogueKey;
                    npcPlayedStoryKeys[id].Add(uniqueStoryID);
                    break;
                }
            }
        }

        if (!string.IsNullOrEmpty(storyKeyToPlay))
        {
            CheckItemRewards(config);
            return new List<string> { GetLocalizedText(config.localizationTableName, storyKeyToPlay) };
        }

        // 👑 ลำดับ B: บทพูดปกติประจำวัน (เก็บแยกใส่ List ทีละประโยค)[cite: 15]
        NPCConfig.DayDialogue todayData = config.daySpecificDialogues.Find(x => x.day == currentDay);
        List<string> fullDialogueList = new List<string>();

        if (todayData.dialogueKeys != null && todayData.dialogueKeys.Count > 0)
        {
            foreach (string key in todayData.dialogueKeys)
            {
                fullDialogueList.Add(GetLocalizedText(config.localizationTableName, key));
            }
        }
        else
        {
            fullDialogueList.Add(GetLocalizedText(config.localizationTableName, config.defaultDialogueKey));
        }

        CheckItemRewards(config);

        return fullDialogueList; // คืนค่าเป็น List ประโยคทั้งหมด
    }

    private string GetLocalizedText(string tableName, string key)
    {
        if (string.IsNullOrEmpty(key)) return "ไม่มีอะไรจะคุยแล้ว...";
        try { return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key); }
        catch { return $"[{key}]"; }
    }

    private void CheckItemRewards(NPCConfig config)
    {
        int currentRelation = npcRelationshipData[config.npcID];
        foreach (var reward in config.relationshipRewards)
        {
            string rewardKey = $"{config.npcID}_{reward.requiredRelationship}_{reward.itemID}";
            if (currentRelation >= reward.requiredRelationship && !unlockedRewards.Contains(rewardKey))
            {
                unlockedRewards.Add(rewardKey);
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItem(reward.itemID);
                    ItemData itemData = InventoryManager.Instance.itemDatabase.Find(x => x.itemID == reward.itemID);

                    if (itemData != null && ItemRewardPopup.Instance != null)
                    {
                        string localizedNPCName = config.npcID;
                        try { localizedNPCName = LocalizationSettings.StringDatabase.GetLocalizedString(config.localizationTableName, config.npcNameKey); }
                        catch { localizedNPCName = config.npcID; }

                        string giftDialogue = "ได้รับไอเท็มชิ้นใหม่!";
                        try { giftDialogue = itemData.firstEncounterText.GetLocalizedString(); } catch { }

                        ItemRewardPopup.Instance.ShowReward(localizedNPCName, itemData, giftDialogue);
                    }
                }
            }
        }
    }

    public void PackageDataForSave(ref GameData data) { }
    public void SyncFromSaveManager() { }
}