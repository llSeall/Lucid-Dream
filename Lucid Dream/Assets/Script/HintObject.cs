using UnityEngine;
using UnityEngine.Events;

public class HintObject : MonoBehaviour
{
    [Header("UI & Outline Settings")]
    [Tooltip("UI คำใบ้ที่จะให้เด้งขึ้นมาเมื่อผู้เล่นมอง (เช่น ข้อความคำใบ้ หรือ Icon)")]
    public GameObject promptUI;

    [Tooltip("Component Outline (ถ้ามี) สำหรับไฮไลท์ขอบวัตถุ")]
    public Outline outlineComponent;

    [Header("Distance Settings")]
    [Tooltip("ระยะห่างสูงสุดที่ผู้เล่นมองแล้ว UI จะขึ้น")]
    public float interactDistance = 2.5f;

    [Header("Events (Optional)")]
    [Tooltip("Event เพิ่มเติมเมื่อผู้เล่นมองวัตถุนี้ (ถ้าต้องการใช้)")]
    public UnityEvent OnLookAtEvent;

    private bool isBeingLookedAt = false;

    void Start()
    {
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
        }

        // ปิด UI และ Outline เป็นค่าเริ่มต้น
        SetHighlight(false);
    }

    /// <summary>
    /// เรียกใช้เมื่อ Raycast ของผู้เล่นมองมาที่วัตถุนี้ (active = true) หรือหันหน้าหนี (active = false)
    /// </summary>
    public void SetHighlight(bool active)
    {
        if (promptUI != null)
        {
            promptUI.SetActive(active);
        }

        if (outlineComponent != null)
        {
            outlineComponent.enabled = active;
        }

        // เรียก Event เมื่อผู้เล่นเพิ่งมองเห็นวัตถุเป็นครั้งแรก
        if (active && !isBeingLookedAt)
        {
            isBeingLookedAt = true;
            OnLookAtEvent?.Invoke();
        }
        else if (!active)
        {
            isBeingLookedAt = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}