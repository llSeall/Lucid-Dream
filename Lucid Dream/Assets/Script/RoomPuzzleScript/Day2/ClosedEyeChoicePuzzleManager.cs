using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.Localization;

public class ClosedEyeChoicePuzzleManager : MonoBehaviour
{
    [Header("🐞 Debug Settings")]
    [SerializeField] private bool enableDebugLogs = true;

    [Header("👁️ Eye System Reference")]
    [Tooltip("ลาก EyeToggleWorldManager มาใส่ (ถ้าเว้นว่างระบบจะค้นหาให้อัตโนมัติ)")]
    [SerializeField] private EyeToggleWorldManager eyeManager;

    [Header("🧩 Puzzle Items (สิ่งของทั้งหมดในพัซเซิล)")]
    [Tooltip("ลากสิ่งของทั้งหมดมาใส่ (หรือวางเป็นลูกของ Object นี้ ระบบจะดึงให้อัตโนมัติ)")]
    [SerializeField] private List<ClosedEyeChoiceItem> choiceItems = new List<ClosedEyeChoiceItem>();

    [Tooltip("ซ่อนสิ่งของทั้งหมดทันทีเมื่อผู้เล่นเลือกชิ้นที่ถูกต้องแล้ว")]
    [SerializeField] private bool hideItemsWhenSolved = true;

    [Header("💡 Lights To Turn Off (ไฟที่จะปิดเมื่อเลือกถูก)")]
    [SerializeField] private List<Light> targetLights = new List<Light>();
    [SerializeField] private List<GameObject> targetLightObjects = new List<GameObject>();

    [Header("🚪 Scene Transition (ย้ายซีนเมื่อเลือกถูก)")]
    [SerializeField] private string nextSceneName = "";
    [SerializeField] private bool useDreamClearedFlow = false;
    [SerializeField] private float delayBeforeSceneLoad = 0.6f;
    [SerializeField] private CanvasGroup fadeOutCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("💬 Character Dialogue / Warning Box (กล่องข้อความบทพูด)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private float messageDisplayDuration = 3.0f;

    [Header("🌐 Localization (รองรับหลายภาษา)")]
    [SerializeField] private LocalizedString wrongChoiceLocalizedString;
    [SerializeField] private string fallbackWrongMessage = "ยังไม่ใช่ชิ้นนี้... ลองดูใหม่อีกที";
    [SerializeField] private LocalizedString correctChoiceLocalizedString;

    [Header("🔊 Audio Settings (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wrongChoiceClip;
    [SerializeField] private AudioClip correctChoiceClip;
    [SerializeField] private AudioClip lightTurnOffClip;

    [Header("🎉 Puzzle Events")]
    public UnityEvent OnPuzzleSolved;
    public UnityEvent OnWrongChoice;

    public bool IsPuzzleSolved { get; private set; } = false;
    public bool IsEyesClosed
    {
        get
        {
            if (eyeManager == null)
            {
                eyeManager = FindAnyObjectByType<EyeToggleWorldManager>();
            }
            return eyeManager != null && eyeManager.IsEyesClosed;
        }
    }

    private bool lastEyeState = false;
    private Coroutine hideMessageCoroutine;

    private void Start()
    {
        if (eyeManager == null)
        {
            eyeManager = FindAnyObjectByType<EyeToggleWorldManager>();
        }

        if (eyeManager == null)
        {
            Debug.LogError($"<color=red>[PuzzleDebug] ❌ ไม่พบ EyeToggleWorldManager ในซีน! พัซเซิล {gameObject.name} จะเช็กสถานะหลับตาไม่ได้</color>");
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (choiceItems == null || choiceItems.Count == 0)
        {
            choiceItems = new List<ClosedEyeChoiceItem>(GetComponentsInChildren<ClosedEyeChoiceItem>(true));
        }

        if (choiceItems.Count == 0)
        {
            Debug.LogError($"<color=red>[PuzzleDebug] ❌ ไม่พบ ClosedEyeChoiceItem ใน {gameObject.name}! กรุณานำไอเท็มมาวางเป็นลูก หรือลากใส่ช่อง Choice Items</color>");
        }
        else if (enableDebugLogs)
        {
            Debug.Log($"<color=cyan>[PuzzleDebug] ✅ พบไอเท็มในพัซเซิลทั้งหมด {choiceItems.Count} ชิ้น</color>");
        }

        foreach (var item in choiceItems)
        {
            if (item != null)
            {
                item.Initialize(this);
            }
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        lastEyeState = IsEyesClosed;
        UpdateItemsVisibility(lastEyeState);
    }

    private void Update()
    {
        if (IsPuzzleSolved) return;

        bool currentEyeState = IsEyesClosed;
        if (currentEyeState != lastEyeState)
        {
            lastEyeState = currentEyeState;
            if (enableDebugLogs)
            {
                Debug.Log($"<color=cyan>[PuzzleDebug] 👁️ สถานะหลับตาเปลี่ยนเป็น: {currentEyeState} (กำลัง {(currentEyeState ? "เปิด" : "ซ่อน")} ไอเท็มพัซเซิล)</color>");
            }
            UpdateItemsVisibility(currentEyeState);
        }
    }

    private void UpdateItemsVisibility(bool canSeeItems)
    {
        foreach (var item in choiceItems)
        {
            if (item != null)
            {
                item.SetHighlight(false);
                item.gameObject.SetActive(canSeeItems);
            }
        }
    }

    public void OnItemSelected(ClosedEyeChoiceItem selectedItem)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"<color=yellow>[PuzzleDebug] 👉 ผู้เล่นกดเลือกไอเท็ม: {selectedItem?.name} | ถูกหรือไม่: {selectedItem?.isCorrectItem}</color>");
        }

        if (IsPuzzleSolved || selectedItem == null) return;

        if (!IsEyesClosed)
        {
            Debug.LogWarning("[PuzzleDebug] ⚠️ กดเลือกไม่สำเร็จ เพราะสถานะ IsEyesClosed เป็น false");
            return;
        }

        if (selectedItem.isCorrectItem)
        {
            HandleCorrectSelection(selectedItem);
        }
        else
        {
            HandleWrongSelection(selectedItem);
        }
    }

    private void HandleCorrectSelection(ClosedEyeChoiceItem correctItem)
    {
        IsPuzzleSolved = true;

        if (enableDebugLogs)
        {
            Debug.Log($"<color=green>[PuzzleDebug] 🎉 เลือกถูกต้อง ({correctItem.name})! กำลังปิดไฟ {targetLights.Count} ดวง และเตรียมย้ายซีน...</color>");
        }

        TurnOffTargetLights();

        if (audioSource != null)
        {
            if (correctChoiceClip != null) audioSource.PlayOneShot(correctChoiceClip);
            if (lightTurnOffClip != null) audioSource.PlayOneShot(lightTurnOffClip);
        }

        if (correctChoiceLocalizedString != null && !correctChoiceLocalizedString.IsEmpty)
        {
            ShowDialogue(correctChoiceLocalizedString.GetLocalizedString());
        }

        foreach (var item in choiceItems)
        {
            if (item == null) continue;

            item.SetHighlight(false);

            if (eyeManager != null)
            {
                eyeManager.UnregisterObject(item.gameObject);
            }

            if (hideItemsWhenSolved)
            {
                item.gameObject.SetActive(false);
            }
        }

        OnPuzzleSolved?.Invoke();
        StartCoroutine(TransitionSceneAfterSolvedRoutine());
    }

    private void TurnOffTargetLights()
    {
        if (targetLights != null)
        {
            foreach (Light lightComp in targetLights)
            {
                if (lightComp != null)
                {
                    lightComp.enabled = false;
                }
            }
        }

        if (targetLightObjects != null)
        {
            foreach (GameObject lightObj in targetLightObjects)
            {
                if (lightObj != null)
                {
                    lightObj.SetActive(false);
                }
            }
        }
    }

    private IEnumerator TransitionSceneAfterSolvedRoutine()
    {
        if (delayBeforeSceneLoad > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeSceneLoad);
        }

        if (fadeOutCanvasGroup != null)
        {
            fadeOutCanvasGroup.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
                fadeOutCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
                yield return null;
            }
            fadeOutCanvasGroup.alpha = 1f;
        }

        if (useDreamClearedFlow && GameManager.Instance != null)
        {
            if (enableDebugLogs) Debug.Log("<color=green>[PuzzleDebug] เรียก GameManager.Instance.OnDreamCleared()</color>");
            GameManager.Instance.OnDreamCleared();
        }
        else if (!string.IsNullOrEmpty(nextSceneName))
        {
            if (enableDebugLogs) Debug.Log($"<color=green>[PuzzleDebug] กำลังโหลดฉาก: {nextSceneName}</color>");
            SceneManager.LoadScene(nextSceneName);
        }
        else
        {
            Debug.LogWarning("[PuzzleDebug] ⚠️ เลือกถูกและปิดไฟแล้ว แต่ยังไม่ได้ระบุชื่อฉากในช่อง Next Scene Name!");
        }
    }

    private void HandleWrongSelection(ClosedEyeChoiceItem wrongItem)
    {
        if (audioSource != null && wrongChoiceClip != null)
        {
            audioSource.PlayOneShot(wrongChoiceClip);
        }

        string messageToShow = fallbackWrongMessage;

        if (wrongItem.customWrongMessage != null && !wrongItem.customWrongMessage.IsEmpty)
        {
            messageToShow = wrongItem.customWrongMessage.GetLocalizedString();
        }
        else if (wrongChoiceLocalizedString != null && !wrongChoiceLocalizedString.IsEmpty)
        {
            messageToShow = wrongChoiceLocalizedString.GetLocalizedString();
        }

        if (enableDebugLogs)
        {
            Debug.Log($"<color=orange>[PuzzleDebug] ❌ เลือกผิดชิ้น ({wrongItem.name})! ข้อความเตือน: \"{messageToShow}\"</color>");
        }

        ShowDialogue(messageToShow);
        OnWrongChoice?.Invoke();
    }

    private void ShowDialogue(string message)
    {
        if (dialoguePanel == null || dialogueText == null)
        {
            Debug.LogError($"<color=red>[PuzzleDebug] ❌ แสดงกล่องข้อความไม่ได้ เพราะช่อง Dialogue Panel ({dialoguePanel != null}) หรือ Dialogue Text ({dialogueText != null}) ใน Inspector ยังว่างอยู่!</color>");
            return;
        }

        dialogueText.text = message;
        dialoguePanel.SetActive(true);

        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
        }
        hideMessageCoroutine = StartCoroutine(HideDialogueAfterDelay());
    }

    private IEnumerator HideDialogueAfterDelay()
    {
        yield return new WaitForSeconds(messageDisplayDuration);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        hideMessageCoroutine = null;
    }
}