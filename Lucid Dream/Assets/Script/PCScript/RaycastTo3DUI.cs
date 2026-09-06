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

            Vector2 uiScreenPosition = new Vector2(
                uv.x * uiCamera.pixelWidth,
                uv.y * uiCamera.pixelHeight
            );

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

            // ✨ สร้าง PointerEventData โดยระบุเฉพาะ property ที่ writable ได้
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
                // ✨ ใส่ข้อมูล RaycastResult เพื่อให้ Unity รู้ว่าใช้กล้องตัวไหนโดยอัตโนมัติ
                pointerData.pointerCurrentRaycast = results[0];
                targetUI = results[0].gameObject;
            }

            ProcessHover(targetUI, pointerData);
            ProcessClick(targetUI, pointerData, results.Count > 0 ? results[0] : new RaycastResult());
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

    private void ProcessClick(GameObject targetUI, PointerEventData pointerData, RaycastResult raycastResult)
    {
        if (targetUI == null) return;

        // กดเม้าส์ซ้ายลง
        if (Input.GetMouseButtonDown(0))
        {
            pointerData.pointerPressRaycast = raycastResult;
            pointerData.pointerPress = targetUI;
            ExecuteEvents.ExecuteHierarchy(targetUI, pointerData, ExecuteEvents.pointerDownHandler);
            Debug.Log($"👇 Press Down บน UI: <color=yellow>{targetUI.name}</color>");
        }

        // ปล่อยเม้าส์ซ้าย
        if (Input.GetMouseButtonUp(0))
        {
            ExecuteEvents.ExecuteHierarchy(targetUI, pointerData, ExecuteEvents.pointerUpHandler);
            ExecuteEvents.ExecuteHierarchy(targetUI, pointerData, ExecuteEvents.pointerClickHandler);
            Debug.Log($"✅ Executed Click บน UI: <color=green>{targetUI.name}</color>");
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