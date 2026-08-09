using System.Collections.Generic;
using UnityEngine;

public class NPCEntity : MonoBehaviour
{
    [Header("📄 NPC Data Settings")]
    [SerializeField] private NPCConfig npcConfiguration;

    private bool isPlayerInRange = false;

    private void OnEnable()
    {
        TimeManager.OnDayChangedSafe += UpdateNPCRegistry;
        UpdateNPCRegistry();
    }

    private void OnDisable()
    {
        TimeManager.OnDayChangedSafe -= UpdateNPCRegistry;
    }

    private void Update()
    {
        if (isPlayerInRange)
        {
            if (DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive)
                return;

#if ENABLE_INPUT_SYSTEM            
            bool pressedE = UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame;
#else
            bool pressedE = Input.GetKeyDown(KeyCode.E);
#endif

            if (pressedE)
            {
                OnInteract();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }

    public void UpdateNPCRegistry()
    {
        if (npcConfiguration == null) return;
        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;

        if (npcConfiguration.activeDays.Contains(currentDay))
        {
            gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
            isPlayerInRange = false;
        }
    }

    public void OnInteract()
    {
        if (npcConfiguration == null || NPCManager.Instance == null) return;

        int currentDay = TimeManager.Instance != null ? TimeManager.Instance.currentDay : 1;

        if (NPCManager.Instance.IsNPCLockedToday(npcConfiguration.npcID, currentDay))
        {
            return;
        }

        // ✨ รับค่าคิวบทพูด List<string>
        List<string> dialogueResult = NPCManager.Instance.InteractWithNPC(npcConfiguration, currentDay);

        if (dialogueResult == null || dialogueResult.Count == 0) return;

        string localizedName = npcConfiguration.npcID;
        try
        {
            if (!string.IsNullOrEmpty(npcConfiguration.npcNameKey))
            {
                localizedName = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase
                    .GetLocalizedString(npcConfiguration.localizationTableName, npcConfiguration.npcNameKey);
            }
        }
        catch { localizedName = npcConfiguration.npcID; }

        if (ItemRewardPopup.Instance != null && ItemRewardPopup.Instance.IsPopupActive)
        {
            ItemRewardPopup.Instance.SetPendingDialogue(localizedName, string.Join("\n\n", dialogueResult));
        }
        else if (DialogueUIController.Instance != null)
        {
            // ✨ ส่ง List<string> เข้า UI Controller โดยตรง
            DialogueUIController.Instance.ShowDialogue(localizedName, dialogueResult);
        }
    }
}