using UnityEngine;

public class Script_Outline : MonoBehaviour
{
    [Header("🎯 Wide Aim & Range Settings (ไม่ต้องเพ่งตรงกลางเป๊ะ)")]
    [Tooltip("ระยะตรวจจับสูงสุดรอบตัวผู้เล่น")]
    public float maxDetectDistance = 6f;

    [Tooltip("ความกว้างของรัศมีรอบกลางจอที่ยอมให้ขึ้น Outline และกด E ได้ (0.28 = ครอบคลุมพื้นที่กลางจอ ~56%)")]
    [Range(0.05f, 0.5f)]
    public float aimForgivenessRadius = 0.28f;

    public LayerMask interactableLayer = ~0;

    private enum TargetType
    {
        None,
        Key,
        ChoiceItem,
        Newspaper,
        Hint,
        EntityItem,
        AnswerPaper,
        OutlineOnly
    }

    private TargetType currentType = TargetType.None;
    private KeyPickup currentKey;
    private ClosedEyeChoiceItem currentChoiceItem;
    private NewspaperObject currentNewspaper;
    private HintObject currentHint;
    private EntityTriggerItem currentEntityItem;
    private AnswerPaper currentPaper;
    private Outline currentOutlineOnly;

    private readonly Collider[] overlapBuffer = new Collider[64];

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
        if (NewspaperObject.IsReadingNewspaper)
        {
            Clear();
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 camPos = cam.transform.position;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        TargetType bestType = TargetType.None;
        float bestViewportDist = aimForgivenessRadius;

        KeyPickup bestKey = null;
        ClosedEyeChoiceItem bestChoice = null;
        NewspaperObject bestNewspaper = null;
        HintObject bestHint = null;
        EntityTriggerItem bestEntityItem = null;
        AnswerPaper bestPaper = null;
        Outline bestOutline = null;

        // 1. ตรวจจับไอเท็มในพัซเซิลหลับตาโดยตรง (ไม่ต้องเพ่ง และไม่ติด Collider โต๊ะ)
        foreach (ClosedEyeChoiceItem item in ClosedEyeChoiceItem.ActiveItems)
        {
            if (item == null || !item.enabled || !item.gameObject.activeInHierarchy || !item.CanInteract())
                continue;

            float dist = item.GetDistanceFrom(camPos);
            if (dist > item.interactDistance) continue;

            if (TryGetScreenCenterDistance(cam, item.transform.position, screenCenter, out float vpDist) && vpDist <= bestViewportDist)
            {
                bestViewportDist = vpDist;
                bestType = TargetType.ChoiceItem;
                bestChoice = item;
            }
        }

        // 2. ตรวจจับวัตถุอื่นๆ รอบตัวผู้เล่นในระยะ maxDetectDistance
        int hitCount = Physics.OverlapSphereNonAlloc(camPos, maxDetectDistance, overlapBuffer, interactableLayer);
        for (int i = 0; i < hitCount; i++)
        {
            Collider col = overlapBuffer[i];
            if (col == null) continue;

            Vector3 closestPoint = col.ClosestPoint(camPos);
            float dist = Vector3.Distance(camPos, closestPoint);

            if (!TryGetScreenCenterDistance(cam, closestPoint, screenCenter, out float vpDist))
                continue;

            if (vpDist > bestViewportDist)
                continue;

            KeyPickup key = col.GetComponentInParent<KeyPickup>();
            if (key != null && key.enabled && dist <= key.interactDistance)
            {
                bestViewportDist = vpDist;
                bestType = TargetType.Key;
                bestKey = key;
                continue;
            }

            NewspaperObject newspaper = col.GetComponentInParent<NewspaperObject>();
            if (newspaper != null && newspaper.enabled && dist <= newspaper.interactDistance)
            {
                bestViewportDist = vpDist;
                bestType = TargetType.Newspaper;
                bestNewspaper = newspaper;
                continue;
            }

            HintObject hint = col.GetComponentInParent<HintObject>();
            if (hint != null && hint.enabled && dist <= hint.interactDistance)
            {
                bestViewportDist = vpDist;
                bestType = TargetType.Hint;
                bestHint = hint;
                continue;
            }

            EntityTriggerItem entityItem = col.GetComponentInParent<EntityTriggerItem>();
            if (entityItem != null && entityItem.enabled && dist <= entityItem.interactDistance)
            {
                bestViewportDist = vpDist;
                bestType = TargetType.EntityItem;
                bestEntityItem = entityItem;
                continue;
            }

            AnswerPaper paper = col.GetComponentInParent<AnswerPaper>();
            if (paper != null && paper.enabled && paper.CanInteract() && dist <= paper.interactDistance)
            {
                bestViewportDist = vpDist;
                bestType = TargetType.AnswerPaper;
                bestPaper = paper;
                continue;
            }

            // วัตถุทั่วไปที่มีเฉพาะ Outline (ข้ามวัตถุที่มีสคริปต์ Interact ด้านบน)
            Outline outline = col.GetComponentInParent<Outline>();
            if (outline != null
                && key == null
                && col.GetComponentInParent<ClosedEyeChoiceItem>() == null
                && newspaper == null
                && hint == null
                && entityItem == null
                && paper == null
                && dist <= 3f)
            {
                bestViewportDist = vpDist;
                bestType = TargetType.OutlineOnly;
                bestOutline = outline;
            }
        }

        // อัปเดตการแสดงเส้น Outline และคำใบ้
        ApplyBestTarget(bestType, bestKey, bestChoice, bestNewspaper, bestHint, bestEntityItem, bestPaper, bestOutline);

        // ทันทีที่เส้น Outline ขึ้นอยู่ -> กดปุ่ม E แล้วทำงานกับชิ้นที่ไฮไลท์อยู่ทันที!
        HandleInteractInput();
    }

    private bool TryGetScreenCenterDistance(Camera cam, Vector3 worldPos, Vector2 screenCenter, out float viewportDist)
    {
        Vector3 vp = cam.WorldToViewportPoint(worldPos);
        if (vp.z <= 0f)
        {
            viewportDist = float.MaxValue;
            return false;
        }

        viewportDist = Vector2.Distance(new Vector2(vp.x, vp.y), screenCenter);
        return true;
    }

    private void ApplyBestTarget(
        TargetType type,
        KeyPickup key,
        ClosedEyeChoiceItem choice,
        NewspaperObject newspaper,
        HintObject hint,
        EntityTriggerItem entityItem,
        AnswerPaper paper,
        Outline outline)
    {
        bool isSameTarget =
            currentType == type &&
            currentKey == key &&
            currentChoiceItem == choice &&
            currentNewspaper == newspaper &&
            currentHint == hint &&
            currentEntityItem == entityItem &&
            currentPaper == paper &&
            currentOutlineOnly == outline;

        if (isSameTarget) return;

        Clear();
        currentType = type;

        switch (type)
        {
            case TargetType.Key:
                currentKey = key;
                currentKey.SetHighlight(true);
                break;

            case TargetType.ChoiceItem:
                currentChoiceItem = choice;
                currentChoiceItem.SetHighlight(true);
                break;

            case TargetType.Newspaper:
                currentNewspaper = newspaper;
                currentNewspaper.SetHighlight(true);
                break;

            case TargetType.Hint:
                currentHint = hint;
                currentHint.SetHighlight(true);
                break;

            case TargetType.EntityItem:
                currentEntityItem = entityItem;
                currentEntityItem.SetHighlight(true);
                break;

            case TargetType.AnswerPaper:
                currentPaper = paper;
                currentPaper.SetHighlight(true);
                break;

            case TargetType.OutlineOnly:
                currentOutlineOnly = outline;
                currentOutlineOnly.enabled = true;
                break;
        }
    }

    private void HandleInteractInput()
    {
        switch (currentType)
        {
            case TargetType.Key:
                if (currentKey != null && Input.GetKeyDown(KeyCode.E))
                {
                    KeyPickup target = currentKey;
                    Clear();
                    target.Interact();
                }
                break;

            case TargetType.ChoiceItem:
                if (currentChoiceItem != null && Input.GetKeyDown(currentChoiceItem.interactKey))
                {
                    ClosedEyeChoiceItem target = currentChoiceItem;
                    Clear();
                    target.Interact();
                }
                break;

            case TargetType.Newspaper:
                if (currentNewspaper != null && Input.GetKeyDown(currentNewspaper.interactKey))
                {
                    NewspaperObject target = currentNewspaper;
                    Clear();
                    target.OpenNewspaper();
                }
                break;

            case TargetType.EntityItem:
                if (currentEntityItem != null && Input.GetKeyDown(currentEntityItem.interactKey))
                {
                    EntityTriggerItem target = currentEntityItem;
                    Clear();
                    target.Interact();
                }
                break;

            case TargetType.AnswerPaper:
                if (currentPaper != null && Input.GetKeyDown(KeyCode.E))
                {
                    AnswerPaper target = currentPaper;
                    Clear();
                    target.Interact();
                }
                break;
        }
    }

    void Clear()
    {
        currentType = TargetType.None;

        if (currentKey != null)
        {
            currentKey.SetHighlight(false);
            currentKey = null;
        }

        if (currentChoiceItem != null)
        {
            currentChoiceItem.SetHighlight(false);
            currentChoiceItem = null;
        }

        if (currentNewspaper != null)
        {
            currentNewspaper.SetHighlight(false);
            currentNewspaper = null;
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