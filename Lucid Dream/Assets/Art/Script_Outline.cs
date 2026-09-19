using UnityEngine;

public class Script_Outline : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float maxRaycastDistance = 10f;
    public LayerMask interactableLayer = ~0;

    private KeyPickup currentKey;
    private EntityTriggerItem currentEntityItem;
    private HintObject currentHint; // เพิ่มตัวแปรเก็บวัตถุคำใบ้ปัจจุบัน
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
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxRaycastDistance, interactableLayer))
        {
            KeyPickup key = hit.collider.GetComponentInParent<KeyPickup>();
            EntityTriggerItem entityItem = hit.collider.GetComponentInParent<EntityTriggerItem>();
            HintObject hint = hit.collider.GetComponentInParent<HintObject>(); // ดึง Component HintObject
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

            // 4. วัตถุทั่วไปที่มีเฉพาะ Outline
            if (outline != null && (key == null || !key.enabled) && (hint == null || !hint.enabled) && (entityItem == null || !entityItem.enabled))
            {
                if (currentOutlineOnly != outline)
                {
                    Clear();
                    currentOutlineOnly = outline;
                    currentOutlineOnly.enabled = true;
                }
                return;
            }

            // ถ้ามองโดนสิ่งของอย่างอื่นที่ไม่ใช่กุญแจ/คำใบ้/ไอเท็ม ให้เคลียร์การแสดงผล
            Clear();
        }
        else
        {
            // ถ้าไม่มองโดนอะไรเลย ให้เคลียร์การแสดงผลทั้งหมด
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

        // ปิด UI/Outline ของวัตถุคำใบ้เดิมก่อนสลับหรือเมื่อเดินออกห่าง
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

        if (currentOutlineOnly != null)
        {
            currentOutlineOnly.enabled = false;
            currentOutlineOnly = null;
        }
    }
}