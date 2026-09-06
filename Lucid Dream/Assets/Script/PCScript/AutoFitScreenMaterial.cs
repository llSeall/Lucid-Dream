using UnityEngine;

[ExecuteInEditMode]
public class AutoFitScreenMaterial : MonoBehaviour
{
    [Header("🎯 Target Screen Renderer")]
    [SerializeField] private Renderer screenRenderer;
    [SerializeField] private int materialIndex = 0; // ลำดับ Material ของหน้าจอ (ปกติคือ 0)

    [Header("⚙️ Auto-Calculated Properties")]
    [SerializeField] private Vector2 calculatedTiling = Vector2.one;
    [SerializeField] private Vector2 calculatedOffset = Vector2.zero;

    [ContextMenu("✨ Auto Fit Material To Screen")]
    public void AutoFitUVs()
    {
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("ไม่พบ MeshFilter บน Object นี้!");
            return;
        }

        Vector2[] uvs = mf.sharedMesh.uv;
        if (uvs.Length == 0)
        {
            Debug.LogError("โมเดลนี้ไม่มีพิกัด UV!");
            return;
        }

        // หาขอบเขต Min / Max ของพิกัด UV บนโมเดล
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var uv in uvs)
        {
            if (uv.x < minX) minX = uv.x;
            if (uv.x > maxX) maxX = uv.x;
            if (uv.y < minY) minY = uv.y;
            if (uv.y > maxY) maxY = uv.y;
        }

        float width = maxX - minX;
        float height = maxY - minY;

        // คำนวณ Tiling และ Offset ให้ครอบคลุมเฉพาะโซน UV หน้าจอ
        if (width > 0 && height > 0)
        {
            calculatedTiling = new Vector2(1f / width, 1f / height);
            calculatedOffset = new Vector2(-minX * calculatedTiling.x, -minY * calculatedTiling.y);

            ApplyToMaterial();
            Debug.Log($"✅ ปรับ Tiling เป็น {calculatedTiling} และ Offset เป็น {calculatedOffset} เรียบร้อย!");
        }
    }

    public void ApplyToMaterial()
    {
        if (screenRenderer == null) screenRenderer = GetComponent<Renderer>();
        if (screenRenderer != null)
        {
            Material mat = Application.isPlaying ? screenRenderer.materials[materialIndex] : screenRenderer.sharedMaterials[materialIndex];
            if (mat != null)
            {
                mat.mainTextureScale = calculatedTiling;
                mat.mainTextureOffset = calculatedOffset;
            }
        }
    }

    private void OnValidate()
    {
        ApplyToMaterial();
    }
}