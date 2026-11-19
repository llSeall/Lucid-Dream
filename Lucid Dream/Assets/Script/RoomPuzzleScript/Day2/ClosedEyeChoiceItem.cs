using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

public class ClosedEyeChoiceItem : MonoBehaviour
{
    // เก็บรายชื่อไอเท็มที่กำลังเปิดแสดงผลอยู่ เพื่อให้ตรวจจับได้ทันทีโดยไม่ติด Collider ของโต๊ะ
    public static readonly HashSet<ClosedEyeChoiceItem> ActiveItems = new HashSet<ClosedEyeChoiceItem>();

    [Header("✅ Puzzle Answer Config")]
    [Tooltip("ติ๊กถูกเฉพาะชิ้นที่เป็นคำตอบที่ถูกต้องเพียง 1 ชิ้นเท่านั้น")]
    public bool isCorrectItem = false;

    [Header("🔍 Highlight & Interaction Settings")]
    [Tooltip("UI คำใบ้ตอนหันมอง (เช่น ปุ่ม [E] เลือก)")]
    public GameObject promptUI;
    [Tooltip("Component Outline สำหรับไฮไลท์ขอบวัตถุ")]
    public Outline outlineComponent;
    [Tooltip("ระยะห่างสูงสุดที่ผู้เล่นอยู่ใกล้แล้วเส้น Outline ขึ้น + กดเลือกได้")]
    public float interactDistance = 2.5f;
    public KeyCode interactKey = KeyCode.E;

    [Header("🌐 Custom Warning Message (Optional)")]
    public LocalizedString customWrongMessage;

    private ClosedEyeChoicePuzzleManager puzzleManager;
    private Collider cachedCollider;

    private void Awake()
    {
        if (outlineComponent == null)
        {
            outlineComponent = GetComponentInChildren<Outline>(true);
        }
        cachedCollider = GetComponentInChildren<Collider>(true);
    }

    private void OnEnable()
    {
        ActiveItems.Add(this);
    }

    private void OnDisable()
    {
        ActiveItems.Remove(this);

        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }

        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }
    }

    public void Initialize(ClosedEyeChoicePuzzleManager manager)
    {
        puzzleManager = manager;
        SetHighlight(false);
    }

    public float GetDistanceFrom(Vector3 origin)
    {
        float transformDist = Vector3.Distance(origin, transform.position);
        if (cachedCollider != null && cachedCollider.enabled)
        {
            Vector3 closest = cachedCollider.ClosestPoint(origin);
            return Mathf.Min(transformDist, Vector3.Distance(origin, closest));
        }
        return transformDist;
    }

    public bool CanInteract()
    {
        if (puzzleManager == null)
        {
            puzzleManager = GetComponentInParent<ClosedEyeChoicePuzzleManager>();
            if (puzzleManager == null)
            {
                puzzleManager = FindAnyObjectByType<ClosedEyeChoicePuzzleManager>();
            }
        }

        if (puzzleManager == null) return false;
        return !puzzleManager.IsPuzzleSolved && puzzleManager.IsEyesClosed;
    }

    public void SetHighlight(bool active)
    {
        if (!CanInteract()) active = false;

        if (promptUI != null)
        {
            promptUI.SetActive(active);
        }

        if (outlineComponent != null)
        {
            outlineComponent.enabled = active;
        }
    }

    public void Interact()
    {
        if (!CanInteract()) return;

        SetHighlight(false);
        puzzleManager.OnItemSelected(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}