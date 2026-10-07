using UnityEngine;

public class AnswerPaper : MonoBehaviour
{
    [Header("Paper Settings")]
    [Tooltip("ลำดับกระดาษแผ่นนี้ (0, 1 หรือ 2)")]
    public int paperIndex = 0;

    [Tooltip("ระยะการกดโต้ตอบ")]
    public float interactDistance = 2.5f;

    [Header("Outline / Highlight")]
    [SerializeField] private Outline outlineComponent;

    private void Awake()
    {
        if (outlineComponent == null)
            outlineComponent = GetComponent<Outline>();

        SetHighlight(false);
    }

    public bool CanInteract()
    {
        return QuizClassroomManager.Instance != null && QuizClassroomManager.Instance.IsQuizActive;
    }

    public void SetHighlight(bool active)
    {
        if (active && !CanInteract()) return;

        if (outlineComponent != null)
        {
            outlineComponent.enabled = active;
        }
    }

    public void Interact()
    {
        Debug.Log($"📄 [AnswerPaper] ฟังก์ชัน Interact() ของกระดาษแผ่นที่ {paperIndex} ถูกเรียกทำงาน!");

        if (!CanInteract())
        {
            Debug.LogWarning($"⚠️ [AnswerPaper] แผ่นที่ {paperIndex} กดตอบไม่ได้ เพราะ CanInteract() เป็น false (เช็กว่าเข้า Trigger Zone หรือยัง)");
            return;
        }

        if (QuizClassroomManager.Instance != null)
        {
            Debug.Log($"📄 [AnswerPaper] กำลังส่งคำตอบ Index: {paperIndex} ไปที่ QuizClassroomManager...");
            QuizClassroomManager.Instance.SubmitAnswer(paperIndex);
        }
        else
        {
            Debug.LogError("❌ [AnswerPaper] ไม่พบ QuizClassroomManager.Instance ในฉาก!");
        }
    }
}