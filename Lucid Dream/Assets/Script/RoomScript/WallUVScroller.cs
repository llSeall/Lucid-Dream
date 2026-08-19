using UnityEngine;

public class WallUVScroller : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Renderer surfaceRenderer;

    [Tooltip("ความเร็วในการเลื่อนลาย")]
    [SerializeField] private float scrollSpeed = 1.0f;

    [Tooltip("ปรับทิศทาง UV อิสระ (X และ Y)\n- กำแพงส่วนใหญ่: (1, 0) หรือ (-1, 0)\n- พื้นส่วนใหญ่: (0, 1) หรือ (0, -1)")]
    [SerializeField] private Vector2 scrollDirection = new Vector2(1, 0);

    private Material surfaceMaterial;
    private int texturePropID;

    void Awake()
    {
        if (surfaceRenderer == null) surfaceRenderer = GetComponent<Renderer>();

        if (surfaceRenderer != null)
        {
            surfaceMaterial = surfaceRenderer.material;

            // ตรวจสอบชื่อช่อง Texture ว่าเป็น URP (_BaseMap) หรือ Built-in (_MainTex)
            if (surfaceMaterial.HasProperty("_BaseMap"))
            {
                texturePropID = Shader.PropertyToID("_BaseMap");
            }
            else
            {
                texturePropID = Shader.PropertyToID("_MainTex");
            }
        }
    }

    /// <summary>
    /// ฟังก์ชันสั่งให้ UV เลื่อนภาพ
    /// </summary>
    public void Scroll(float inputAmount)
    {
        if (surfaceMaterial == null) return;

        // อ่านค่า Offset ปัจจุบัน (รองรับทั้ง URP และ Built-in)
        Vector2 currentOffset = surfaceMaterial.GetTextureOffset(texturePropID);

        // คำนวณ Offset ใหม่ตาม Vector2 scrollDirection ที่กำหนดใน Inspector
        currentOffset += scrollDirection * (inputAmount * scrollSpeed * Time.deltaTime);

        // บันทึกค่า Offset กลับไปที่ Material
        surfaceMaterial.SetTextureOffset(texturePropID, currentOffset);
    }
}