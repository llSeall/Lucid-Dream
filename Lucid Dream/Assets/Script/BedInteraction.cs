using System.Collections;
using UnityEngine;
using UnityEngine.Localization; // ระบบแปลภาษา Unity
using TMPro;

public class BedInteraction : MonoBehaviour
{
    [Header("🎯 Interaction Config")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 3f;

    [Header("📺 UI Prompts")]
    [Tooltip("ลาก UI ข้อความปกติ เช่น 'กด E เพื่อเข้านอน' มาใส่")]
    [SerializeField] private GameObject interactionPromptUI;

    [Tooltip("ลาก UI ข้อความเตือน เช่น 'คุณยังไม่ได้อ่านโน๊ตประจำวัน!' มาใส่")]
    [SerializeField] private GameObject mustReadNotePromptUI;

    [Header("💬 Warning Localization ✨")]
    [Tooltip("ใส่ TextMeshPro ของข้อความเตือนเพื่ออัปเดตภาษา (ปล่อยว่างได้ถ้าใช้ Localize String Event)")]
    [SerializeField] private TextMeshProUGUI warningTextUI;

    [Tooltip("เลือก String Table และ Entry ข้อความเตือนภาษาไทย/อังกฤษ")]
    [SerializeField] private LocalizedString warningPromptMessage;

    [Tooltip("ระยะเวลาที่ข้อความเตือนจะแสดงบนจอก่อนเปลี่ยนกลับเป็นปกติ (วินาที)")]
    [SerializeField] private float warningDisplayDuration = 3.0f;

    private bool isPlayerInRange = false;
    private Transform playerTransform;
    private Coroutine warningCoroutine;

    private void Start()
    {
        HideAllPrompts();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            playerTransform = other.transform;

            // เมื่อเดินเข้าใกล้ แสดงคำสั่งกด E ปกติ
            ShowNormalPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            if (warningCoroutine != null) StopCoroutine(warningCoroutine);
            HideAllPrompts();
        }
    }

    private void Update()
    {
        if (isPlayerInRange && Input.GetKeyDown(interactKey))
        {
            if (playerTransform != null && Vector3.Distance(transform.position, playerTransform.position) <= interactionDistance)
            {
                TrySleep();
            }
        }
    }

    private void TrySleep()
    {
        bool hasReadNote = (ComputerUIManager.Instance != null && ComputerUIManager.Instance.hasReadTodayNote);

        // เงื่อนไข: ถ้ายังไม่อ่านโน๊ต แล้วกด E ให้เด้งข้อความเตือนขึ้นมา
        if (!hasReadNote)
        {
            if (warningCoroutine != null) StopCoroutine(warningCoroutine);
            warningCoroutine = StartCoroutine(ShowWarningRoutine());
            return;
        }

        // อ่านโน๊ตแล้ว -> เข้านอนได้
        TriggerSleep();
    }

    private IEnumerator ShowWarningRoutine()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        if (mustReadNotePromptUI != null) mustReadNotePromptUI.SetActive(true);

        // ดึงค่าภาษาแปลเฉพาะข้อความเตือนมาใส่ (ถ้าผูกไว้)
        if (warningTextUI != null && warningPromptMessage != null && !warningPromptMessage.IsEmpty)
        {
            warningTextUI.text = warningPromptMessage.GetLocalizedString();
        }

        yield return new WaitForSeconds(warningDisplayDuration);

        // เมื่อครบกำหนดเวลา หากผู้เล่นยังยืนใกล้อยู่ ให้กลับเป็นคำใบ้กด E ปกติ
        if (isPlayerInRange)
        {
            ShowNormalPrompt();
        }
    }

    private void ShowNormalPrompt()
    {
        if (mustReadNotePromptUI != null) mustReadNotePromptUI.SetActive(false);
        if (interactionPromptUI != null) interactionPromptUI.SetActive(true);
    }

    private void HideAllPrompts()
    {
        if (interactionPromptUI != null) interactionPromptUI.SetActive(false);
        if (mustReadNotePromptUI != null) mustReadNotePromptUI.SetActive(false);
    }

    private void TriggerSleep()
    {
        HideAllPrompts();
        Debug.Log("<color=purple>💤 ผู้เล่นเข้านอนแล้ว กำลังเดินทางเข้าสู่โลกความฝัน...</color>");

        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.EnterNighttime();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadSceneForState(GameState.Nighttime);
        }
        else
        {
            Debug.LogError("🚨 ไม่พบ TimeManager หรือ GameManager ในฉาก!");
        }
    }
}