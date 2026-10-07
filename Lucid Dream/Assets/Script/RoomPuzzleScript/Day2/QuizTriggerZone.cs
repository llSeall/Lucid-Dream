using UnityEngine;

public class QuizTriggerZone : MonoBehaviour
{
    [Tooltip("แท็กของตัวละครผู้เล่น")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("เปิดใช้งาน ให้ทริกเกอร์ทำงานแค่ครั้งเดียว")]
    [SerializeField] private bool triggerOnce = true;

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnce) return;

        if (other.CompareTag(playerTag))
        {
            if (QuizClassroomManager.Instance != null)
            {
                hasTriggered = true;
                QuizClassroomManager.Instance.ActivateQuizZone();
            }
        }
    }
}