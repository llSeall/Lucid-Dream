using UnityEngine;

public class Script_Outline : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float maxRaycastDistance = 10f;
    public LayerMask interactableLayer = ~0;

    private KeyPickup currentKey;
    private EntityTriggerItem currentEntityItem;
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
            Outline outline = hit.collider.GetComponentInParent<Outline>();

            // 1. ตรวจสอบกุญแจ (ต้องเช็กว่าสคริปต์เปิดใช้งานอยู่ด้วย .enabled)
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

            // 2. ตรวจสอบไอเท็มกิจกรรม (ต้องเช็กว่าสคริปต์เปิดใช้งานอยู่ด้วย .enabled)
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

            // 3. วัตถุทั่วไปที่มีเฉพาะ Outline
            if (outline != null && (key == null || !key.enabled) && (entityItem == null || !entityItem.enabled))
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