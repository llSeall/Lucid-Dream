using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    private Dictionary<string, int> npcRelationshipData = new Dictionary<string, int>(); // นับจำนวน "วัน" ที่เคยคุยมาในอดีต
    private Dictionary<string, int> npcLastTalkedDay = new Dictionary<string, int>();
    private Dictionary<string, int> npcDailyChatCount = new Dictionary<string, int>();     // นับรวมทุกการกดคุยวันนี้
    private Dictionary<string, int> npcDailyNormalChatCount = new Dictionary<string, int>(); // นับคิวเฉพาะตอนขึ้นบทประจำวัน
    private Dictionary<string, bool> npcHasIntroduced = new Dictionary<string, bool>();     // แฟล็กจำการแนะนำตัว
    private Dictionary<string, HashSet<string>> npcPlayedStoryKeys = new Dictionary<string, HashSet<string>>(); // คีย์เนื้อเรื่องที่ดูแล้ว
    private HashSet<string> unlockedRewards = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public string InteractWithNPC(NPCConfig config, int currentDay)
    {
        string id = config.npcID;

        // สปอนเดต้าเริ่มต้นใน RAM หากยังไม่เคยเจอ NPC ตัวนี้เลย
        if (!npcRelationshipData.ContainsKey(id)) npcRelationshipData[id] = 0;
        if (!npcLastTalkedDay.ContainsKey(id)) npcLastTalkedDay[id] = -1;
        if (!npcDailyChatCount.ContainsKey(id)) npcDailyChatCount[id] = 0;
        if (!npcDailyNormalChatCount.ContainsKey(id)) npcDailyNormalChatCount[id] = 0;
        if (!npcHasIntroduced.ContainsKey(id)) npcHasIntroduced[id] = false;
        if (!npcPlayedStoryKeys.ContainsKey(id)) npcPlayedStoryKeys[id] = new HashSet<string>();

        // 🔄 ✨ เช็คว่านี่คือการกดคุย "ครั้งแรกของวันใหม่" หรือเปล่า
        bool isNewDayClick = (npcLastTalkedDay[id] != currentDay);

        if (isNewDayClick)
        {
            npcDailyChatCount[id] = 0;
            npcDailyNormalChatCount[id] = 0; // รีเซ็ตคิวบทพูดประจำวันให้เริ่มนับประโยคที่ 1 ใหม่ในวันใหม่
        }

        // 🛑 ตรวจสอบโควตารวมประจำวัน (ห้ามคุยเกิน MaxDailyChats)
        if (npcDailyChatCount[id] >= config.MaxDailyChats)
        {
            return GetLocalizedText(config.localizationTableName, config.dailyLimitDialogueKey);
        }

        npcDailyChatCount[id]++; // บันทึกว่ามีการคลิกคุยเพิ่มขึ้น 1 ครั้ง

        // ==========================================
        // 👑 ลำดับความสำคัญที่ 1: บทแนะนำตัว (ครั้งแรกสุดในชีวิต)
        // ==========================================
        if (!npcHasIntroduced[id])
        {
            npcHasIntroduced[id] = true;

            // แสตมป์ล็อกวันและเพิ่มแต้มความสัมพันธ์ประจำวันนี้ทันที
            if (isNewDayClick)
            {
                npcRelationshipData[id]++;
                npcLastTalkedDay[id] = currentDay;
            }

            CheckItemRewards(config);
            return GetLocalizedText(config.localizationTableName, config.defaultDialogueKey);
        }

        // ==========================================
        // 👑 ลำดับความสำคัญที่ 2: บทพูดเนื้อเรื่องพิเศษตามแต้ม "จำนวนวันสะสม"
        // ==========================================
        // ✨ [ปรับปรุง] ระบบจะยอมให้บทเนื้อเรื่องพิเศษโผล่มาสอดแทรกได้เฉพาะ "การคุยครั้งแรกของวัน" เท่านั้น!
        if (isNewDayClick)
        {
            int pastRelationDays = npcRelationshipData[id]; // ดึงแต้มวันสะสมจาก "อดีต" (ยังไม่รวมวันนี้) มาคำนวณ
            string storyKeyToPlay = null;

            foreach (var relDiag in config.relationshipDialogues)
            {
                // ถ้าคุยสะสมครบ X วันในอดีตเป้าหมายตรงกันเป๊ะ
                if (pastRelationDays == relDiag.requiredRelationship)
                {
                    string uniqueStoryID = $"{id}_story_day_{relDiag.requiredRelationship}";

                    // เช็คว่าบทเนื้อเรื่องนี้เคยเล่นไปแล้วหรือยัง
                    if (!npcPlayedStoryKeys[id].Contains(uniqueStoryID))
                    {
                        storyKeyToPlay = relDiag.dialogueKey;
                        npcPlayedStoryKeys[id].Add(uniqueStoryID); // สลักล็อกระเบิดทำงานทันที! กดคุยครั้งหน้าบทนี้จะหายไป
                        break;
                    }
                }
            }

            // ถ้าเจอสลักบทพูดเนื้อเรื่องพิเศษที่ตรงเงื่อนไขวัน
            if (!string.IsNullOrEmpty(storyKeyToPlay))
            {
                // ถือว่าวันนี้ได้มาคุยแล้ว ทำการแสตมป์บวกแต้มวันสะสมเพิ่มขึ้น 1 แต้มเพื่อเตรียมใช้ในวันถัดๆ ไป
                npcRelationshipData[id]++;
                npcLastTalkedDay[id] = currentDay;

                CheckItemRewards(config);
                return GetLocalizedText(config.localizationTableName, storyKeyToPlay);
            }
        }

        // ==========================================
        // 👑 ลำดับความสำคัญที่ 3: บทพูดปกติประจำวัน (วันที่ 1 มี 4-5 บทไล่ตามคิว)
        // ==========================================
        // ✨ [ปรับปรุง] ถ้าเป็นวันใหม่แล้วไม่มีบทพิเศษ/บทแนะนำตัวมาคั่น ให้แสตมป์บวกแต้มวันของวันนี้ตรงนี้เลย
        if (isNewDayClick)
        {
            npcRelationshipData[id]++;
            npcLastTalkedDay[id] = currentDay;
        }

        string chosenKey = config.defaultDialogueKey;
        NPCConfig.DayDialogue todayData = config.daySpecificDialogues.Find(x => x.day == currentDay);

        if (todayData.dialogueKeys != null && todayData.dialogueKeys.Count > 0)
        {
            int currentLineIndex = npcDailyNormalChatCount[id];

            // ป้องกันดัชนีเกินจำนวนบทที่มี ถ้าคุยเกินคิวที่มี ให้ยึดประโยคสุดท้ายค้างไว้
            if (currentLineIndex >= todayData.dialogueKeys.Count)
            {
                currentLineIndex = todayData.dialogueKeys.Count - 1;
            }

            chosenKey = todayData.dialogueKeys[currentLineIndex];
            npcDailyNormalChatCount[id]++; // เลื่อนคิวไปประโยคถัดไปสำหรับการกดคุยรอบถัดไปของวันนี้
        }

        CheckItemRewards(config);
        return GetLocalizedText(config.localizationTableName, chosenKey);
    }

    private string GetLocalizedText(string tableName, string key)
    {
        if (string.IsNullOrEmpty(key)) return "...";
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
                if (InventoryManager.Instance != null) InventoryManager.Instance.AddItem(reward.itemID);
            }
        }
    }

    public void PackageDataForSave(ref GameData data)
    {
        data.npcSaveStates.Clear();
        foreach (var id in npcRelationshipData.Keys)
        {
            NPCSaveData state = new NPCSaveData
            {
                npcID = id,
                relationshipPoints = npcRelationshipData[id],
                lastTalkedDay = npcLastTalkedDay[id],
                dailyChatCount = npcDailyChatCount[id],
                dailyNormalChatCount = npcDailyNormalChatCount[id],
                hasIntroduced = npcHasIntroduced.ContainsKey(id) ? npcHasIntroduced[id] : false,
                playedStoryKeys = npcPlayedStoryKeys.ContainsKey(id) ? new List<string>(npcPlayedStoryKeys[id]) : new List<string>()
            };
            data.npcSaveStates.Add(state);
        }
        data.claimedNPCRewards = new List<string>(unlockedRewards);
    }

    public void SyncFromSaveManager()
    {
        if (SaveManager.Instance == null || SaveManager.Instance.gameData == null) return;

        npcRelationshipData.Clear();
        npcLastTalkedDay.Clear();
        npcDailyChatCount.Clear();
        npcDailyNormalChatCount.Clear();
        npcHasIntroduced.Clear();
        npcPlayedStoryKeys.Clear();
        unlockedRewards.Clear();

        var saveData = SaveManager.Instance.gameData;
        foreach (var state in saveData.npcSaveStates)
        {
            npcRelationshipData[state.npcID] = state.relationshipPoints;
            npcLastTalkedDay[state.npcID] = state.lastTalkedDay;
            npcDailyChatCount[state.npcID] = state.dailyChatCount;
            npcDailyNormalChatCount[state.npcID] = state.dailyNormalChatCount;
            npcHasIntroduced[state.npcID] = state.hasIntroduced;
            npcPlayedStoryKeys[state.npcID] = new HashSet<string>(state.playedStoryKeys);
        }
        unlockedRewards = new HashSet<string>(saveData.claimedNPCRewards);
    }
}