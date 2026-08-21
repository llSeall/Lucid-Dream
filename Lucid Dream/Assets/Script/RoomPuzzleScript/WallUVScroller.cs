using UnityEngine;

public class WallUVScroller : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Renderer surfaceRenderer;

    [Header("Scroll")]
    [SerializeField] private float scrollSpeed = 1.0f;

    [SerializeField]
    private Vector3 scrollDirection = new Vector3(0, 0, 1);

    private Material surfaceMaterial;

    private static readonly int ScrollOffsetID =
        Shader.PropertyToID("_ScrollOffset");

    private Vector3 currentOffset;

    void Awake()
    {
        if (surfaceRenderer == null)
            surfaceRenderer = GetComponent<Renderer>();

        if (surfaceRenderer != null)
        {
            surfaceMaterial = surfaceRenderer.material;

            if (!surfaceMaterial.HasProperty(ScrollOffsetID))
            {
                Debug.LogError(
                    $"Material '{surfaceMaterial.name}' ไม่มี _ScrollOffset"
                );
            }
        }
    }

    // ✨ เรียกใช้ใน Update() ของผู้เล่น
    public void Scroll(float inputAmount)
    {
        if (surfaceMaterial == null)
            return;

        // คำนวณการเคลื่อนที่ตามเวลา
        currentOffset +=
            scrollDirection *
            (inputAmount * scrollSpeed * Time.deltaTime);

        // ✨ วนลูปค่าให้อยู่ในระนาบ 0 ถึง 1 เสมอ ป้องกัน float เพี้ยนเมื่อสะสมตัวเลขมากไป
        currentOffset.x = Mathf.Repeat(currentOffset.x, 1f);
        currentOffset.y = Mathf.Repeat(currentOffset.y, 1f);
        currentOffset.z = Mathf.Repeat(currentOffset.z, 1f);

        surfaceMaterial.SetVector(
            ScrollOffsetID,
            currentOffset
        );
    }
}