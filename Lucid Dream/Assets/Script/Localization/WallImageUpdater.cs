using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;

public class WallImageUpdater : MonoBehaviour
{
    [Header("🎯 Target Renderers")]
    [Tooltip("ใส่ SpriteRenderer หากเป็นป้าย 2D/3D ในฉาก")]
    [SerializeField] private SpriteRenderer wallSpriteRenderer;
    [Tooltip("ใส่ UI Image หากเป็นภาพบน Canvas")]
    [SerializeField] private Image wallImageUI;

    [Header("🌐 Localized Asset")]
    [Tooltip("ลาก Asset Table ของ Sprite มาใส่ที่นี่")]
    [SerializeField] private LocalizedSprite localizedSprite;

    private void OnEnable()
    {
        // ดักฟัง Event เมื่อเปลี่ยนภาษา (เปลี่ยนจาก StringChanged เป็น AssetChanged)
        localizedSprite.AssetChanged += OnAssetChanged;
    }

    private void OnDisable()
    {
        localizedSprite.AssetChanged -= OnAssetChanged;
    }

    private void OnAssetChanged(Sprite translatedSprite)
    {
        if (wallSpriteRenderer != null && translatedSprite != null)
        {
            // 1. เก็บขนาด Bounds เดิมของ Sprite ก่อนเปลี่ยนภาพ
            Vector2 originalWorldSize = wallSpriteRenderer.bounds.size;

            // 2. เปลี่ยนสไปร์ทเป็นภาพแปลภาษา
            wallSpriteRenderer.sprite = translatedSprite;

            // 3. คำนวณหาอัตราส่วนเพื่อปรับ localScale ของ GameObject ให้ตรงกับขนาดเดิม
            Vector2 newSpriteSize = translatedSprite.bounds.size;

            if (newSpriteSize.x > 0 && newSpriteSize.y > 0)
            {
                Vector3 scale = wallSpriteRenderer.transform.localScale;
                scale.x *= (originalWorldSize.x / (newSpriteSize.x * wallSpriteRenderer.transform.localScale.x));
                scale.y *= (originalWorldSize.y / (newSpriteSize.y * wallSpriteRenderer.transform.localScale.y));
                wallSpriteRenderer.transform.localScale = scale;
            }
        }
    }
}