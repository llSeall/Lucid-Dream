using UnityEngine;
using UnityEngine.Events;

public class KeyPickup : MonoBehaviour
{
    [Header("Key Settings")]
    public string keyID = "MainKey";
    public float interactDistance = 2.5f;

    [Header("UI & Outline Settings")]
    public GameObject promptUI;
    public Outline outlineComponent;

    [Header("Toggle Visibility Settings")]
    public TriggerToggleVisibility toggleVisibility;

    [Header("Events")]
    public UnityEvent onInteractEvent;

    [Header("Destroy Delay")]
    [Tooltip("ระยะเวลาหน่วงก่อนทำลายวัตถุ เพื่อให้ Event หรือเสียงทำงานจนจบ")]
    public float destroyDelay = 0.5f;

    private bool isInteractable = true;

    void Start()
    {
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
        }

        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (!isInteractable) return;

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
        if (!isInteractable) return;
        isInteractable = false;

        // 1. ซ่อน UI และ Outline ทันที
        SetHighlight(false);

        // 2. ซ่อนภาพโมเดลและปิด Collider ทันที เพื่อไม่ให้ผู้เล่นเห็นหรือกดซ้ำได้
        HideMeshAndCollider();

        // 3. ทำงานระบบกุญแจ
        if (PlayerKeyHolder.Instance != null)
        {
            PlayerKeyHolder.Instance.AddKey(keyID);
        }

        // 4. สั่งทำงาน TriggerToggleVisibility
        if (toggleVisibility != null)
        {
            toggleVisibility.ExecuteToggle();
        }

        // 5. เรียก Event ภายนอก
        onInteractEvent?.Invoke();

        // 6. หน่วงเวลาทำลายวัตถุ เพื่อให้ระบบและ Event ประมวลผลเสร็จสิ้นก่อน
        Destroy(gameObject, destroyDelay);
    }

    private void HideMeshAndCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}