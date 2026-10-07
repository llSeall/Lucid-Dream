using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class QuestionData
{
    [TextArea(2, 4)]
    public string questionText;

    [Header("คำตอบบนกระดาษ 3 แผ่น")]
    public string[] normalAnswers = new string[3];
    public string[] closedEyeAnswers = new string[3];

    [Header("เฉลย")]
    [Range(0, 2)]
    public int correctAnswerIndex = 0;
}

public class QuizClassroomManager : MonoBehaviour
{
    public static QuizClassroomManager Instance { get; private set; }

    [Header("📝 Question Data (6 ข้อ)")]
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

    [Header("🚪 Door Settings (ใช้งานร่วมกับ LockedHingeDoor) ✨")]
    [Tooltip("ประตูทางเข้า (จะสั่ง LockDoor เมื่อผู้เล่นเดินเข้าโซน)")]
    [SerializeField] private LockedHingeDoor entranceDoor;

    [Tooltip("ประตูทางออกไปห้องถัดไป (จะสั่ง UnlockDoor เมื่อตอบคำถามครบ)")]
    [SerializeField] private LockedHingeDoor exitDoor;

    [Tooltip("ติ๊กถูก: สั่งดึงประตูทางเข้ากลับมาปิดสนิทก่อนล็อก")]
    [SerializeField] private bool closeEntranceDoorBeforeLock = true;

    [Header("😱 Jumpscare & UI Settings")]
    [SerializeField] private CanvasGroup textOverlayCanvasGroup;
    [SerializeField] private TMP_Text jumpscareWarningText;
    [TextArea(2, 3)]
    [SerializeField] private string lastWordsMessage = "คุณตอบผิดครบกำหนดแล้ว...";
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

    private void Start()
    {
        if (eyeManager == null)
        {
            eyeManager = FindAnyObjectByType<EyeToggleWorldManager>();
        }

        // 1. ซ่อน Penalty Objects ทั้งหมดตอนเริ่ม
        ClearPenaltyObjects();

        if (textOverlayCanvasGroup != null)
        {
            textOverlayCanvasGroup.alpha = 0f;
        }

        // 2. สั่งให้ประตูทางออกไปห้องถัดไปอยู่ในสถานะล็อกไว้ก่อนตั้งแต่เริ่มเกม
        if (exitDoor != null)
        {
            exitDoor.LockDoor(false);
        }

        // 3. ล้างข้อความบนกระดาน/กระดาษทิ้งไว้ก่อน
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

    /// <summary>
    /// เรียกทำงานเมื่อผู้เล่นเดินเข้า Trigger Zone (ล็อกประตูทางเข้า + เริ่มพัซเซิล)
    /// </summary>
    public void ActivateQuizZone()
    {
        if (isQuizActive) return;

        isQuizActive = true;

        // สั่งล็อกประตูทางเข้าทันที
        if (entranceDoor != null)
        {
            entranceDoor.LockDoor(closeEntranceDoorBeforeLock);
        }

        LoadQuestion(currentQuestionIndex);
        Debug.Log("🚪 [QuizClassroomManager] ผู้เล่นเข้าโซน: ล็อกประตูทางเข้าและเริ่มคำถาม");
    }

    /// <summary>
    /// ปลดล็อกประตูทั้ง 2 บานเมื่อตอบคำถามครบทุกข้อ
    /// </summary>
    private void UnlockAllDoors()
    {
        // 1. ปลดล็อกประตูทางเข้า (ประตูบานที่ 1)
        if (entranceDoor != null)
        {
            entranceDoor.UnlockDoor();
        }

        // 2. ปลดล็อกประตูออกไปห้องถัดไป (ประตูบานที่ 2)
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
        // เมื่อตอบคำถามครบทุกข้อแล้ว
        if (index >= questions.Count)
        {
            OnQuizCompleted();
            return;
        }

        QuestionData q = questions[index];

        if (boardText != null)
            boardText.text = q.questionText;

        bool isClosed = (eyeManager != null) && eyeManager.IsEyesClosed;
        UpdatePaperTexts(isClosed);
    }

    /// <summary>
    /// ทำงานเมื่อเล่นจบ/ตอบคำถามครบทุกข้อแล้ว
    /// </summary>
    private void OnQuizCompleted()
    {
        isQuizActive = false;
        ClearAllTexts();
        ClearPenaltyObjects(); // ✨ ปิด/ซ่อน Penalty Objects ทั้งหมดที่เคยเปิดขึ้นมา
        UnlockAllDoors();      // ✨ ปลดล็อกประตูทั้ง 2 บานผ่าน LockedHingeDoor

        if (boardText != null)
        {
            boardText.text = "PASSED"; // ข้อความขึ้นบอกว่าผ่านแล้ว
        }
    }

    private void UpdatePaperTexts(bool isEyesClosed)
    {
        if (!isQuizActive || currentQuestionIndex >= questions.Count) return;

        QuestionData q = questions[currentQuestionIndex];
        string[] currentAnswers = isEyesClosed ? q.closedEyeAnswers : q.normalAnswers;

        for (int i = 0; i < paperTexts.Length; i++)
        {
            if (paperTexts[i] != null && i < currentAnswers.Length)
            {
                paperTexts[i].text = currentAnswers[i];
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

        bool isCorrect = (selectedIndex == q.correctAnswerIndex);

        if (isCorrect)
        {
            yield return StartCoroutine(FlickerLightsRoutine());
        }
        else
        {
            wrongCount++;

            // เปิด Penalty Object สะสมตามจำนวนครั้งที่ผิด
            if (wrongCount <= penaltyObjects.Length && penaltyObjects[wrongCount - 1] != null)
            {
                penaltyObjects[wrongCount - 1].SetActive(true);
            }

            yield return StartCoroutine(FlickerLightsRoutine());

            // ตอบผิดครั้งที่ 4 เข้าสู่ Jumpscare
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
        if (jumpscareWarningText != null)
        {
            jumpscareWarningText.text = lastWordsMessage;
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