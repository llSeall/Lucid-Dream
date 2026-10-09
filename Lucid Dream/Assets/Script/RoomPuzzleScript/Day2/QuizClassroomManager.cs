using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Localization;          // ✨ เรียกใช้ระบบ Localization
using UnityEngine.Localization.Settings; // ✨ เรียกใช้ระบบ Settings ของ Localization
// ✨ เพิ่ม enum กำหนดเงื่อนไขสถานะตาตอนกดตอบ
public enum RequiredEyeState
{
    Either,       // ตอบตอนลืมตาหรือหลับตาก็ได้
    MustBeClosed, // ต้องกดตอบตอน "หลับตา" เท่านั้น (ถ้าลืมตากดตอบ จะถือว่าผิดทันที)
    MustBeOpen    // ต้องกดตอบตอน "ลืมตา" เท่านั้น
}
[System.Serializable]
public class QuestionData
{
    [Tooltip("โจทย์ข้อความสเกลภาษาที่จะแสดงบนกระดานดำ")]
    public LocalizedString questionText;

    [Header("คำตอบบนกระดาษ 3 แผ่น (ตอนลืมตา)")]
    public LocalizedString[] normalAnswers = new LocalizedString[3];

    [Header("คำตอบบนกระดาษ 3 แผ่น (ตอนหลับตา)")]
    public LocalizedString[] closedEyeAnswers = new LocalizedString[3];

    [Header("เฉลย")]
    [Range(0, 2)]
    public int correctAnswerIndex = 0;

    [Header("👁️ เงื่อนไขการหลับตา/ลืมตาตอนกดตอบ ✨")]
    [Tooltip("กำหนดว่าข้อนี้ผู้เล่นต้องหลับตาหรือลืมตากดตอบ")]
    public RequiredEyeState requiredEyeState = RequiredEyeState.Either;
}


public class QuizClassroomManager : MonoBehaviour
{
    public static QuizClassroomManager Instance { get; private set; }

    [Header("📝 Question Data (6 ข้อ) ✨")]
    [SerializeField] private List<QuestionData> questions = new List<QuestionData>();

    [Header("🖥️ World Space 3D Text References")]
    [SerializeField] private TMP_Text boardText;
    [SerializeField] private TMP_Text[] paperTexts = new TMP_Text[3];

    [Header("💡 Room Light Settings")]
    [SerializeField] private List<Light> roomLights = new List<Light>();
    [SerializeField] private float flickerDuration = 1.0f;
    [SerializeField] private float flickerSpeed = 0.08f;

    [Header("⚠️ Penalty Objects (ตอบผิดเปิดออปเจกต์สะสม)")]
    [SerializeField] private GameObject[] penaltyObjects = new GameObject[3];

    [Header("🚪 Door Settings (ใช้งานร่วมกับ LockedHingeDoor)")]
    [Tooltip("ประตูทางเข้า (จะสั่ง LockDoor เมื่อผู้เล่นเดินเข้าโซน)")]
    [SerializeField] private LockedHingeDoor entranceDoor;

    [Tooltip("ประตูทางออกไปห้องถัดไป (จะสั่ง UnlockDoor เมื่อตอบคำถามครบ)")]
    [SerializeField] private LockedHingeDoor exitDoor;

    [Tooltip("ติ๊กถูก: สั่งดึงประตูทางเข้ากลับมาปิดสนิทก่อนล็อก")]
    [SerializeField] private bool closeEntranceDoorBeforeLock = true;

    [Header("😱 Jumpscare & UI Settings ✨")]
    [SerializeField] private CanvasGroup textOverlayCanvasGroup;
    [SerializeField] private TMP_Text jumpscareWarningText;
    [SerializeField] private LocalizedString lastWordsMessage;
    [SerializeField] private LocalizedString passedMessage;
    [SerializeField] private float textDisplayDuration = 2.5f;

    [Header("👻 Jumpscare Data")]
    [SerializeField] private Sprite[] jumpscareFrames;
    [SerializeField] private float jumpscareFrameRate = 12f;
    [SerializeField] private AudioClip jumpscareScreamClip;

    [Header("🔗 Manager References")]
    [SerializeField] private EyeToggleWorldManager eyeManager;

    private int currentQuestionIndex = 0;
    private int wrongCount = 0;
    private bool lastEyeClosedState = false;
    private bool isProcessingAnswer = false;
    private bool isQuizActive = false;

    public bool IsQuizActive => isQuizActive;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        // ✨ ดักจับ Event เมื่อผู้เล่นสลับภาษาในเกม ให้รีเฟรชข้อความบนกระดาน/กระดาษทันที
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void Start()
    {
        if (eyeManager == null)
        {
            eyeManager = FindAnyObjectByType<EyeToggleWorldManager>();
        }

        ClearPenaltyObjects();

        if (textOverlayCanvasGroup != null)
        {
            textOverlayCanvasGroup.alpha = 0f;
        }

        if (exitDoor != null)
        {
            exitDoor.LockDoor(false);
        }

        ClearAllTexts();
    }

    private void Update()
    {
        if (!isQuizActive || eyeManager == null || isProcessingAnswer) return;

        bool currentEyeClosed = eyeManager.IsEyesClosed;
        if (currentEyeClosed != lastEyeClosedState)
        {
            lastEyeClosedState = currentEyeClosed;
            UpdatePaperTexts(currentEyeClosed);
        }
    }

    private void OnLocaleChanged(UnityEngine.Localization.Locale newLocale)
    {
        // เมื่อมีการเปลี่ยนภาษา ให้โหลดข้อความของข้อปัจจุบันใหม่
        if (isQuizActive && currentQuestionIndex < questions.Count)
        {
            LoadQuestion(currentQuestionIndex);
        }
    }

    public void ActivateQuizZone()
    {
        if (isQuizActive) return;

        isQuizActive = true;

        if (entranceDoor != null)
        {
            entranceDoor.LockDoor(closeEntranceDoorBeforeLock);
        }

        LoadQuestion(currentQuestionIndex);
        Debug.Log("🚪 [QuizClassroomManager] ผู้เล่นเข้าโซน: ล็อกประตูทางเข้าและเริ่มคำถาม");
    }

    private void UnlockAllDoors()
    {
        if (entranceDoor != null)
        {
            entranceDoor.UnlockDoor();
        }

        if (exitDoor != null)
        {
            exitDoor.UnlockDoor();
        }

        Debug.Log("🔓 [QuizClassroomManager] ตอบคำถามครบทุกข้อ: ปลดล็อกประตูทั้ง 2 บานเรียบร้อย!");
    }

    private void ClearPenaltyObjects()
    {
        foreach (var obj in penaltyObjects)
        {
            if (obj != null) obj.SetActive(false);
        }
    }

    private void ClearAllTexts()
    {
        if (boardText != null) boardText.text = "";
        for (int i = 0; i < paperTexts.Length; i++)
        {
            if (paperTexts[i] != null) paperTexts[i].text = "";
        }
    }

    private void LoadQuestion(int index)
    {
        if (index >= questions.Count)
        {
            OnQuizCompleted();
            return;
        }

        QuestionData q = questions[index];

        // ✨ ดึงข้อความแปลตามภาษาปัจจุบันแสดงบนกระดานดำ
        if (boardText != null && q.questionText != null)
        {
            boardText.text = q.questionText.GetLocalizedString();
        }

        bool isClosed = (eyeManager != null) && eyeManager.IsEyesClosed;
        UpdatePaperTexts(isClosed);
    }

    private void OnQuizCompleted()
    {
        isQuizActive = false;
        ClearAllTexts();
        ClearPenaltyObjects();
        UnlockAllDoors();

        if (boardText != null)
        {
            // ✨ แสดงข้อความเมื่อผ่านตามภาษาที่เลือก
            boardText.text = (passedMessage != null && !passedMessage.IsEmpty) ? passedMessage.GetLocalizedString() : "PASSED";
        }
    }

    private void UpdatePaperTexts(bool isEyesClosed)
    {
        if (!isQuizActive || currentQuestionIndex >= questions.Count) return;

        QuestionData q = questions[currentQuestionIndex];
        LocalizedString[] currentAnswers = isEyesClosed ? q.closedEyeAnswers : q.normalAnswers;

        for (int i = 0; i < paperTexts.Length; i++)
        {
            if (paperTexts[i] != null && i < currentAnswers.Length && currentAnswers[i] != null)
            {
                // ✨ ดึงคำตอบแปลตามภาษาปัจจุบันลงบนกระดาษ
                paperTexts[i].text = currentAnswers[i].GetLocalizedString();
            }
        }
    }

    public void SubmitAnswer(int selectedIndex)
    {
        if (!isQuizActive || isProcessingAnswer || currentQuestionIndex >= questions.Count) return;

        StartCoroutine(ProcessAnswerRoutine(selectedIndex));
    }

    private IEnumerator ProcessAnswerRoutine(int selectedIndex)
    {
        isProcessingAnswer = true;
        QuestionData q = questions[currentQuestionIndex];

        // ✨ 1. เช็กสถานะตาปัจจุบันของผู้เล่น
        bool isClosed = (eyeManager != null) && eyeManager.IsEyesClosed;

        // ✨ 2. ตรวจสอบว่าสถานะตาตรงตามเงื่อนไขโจทย์หรือไม่
        bool isEyeStateCorrect = true;
        if (q.requiredEyeState == RequiredEyeState.MustBeClosed && !isClosed)
        {
            isEyeStateCorrect = false; // ต้องหลับตา แต่ผู้เล่นดันลืมตากด
        }
        else if (q.requiredEyeState == RequiredEyeState.MustBeOpen && isClosed)
        {
            isEyeStateCorrect = false; // ต้องลืมตา แต่ผู้เล่นดันหลับตากด
        }

        // ✨ 3. คำตอบจะถูกต้องต่อเมื่อ เลือกกระดาษถูกแผ่น AND สถานะตาถูกต้อง
        bool isCorrect = (selectedIndex == q.correctAnswerIndex) && isEyeStateCorrect;

        if (isCorrect)
        {
            yield return StartCoroutine(FlickerLightsRoutine());
        }
        else
        {
            wrongCount++;

            if (wrongCount <= penaltyObjects.Length && penaltyObjects[wrongCount - 1] != null)
            {
                penaltyObjects[wrongCount - 1].SetActive(true);
            }

            yield return StartCoroutine(FlickerLightsRoutine());

            if (wrongCount >= 4)
            {
                yield return StartCoroutine(TriggerJumpscareSequence());
                yield break;
            }
        }

        currentQuestionIndex++;
        LoadQuestion(currentQuestionIndex);
        isProcessingAnswer = false;
    }

    private IEnumerator FlickerLightsRoutine()
    {
        float timer = 0f;
        while (timer < flickerDuration)
        {
            foreach (var light in roomLights)
            {
                if (light != null) light.enabled = !light.enabled;
            }
            yield return new WaitForSeconds(flickerSpeed);
            timer += flickerSpeed;
        }

        foreach (var light in roomLights)
        {
            if (light != null) light.enabled = true;
        }
    }

    private IEnumerator TriggerJumpscareSequence()
    {
        // ✨ ดึงข้อความแจ้งเตือนแปลภาษาตอนตอบผิดครบตามกำหนด
        if (jumpscareWarningText != null && lastWordsMessage != null)
        {
            jumpscareWarningText.text = lastWordsMessage.GetLocalizedString();
        }

        if (textOverlayCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                textOverlayCanvasGroup.alpha = Mathf.Clamp01(timer / 0.5f);
                yield return null;
            }
        }

        yield return new WaitForSeconds(textDisplayDuration);

        if (textOverlayCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < 0.3f)
            {
                timer += Time.deltaTime;
                textOverlayCanvasGroup.alpha = Mathf.Clamp01(1f - (timer / 0.3f));
                yield return null;
            }
        }

        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.ShowJumpscareAnimation(jumpscareFrames, jumpscareFrameRate, jumpscareScreamClip);
        }

        yield return new WaitForSeconds(1.5f);

        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.HideJumpscare();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}