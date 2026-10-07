using UnityEngine;

public class Script_Outline : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float maxRaycastDistance = 10f;
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
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, interactableLayer))
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
                        Debug.Log($"👁️ [Script_Outline] ยิง Raycast โดนกระดาษแผ่นที่ {paper.paperIndex} -> เปิด Outline");
                    }

                    if (Input.GetKeyDown(KeyCode.E))
                    {
                        Debug.Log($"⌨️ [Script_Outline] กดปุ่ม E ใส่กระดาษแผ่นที่ {currentPaper.paperIndex}");
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