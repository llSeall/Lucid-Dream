using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;

public class NewspaperObject : MonoBehaviour
{
    public static bool IsReadingNewspaper { get; private set; } = false;
    public static bool JustClosedThisFrame { get; private set; } = false;
    private static NewspaperObject activeNewspaper;

    [Header("🔍 Highlight & Interaction Settings")]
    [Tooltip("UI คำใบ้ตอนหันมอง (เช่น ปุ่ม [E] อ่านหนังสือพิมพ์)")]
    public GameObject promptUI;
    [Tooltip("Component Outline สำหรับไฮไลท์ขอบหนังสือพิมพ์")]
    public Outline outlineComponent;
    public KeyCode interactKey = KeyCode.E;
    public KeyCode exitKey = KeyCode.Escape;
    public float interactDistance = 2.5f;

    [Header("🖼️ Newspaper UI References")]
    [Tooltip("Panel หลักของหน้าต่างแสดงหนังสือพิมพ์")]
    [SerializeField] private GameObject newspaperPanel;
    [Tooltip("UI Image ที่ใช้แสดงภาพหนังสือพิมพ์กลางจอ")]
    [SerializeField] private Image newspaperDisplayImage;
    [Tooltip("เป้าเล็งตรงกลางจอของผู้เล่น (จะถูกซ่อนขณะอ่าน)")]
    [SerializeField] private GameObject playerCrosshairUI;

    [Header("🌐 Localization Sprites (ภาพตามภาษา)")]
    [Tooltip("ใช้ Asset Table จาก Unity Localization (ถ้าใส่ช่องนี้ ระบบจะดึงจากตารางแปลอัตโนมัติ)")]
    [SerializeField] private LocalizedSprite localizedNewspaperSprite;
    [Tooltip("ภาพหนังสือพิมพ์ภาษาไทย (ใช้สำรองหากไม่ได้ผูก Asset Table)")]
    [SerializeField] private Sprite thaiNewspaperSprite;
    [Tooltip("ภาพหนังสือพิมพ์ภาษาอังกฤษ (ใช้สำรองหากไม่ได้ผูก Asset Table)")]
    [SerializeField] private Sprite englishNewspaperSprite;

    [Header("🔎 Zoom & Drag Pan Settings")]
    [Tooltip("เปอร์เซ็นต์การซูมเริ่มต้นเมื่อกดคลิกซ้ายครั้งแรก")]
    [SerializeField] private float defaultZoomPercent = 180f;
    [Tooltip("เปอร์เซ็นต์การซูมต่ำสุดเมื่อเลื่อนเมาส์กลางลง")]
    [SerializeField] private float minZoomPercent = 120f;
    [Tooltip("เปอร์เซ็นต์การซูมสูงสุดเมื่อเลื่อนเมาส์กลางขึ้น")]
    [SerializeField] private float maxZoomPercent = 300f;
    [Tooltip("ความเร็วในการปรับ % ต่อการหมุนลูกกลิ้ง 1 ขั้น")]
    [SerializeField] private float scrollZoomStep = 15f;
    [Tooltip("ความเร็วในการลากเลื่อนภาพตอนซูม")]
    [SerializeField] private float dragPanSpeed = 1.0f;
    [Tooltip("ความนุ่มนวลในการซูมและเลื่อนภาพ")]
    [SerializeField] private float zoomSmoothSpeed = 14f;

    [Header("Events (Optional)")]
    public UnityEvent OnOpenNewspaper;
    public UnityEvent OnCloseNewspaper;

    private RectTransform imageRectTransform;
    private Canvas parentCanvas;

    private bool isZoomed = false;
    private float currentZoomPercent;
    private Vector2 targetAnchoredPos = Vector2.zero;

    // ตัวแปรแยกแยะ "คลิกซูม" กับ "คลิกค้างลากเลื่อนภาพ"
    private Vector2 mouseDownPos;
    private Vector2 lastMousePos;
    private bool isDragging = false;
    private const float DragThresholdPixels = 8f;

    private AsyncOperationHandle<Sprite> currentLoadHandle;

    private void Start()
    {
        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
        }

        if (newspaperDisplayImage != null)
        {
            imageRectTransform = newspaperDisplayImage.GetComponent<RectTransform>();
            parentCanvas = newspaperDisplayImage.canvas;
        }

        if (newspaperPanel != null)
        {
            newspaperPanel.SetActive(false);
        }

        currentZoomPercent = defaultZoomPercent;
        SetHighlight(false);
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;

        if (activeNewspaper == this)
        {
            ForceCloseImmediate();
        }
    }

    private void OnLocaleChanged(Locale newLocale)
    {
        if (activeNewspaper == this && IsReadingNewspaper)
        {
            ApplyLocalizedSprite();
        }
    }

    public void SetHighlight(bool active)
    {
        if (IsReadingNewspaper) active = false;

        if (promptUI != null)
        {
            promptUI.SetActive(active);
        }

        if (outlineComponent != null)
        {
            outlineComponent.enabled = active;
        }
    }

    public void OpenNewspaper()
    {
        if (IsReadingNewspaper) return;

        IsReadingNewspaper = true;
        activeNewspaper = this;
        SetHighlight(false);

        // รีเซ็ตสถานะซูมและตำแหน่งกลับเป็นค่าเริ่มต้น (100% กลางจอ)
        isZoomed = false;
        isDragging = false;
        targetAnchoredPos = Vector2.zero;
        currentZoomPercent = Mathf.Clamp(defaultZoomPercent, minZoomPercent, maxZoomPercent);

        if (imageRectTransform != null)
        {
            imageRectTransform.localScale = Vector3.one;
            imageRectTransform.anchoredPosition = Vector2.zero;
        }

        ApplyLocalizedSprite();

        if (newspaperPanel != null) newspaperPanel.SetActive(true);
        if (playerCrosshairUI != null) playerCrosshairUI.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnOpenNewspaper?.Invoke();
    }

    public void CloseNewspaper()
    {
        if (activeNewspaper != this) return;

        IsReadingNewspaper = false;
        JustClosedThisFrame = true;
        activeNewspaper = null;
        isZoomed = false;
        isDragging = false;

        if (newspaperPanel != null) newspaperPanel.SetActive(false);
        if (playerCrosshairUI != null) playerCrosshairUI.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        OnCloseNewspaper?.Invoke();
    }

    private void ForceCloseImmediate()
    {
        IsReadingNewspaper = false;
        activeNewspaper = null;
        isZoomed = false;
        isDragging = false;
        if (newspaperPanel != null) newspaperPanel.SetActive(false);
    }

    private void Update()
    {
        if (activeNewspaper != this || !IsReadingNewspaper) return;

        // 1. กด Esc เพื่อออกโดยไม่เปิดเมนูหยุดเกม
        if (Input.GetKeyDown(exitKey))
        {
            CloseNewspaper();
            return;
        }

        // 2. จัดการคลิกซ้าย: แยกระหว่าง "คลิกสั้นๆ เพื่อซูมเข้า/ออก" กับ "คลิกค้างลากเพื่อเลื่อนภาพ"
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPos = Input.mousePosition;
            lastMousePos = Input.mousePosition;
            isDragging = false;
        }
        else if (Input.GetMouseButton(0))
        {
            if (isZoomed)
            {
                if (!isDragging && Vector2.Distance(Input.mousePosition, mouseDownPos) >= DragThresholdPixels)
                {
                    isDragging = true;
                }

                if (isDragging)
                {
                    ApplyMouseDragDelta((Vector2)Input.mousePosition - lastMousePos);
                }
            }
            lastMousePos = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            // ถ้าไม่ได้ลากภาพ (เป็นแค่การคลิกปกติ) ให้สลับ ซูมเข้า <-> ซูมออก
            if (!isDragging)
            {
                isZoomed = !isZoomed;
                if (!isZoomed)
                {
                    targetAnchoredPos = Vector2.zero;
                }
            }
            isDragging = false;
        }

        // รองรับการกดคลิกขวาค้างเพื่อลากเลื่อนภาพตอนซูมด้วย (เผื่อผู้เล่นถนัดคลิกขวา)
        if (isZoomed)
        {
            if (Input.GetMouseButtonDown(1))
            {
                lastMousePos = Input.mousePosition;
            }
            else if (Input.GetMouseButton(1))
            {
                ApplyMouseDragDelta((Vector2)Input.mousePosition - lastMousePos);
                lastMousePos = Input.mousePosition;
            }
        }

        // 3. หมุนลูกกลิ้งเมาส์กลางเพื่อปรับ % การซูม (ทำงานตอนซูมอยู่)
        float scroll = Input.mouseScrollDelta.y;
        if (isZoomed && Mathf.Abs(scroll) > 0.01f)
        {
            currentZoomPercent += scroll * scrollZoomStep;
            currentZoomPercent = Mathf.Clamp(currentZoomPercent, minZoomPercent, maxZoomPercent);
        }

        UpdateZoomAndPan();
    }

    private void ApplyMouseDragDelta(Vector2 screenDelta)
    {
        float scaleFactor = (parentCanvas != null && parentCanvas.scaleFactor > 0f) ? parentCanvas.scaleFactor : 1f;
        targetAnchoredPos += (screenDelta / scaleFactor) * dragPanSpeed;
    }

    private void LateUpdate()
    {
        if (JustClosedThisFrame)
        {
            JustClosedThisFrame = false;
        }
    }

    private void UpdateZoomAndPan()
    {
        if (imageRectTransform == null) return;

        float targetScaleValue = isZoomed ? (currentZoomPercent / 100f) : 1f;
        Vector3 targetScale = new Vector3(targetScaleValue, targetScaleValue, 1f);

        if (isZoomed)
        {
            // จำกัดขอบเขตไม่ให้ลากภาพหลุดออกนอกหน้าจอ
            Vector2 rectSize = imageRectTransform.rect.size;
            Vector2 scaledSize = rectSize * targetScaleValue;

            RectTransform parentRect = imageRectTransform.parent as RectTransform;
            Vector2 viewportSize = (parentRect != null) ? parentRect.rect.size : new Vector2(Screen.width, Screen.height);

            float maxPanX = Mathf.Max((scaledSize.x - viewportSize.x) * 0.5f + 40f, (scaledSize.x - rectSize.x) * 0.4f);
            float maxPanY = Mathf.Max((scaledSize.y - viewportSize.y) * 0.5f + 40f, (scaledSize.y - rectSize.y) * 0.4f);

            targetAnchoredPos.x = Mathf.Clamp(targetAnchoredPos.x, -maxPanX, maxPanX);
            targetAnchoredPos.y = Mathf.Clamp(targetAnchoredPos.y, -maxPanY, maxPanY);
        }
        else
        {
            targetAnchoredPos = Vector2.zero;
        }

        imageRectTransform.localScale = Vector3.Lerp(
            imageRectTransform.localScale,
            targetScale,
            Time.unscaledDeltaTime * zoomSmoothSpeed
        );

        imageRectTransform.anchoredPosition = Vector2.Lerp(
            imageRectTransform.anchoredPosition,
            targetAnchoredPos,
            Time.unscaledDeltaTime * zoomSmoothSpeed
        );
    }

    private void ApplyLocalizedSprite()
    {
        if (newspaperDisplayImage == null) return;

        if (localizedNewspaperSprite != null && !localizedNewspaperSprite.IsEmpty)
        {
            if (currentLoadHandle.IsValid())
            {
                UnityEngine.AddressableAssets.Addressables.Release(currentLoadHandle);
            }

            currentLoadHandle = localizedNewspaperSprite.LoadAssetAsync();
            currentLoadHandle.Completed += handle =>
            {
                if (handle.Status == AsyncOperationStatus.Succeeded && newspaperDisplayImage != null)
                {
                    newspaperDisplayImage.sprite = handle.Result;
                }
            };
            return;
        }

        string langCode = "en";
        if (LocalizationSettings.SelectedLocale != null)
        {
            langCode = LocalizationSettings.SelectedLocale.Identifier.Code.ToLower();
        }

        if (langCode.StartsWith("th") && thaiNewspaperSprite != null)
        {
            newspaperDisplayImage.sprite = thaiNewspaperSprite;
        }
        else if (englishNewspaperSprite != null)
        {
            newspaperDisplayImage.sprite = englishNewspaperSprite;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}