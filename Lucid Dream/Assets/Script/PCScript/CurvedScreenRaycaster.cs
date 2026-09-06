using UnityEngine;
using UnityEngine.EventSystems;

public class CurvedScreenRaycaster : MonoBehaviour
{
    [SerializeField] private Camera uiCamera;          // กล้องที่ถ่าย Canvas
    [SerializeField] private RectTransform canvasRect;  // RectTransform ของ Canvas
    [SerializeField] private Camera mainCamera;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // ตรวจสอบว่าเล็งถูกโมเดลหน้าจอ 3D หรือไม่
                if (hit.collider.transform == transform)
                {
                    Vector2 uv = hit.textureCoord; // ดึงพิกัด UV (0 ถึง 1) จากจุดที่ยิงโดนโมเดลโค้ง

                    // แปลงพิกัด UV เป็นพิกัด Screen บน Canvas 2D
                    Vector2 pixelPos = new Vector2(
                        uv.x * canvasRect.rect.width,
                        uv.y * canvasRect.rect.height
                    );

                    // ส่ง Event คลิกไปยังระบบ UI
                    PointerEventData eventData = new PointerEventData(EventSystem.current)
                    {
                        position = pixelPos
                    };

                    // สามารถใช้ ExecuteEvents.Execute ต่อเพื่อส่งคำสั่ง Click ได้
                }
            }
        }
    }
}