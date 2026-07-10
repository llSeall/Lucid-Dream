using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    private Dictionary<string, int> npcRelationshipData = new Dictionary<string, int>();
    private Dictionary<string, int> npcLastTalkedDay = new Dictionary<string, int>();
    private Dictionary<string, int> npcDailyChatCount = new Dictionary<string, int>();
    private Dictionary<string, int> npcDailyNormalChatCount = new Dictionary<string, int>();
    private Dictionary<string, bool> npcHasIntroduced = new Dictionary<string, bool>();
    private Dictionary<string, HashSet<string>> npcPlayedStoryKeys = new Dictionary<string, HashSet<string>>();
    private HashSet<string> unlockedRewards = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    public string InteractWithNPC(NPCConfig config, int currentDay)
    {
        string id = config.npcID;

        if (!npcRelationshipData.ContainsKey(id)) npcRelationshipData[id] = 0;
        if (!npcLastTalkedDay.ContainsKey(id)) npcLastTalkedDay[id] = -1;
        if (!npcDailyChatCount.ContainsKey(id)) npcDailyChatCount[id] = 0;
        if (!npcDailyNormalChatCount.ContainsKey(id)) npcDailyNormalChatCount[id] = 0;
        if (!npcHasIntroduced.ContainsKey(id)) npcHasIntroduced[id] = false;
        if (!npcPlayedStoryKeys.ContainsKey(id)) npcPlayedStoryKeys[id] = new HashSet<string>();

        bool isNewDayClick = (npcLastTalkedDay[id] != currentDay);

        if (isNewDayClick)
        {
            npcDailyChatCount[id] = 0;
            npcDailyNormalChatCount[id] = 0;
        }

        if (npcDailyChatCount[id] >= config.MaxDailyChats)
        {
            return GetLocalizedText(config.localizationTableName, config.dailyLimitDialogueKey);
        }

        npcDailyChatCount[id]++;

        // 👑 ลำดับ 1: แนะนำตัวครั้งแรก
        if (!npcHasIntroduced[id])
        {
            npcHasIntroduced[id] = true;
            if (isNewDayClick)
            {
                npcRelationshipData[id]++;
                npcLastTalkedDay[id] = currentDay;
            }
            CheckItemRewards(config);
            return GetLocalizedText(config.localizationTableName, config.defaultDialogueKey);
        }

        // 👑 ลำดับ 2: บทพูดเนื้อเรื่องพิเศษตามวันสะสม
        if (isNewDayClick)
        {
            int pastRelationDays = npcRelationshipData[id];
            string storyKeyToPlay = null;

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
                npcRelationshipData[id]++;
                npcLastTalkedDay[id] = currentDay;
                CheckItemRewards(config);
                return GetLocalizedText(config.localizationTableName, storyKeyToPlay);
            }
        }

        // 👑 ลำดับ 3: บทพูดปกติประจำวัน
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
            if (currentLineIndex >= todayData.dialogueKeys.Count)
            {
                currentLineIndex = todayData.dialogueKeys.Count - 1;
            }
            chosenKey = todayData.dialogueKeys[currentLineIndex];
            npcDailyNormalChatCount[id]++;
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

                if (InventoryManager.Instance != null)
                {
                    // 1. เพิ่มไอเทมเข้ากระเป๋าหลังบ้าน
                    InventoryManager.Instance.AddItem(reward.itemID);
                    Debug.Log($"[NPCManager] 🎒 เพิ่มไอเทม ID: {reward.itemID} เข้ากระเป๋าหลังบ้านสำเร็จแล้ว");

                    // 2. ดึงข้อมูล ItemData ตัวเต็มจาก Database
                    ItemData itemData = InventoryManager.Instance.itemDatabase.Find(x => x.itemID == reward.itemID);

                    // 🚨 [จุดตรวจสีแดงที่ 1] เช็คว่าลืมลากไฟล์ไอเทมใส่คลัง InventoryManager หรือไม่
                    if (itemData == null)
                    {
                        Debug.LogError($"❌ [NPCManager Error] ไม่พบข้อมูลไอเทม ID: '{reward.itemID}' ในช่อง itemDatabase ของ InventoryManager! กรุณาตรวจเช็คด่วนว่าพิมพ์ ID สะกดตรงกันไหม หรือลืมลากไฟล์ใส่ช่องรึเปล่า");
                    }

                    // 🚨 [จุดตรวจสีแดงที่ 2] เช็คว่าลืมเปิด GameObject ตัวแม่ของระบบป๊อปอัปรางวัลหรือไม่
                    if (ItemRewardPopup.Instance == null)
                    {
                        Debug.LogError("❌ [NPCManager Error] ItemRewardPopup.Instance เป็น null! แสดงว่า GameObject ตัวแม่ถูกติ๊กปิดสนิทใน Inspector ตั้งแต่เริ่มเกม สคริปต์เลยไม่ทำงาน ให้เปิดวัตถุตัวแม่เอาไว้เสมอครับ");
                    }

                    if (itemData != null && ItemRewardPopup.Instance != null)
                    {
                        string localizedNPCName = config.npcID;
                        try
                        {
                            localizedNPCName = LocalizationSettings.StringDatabase.GetLocalizedString(config.localizationTableName, config.npcNameKey);
                        }
                        catch { localizedNPCName = config.npcID; }

                        // 🚨 [จุดตรวจสีแดงที่ 3] เช็คระบบแปลภาษาของข้อความแรกพบ
                        string giftDialogue = "ได้รับไอเท็มชิ้นใหม่!";
                        try
                        {
                            giftDialogue = itemData.firstEncounterText.GetLocalizedString();
                        }
                        catch
                        {
                            Debug.LogError($"❌ [NPCManager Error] เกิดข้อผิดพลาดในการดึงภาษาฟิลด์ firstEncounterText ของไอเทม {reward.itemID} กรุณาเช็คว่าได้ผูกตารางภาษาไว้ถูกต้องหรือไม่");
                        }

                        // 3. ยิงข้อมูลเปิดป๊อปอัปแรกพบขึ้นบนหน้าจอ
                        ItemRewardPopup.Instance.ShowReward(localizedNPCName, itemData, giftDialogue);
                    }
                }
            }
        }
    }

    public void PackageDataForSave(ref GameData data) { }
    public void SyncFromSaveManager() { }
}