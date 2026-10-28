using UnityEngine;

public class Script_Outline : MonoBehaviour
{
    [Header("Raycast & SphereCast Settings")]
    public float maxRaycastDistance = 10f;
    [Tooltip("รัศมีทรงกลมตรวจจับวัตถุชิ้นเล็ก (แนะนำ 0.1 ถึง 0.25)")]
    public float sphereRadius = 0.15f;
    public LayerMask interactableLayer = ~0;

    private KeyPickup currentKey;
    private EntityTriggerItem currentEntityItem;
    private HintObject currentHint;
    private AnswerPaper currentPaper;
    private Outline currentOutlineOnly;

    void Start()
    {
        Outline[] allOutlines = FindObjectsOfType<Outline>();
        foreach (Outline outline in allOutlines)
        {
            outline.enabled = false;
        }
    }

    void Update()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // ✨ เปลี่ยนเป็น SphereCast เพื่อขยายรัศมีตรวจจับวัตถุชิ้นเล็กอย่างกุญแจ
        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxRaycastDistance, interactableLayer))
        {
            KeyPickup key = hit.collider.GetComponentInParent<KeyPickup>();
            EntityTriggerItem entityItem = hit.collider.GetComponentInParent<EntityTriggerItem>();
            HintObject hint = hit.collider.GetComponentInParent<HintObject>();
            AnswerPaper paper = hit.collider.GetComponentInParent<AnswerPaper>();
            Outline outline = hit.collider.GetComponentInParent<Outline>();

            // 1. ตรวจสอบกุญแจ
            if (key != null && key.enabled)
            {
                if (hit.distance <= key.interactDistance)
                {
                    if (currentKey != key)
                    {
                        Clear();
                        currentKey = key;
                        currentKey.SetHighlight(true);
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        KeyPickup targetKey = currentKey;
                        Clear();
                        targetKey.Interact();
                    }
                    return;
                }
            }

            // 2. ตรวจสอบวัตถุคำใบ้ (HintObject)
            if (hint != null && hint.enabled)
            {
                if (hit.distance <= hint.interactDistance)
                {
                    if (currentHint != hint)
                    {
                        Clear();
                        currentHint = hint;
                        currentHint.SetHighlight(true);
                    }
                    return;
                }
            }

            // 3. ตรวจสอบไอเท็มกิจกรรม (EntityTriggerItem)
            if (entityItem != null && entityItem.enabled)
            {
                if (hit.distance <= entityItem.interactDistance)
                {
                    if (currentEntityItem != entityItem)
                    {
                        Clear();
                        currentEntityItem = entityItem;
                        currentEntityItem.SetHighlight(true);
                    }

                    if (Input.GetKeyDown(entityItem.interactKey))
                    {
                        EntityTriggerItem targetItem = currentEntityItem;
                        Clear();
                        targetItem.Interact();
                    }
                    return;
                }
            }

            // 4. ตรวจสอบกระดาษคำตอบ (AnswerPaper)
            if (paper != null && paper.enabled && paper.CanInteract())
            {
                if (hit.distance <= paper.interactDistance)
                {
                    if (currentPaper != paper)
                    {
                        Clear();
                        currentPaper = paper;
                        currentPaper.SetHighlight(true);
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        AnswerPaper targetPaper = currentPaper;
                        Clear();
                        if (targetPaper != null)
                        {
                            targetPaper.Interact();
                        }
                    }
                    return;
                }
            }

            // 5. วัตถุทั่วไปที่มีเฉพาะ Outline
            if (outline != null && (key == null || !key.enabled) && (hint == null || !hint.enabled) && (entityItem == null || !entityItem.enabled) && (paper == null || !paper.enabled))
            {
                if (currentOutlineOnly != outline)
                {
                    Clear();
                    currentOutlineOnly = outline;
                    currentOutlineOnly.enabled = true;
                }
                return;
            }

            Clear();
        }
        else
        {
            Clear();
        }
    }

    void Clear()
    {
        if (currentKey != null)
        {
            currentKey.SetHighlight(false);
            currentKey = null;
        }

        if (currentHint != null)
        {
            currentHint.SetHighlight(false);
            currentHint = null;
        }

        if (currentEntityItem != null)
        {
            currentEntityItem.SetHighlight(false);
            currentEntityItem = null;
        }

        if (currentPaper != null)
        {
            currentPaper.SetHighlight(false);
            currentPaper = null;
        }

        if (currentOutlineOnly != null)
        {
            currentOutlineOnly.enabled = false;
            currentOutlineOnly = null;
        }
    }
}
