using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RaycastTo3DUI : MonoBehaviour
{
    [Header("🎥 Camera References")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera uiCamera;

    [Header("🖥️ 3D Screen Setup")]
    [SerializeField] private LayerMask screenLayer;
    [SerializeField] private float maxInteractDistance = 10f;

    [Header("🎨 UI Setup")]
    [SerializeField] private GraphicRaycaster canvasGraphicRaycaster;
    [SerializeField] private RectTransform pcCanvasRect;

    [Header("🔄 Advanced UV Transformations")]
    [SerializeField] private bool invertX = false;
    [SerializeField] private bool invertY = false;
    [SerializeField] private bool swapXY = false;

    [Header("🔴 Visual Debug Pointer")]
    [SerializeField] private RectTransform debugPointer;

    private GameObject currentHoveredUI;
    private GameObject currentDraggedUI;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;

        if (pcCanvasRect == null && canvasGraphicRaycaster != null)
        {
            pcCanvasRect = canvasGraphicRaycaster.GetComponent<RectTransform>();
        }
    }

    private void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxInteractDistance, screenLayer))
        {
            Vector2 uv = hit.textureCoord;

            if (swapXY)
            {
                float temp = uv.x;
                uv.x = uv.y;
                uv.y = temp;
            }

            if (invertX) uv.x = 1f - uv.x;
            if (invertY) uv.y = 1f - uv.y;

            // ✨ คำนวณ Screen Position ผ่าน Canvas Rect โดยตรงเพื่อให้ตำแหน่งตรงเป๊ะ 100%
            Vector2 uiScreenPosition = Vector2.zero;
            if (uiCamera != null && pcCanvasRect != null)
            {
                Vector3 localPos = new Vector3(
                    (uv.x - pcCanvasRect.pivot.x) * pcCanvasRect.rect.width,
                    (uv.y - pcCanvasRect.pivot.y) * pcCanvasRect.rect.height,
                    0f
                );
                Vector3 worldPos = pcCanvasRect.TransformPoint(localPos);
                uiScreenPosition = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);
            }
            else
            {
                uiScreenPosition = new Vector2(uv.x * Screen.width, uv.y * Screen.height);
            }

            // ขยับจุดแดง Debug Pointer
            if (debugPointer != null && pcCanvasRect != null)
            {
                if (!debugPointer.gameObject.activeSelf) debugPointer.gameObject.SetActive(true);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    pcCanvasRect,
                    uiScreenPosition,
                    uiCamera,
                    out Vector2 localPoint
                );

                debugPointer.anchoredPosition = localPoint;
            }

            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = uiScreenPosition,
                button = PointerEventData.InputButton.Left
            };

            List<RaycastResult> results = new List<RaycastResult>();
            if (canvasGraphicRaycaster != null)
            {
                canvasGraphicRaycaster.Raycast(pointerData, results);
            }

            GameObject targetUI = null;

            if (results.Count > 0)
            {
                pointerData.pointerCurrentRaycast = results[0];
                targetUI = results[0].gameObject;
            }

            ProcessHover(targetUI, pointerData);
            ProcessClickAndDrag(targetUI, pointerData, results.Count > 0 ? results[0] : new RaycastResult());
            ProcessScroll(targetUI, pointerData); // ✨ เพิ่มฟังก์ชันส่ง Event ลูกกลิ้ง
        }
        else
        {
            if (debugPointer != null && debugPointer.gameObject.activeSelf)
            {
                debugPointer.gameObject.SetActive(false);
            }

            ClearHover();
        }
    }

    private void ProcessHover(GameObject targetUI, PointerEventData pointerData)
    {
        if (currentHoveredUI != targetUI)
        {
            if (currentHoveredUI != null)
            {
                ExecuteEvents.ExecuteHierarchy(currentHoveredUI, pointerData, ExecuteEvents.pointerExitHandler);
            }

            currentHoveredUI = targetUI;

            if (currentHoveredUI != null)
            {
                ExecuteEvents.ExecuteHierarchy(currentHoveredUI, pointerData, ExecuteEvents.pointerEnterHandler);
            }
        }
    }

    private void ProcessClickAndDrag(GameObject targetUI, PointerEventData pointerData, RaycastResult raycastResult)
    {
        if (targetUI == null && currentDraggedUI == null) return;

        // กดเมาส์ซ้ายลง (Begin Drag & Press Down)
        if (Input.GetMouseButtonDown(0))
        {
            pointerData.pointerPressRaycast = raycastResult;
            pointerData.pointerPress = targetUI;
            pointerData.rawPointerPress = targetUI;

            ExecuteEvents.ExecuteHierarchy(targetUI, pointerData, ExecuteEvents.pointerDownHandler);

            // เริ่มต้นระบบ Drag
            currentDraggedUI = ExecuteEvents.GetEventHandler<IDragHandler>(targetUI);
            if (currentDraggedUI != null)
            {
                pointerData.pointerDrag = currentDraggedUI;
                ExecuteEvents.Execute(currentDraggedUI, pointerData, ExecuteEvents.beginDragHandler);
            }
        }

        // ขณะถือเมาส์ซ้ายลาก (Dragging)
        if (Input.GetMouseButton(0) && currentDraggedUI != null)
        {
            pointerData.pointerDrag = currentDraggedUI;
            ExecuteEvents.Execute(currentDraggedUI, pointerData, ExecuteEvents.dragHandler);
        }

        // ปล่อยเมาส์ซ้าย (End Drag & Press Up)
        if (Input.GetMouseButtonUp(0))
        {
            if (currentDraggedUI != null)
            {
                pointerData.pointerDrag = currentDraggedUI;
                ExecuteEvents.Execute(currentDraggedUI, pointerData, ExecuteEvents.endDragHandler);
                currentDraggedUI = null;
            }

            if (targetUI != null)
            {
                ExecuteEvents.ExecuteHierarchy(targetUI, pointerData, ExecuteEvents.pointerUpHandler);
                ExecuteEvents.ExecuteHierarchy(targetUI, pointerData, ExecuteEvents.pointerClickHandler);
            }
        }
    }

    // ✨ ฟังก์ชันจัดการ Scroll Wheel (ลูกกลิ้งเมาส์)
    private void ProcessScroll(GameObject targetUI, PointerEventData pointerData)
    {
        float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scrollDelta) > 0.001f && targetUI != null)
        {
            pointerData.scrollDelta = new Vector2(0, scrollDelta);
            ExecuteEvents.ExecuteHierarchy(targetUI, pointerData, ExecuteEvents.scrollHandler);
        }
    }

    private void ClearHover()
    {
        if (currentHoveredUI != null)
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current);
            ExecuteEvents.ExecuteHierarchy(currentHoveredUI, pointerData, ExecuteEvents.pointerExitHandler);
            currentHoveredUI = null;
        }
    }
}