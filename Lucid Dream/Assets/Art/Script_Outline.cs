using UnityEngine;

public class Script_Outline : MonoBehaviour
{
    [Header("🎯 Raycast Settings")]
    [Tooltip("ระยะมองเห็นเส้น Outline และระยะกดโต้ตอบ")]
    public float interactionDistance = 3.5f; // ปรับให้เป็นค่ามาตรฐานเดียวกัน
    public LayerMask interactableLayer = ~0; // กำหนด Layer ที่ต้องการค้นหา

    private Outline _currentOutline;

    void Start()
    {
        // ซ่อน Outline ทั้งหมดในฉากเมื่อเริ่มเกม
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

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            // ✨ ค้นหา Outline ทั้งในตัวเองและ Object แม่
            Outline foundOutline = hit.collider.GetComponentInParent<Outline>();

            if (foundOutline != null)
            {
                if (_currentOutline == foundOutline) return;

                Clear(); // ล้างอันเก่าก่อนเปิดอันใหม่

                _currentOutline = foundOutline;
                _currentOutline.enabled = true;
            }
            else
            {
                Clear();
            }
        }
        else
        {
            Clear();
        }
    }

    void Clear()
    {
        if (_currentOutline != null)
        {
            _currentOutline.enabled = false;
            _currentOutline = null;
        }
    }
}