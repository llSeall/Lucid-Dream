using UnityEngine;

public class MirrorReflectionController : MonoBehaviour
{
    [Header("🖼️ Sprite References")]
    [SerializeField] private SpriteRenderer faceSpriteRenderer; // SpriteRenderer ของหน้าสะท้อน
    [SerializeField] private Sprite[] faceSprites;              // ใส่ 3 สไปรท์: [0]=Low, [1]=Medium, [2]=High

    [Header("🎥 Player References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Transform playerCamera;

    [Header("📏 Detection Settings")]
    [SerializeField] private float maxDistance = 4f;     // ระยะเริ่มเห็นเงาหน้าลางๆ
    [SerializeField] private float minDistance = 1.2f;   // ระยะที่หน้าชัด 100%
    [Range(0f, 1f)]
    [SerializeField] private float lookThreshold = 0.5f; // ค่ามุมการจ้องมอง (ยิ่งเข้าใกล้ 1 ยิ่งต้องมองตรงจอมอถึงจะเห็น)
    [SerializeField] private float fadeSpeed = 4f;       // ความเร็วในการจางเข้า-ออก

    private float targetAlpha = 0f;
    private Color currentColor;

    private void Start()
    {
        if (faceSpriteRenderer != null)
        {
            currentColor = faceSpriteRenderer.color;
            currentColor.a = 0f;
            faceSpriteRenderer.color = currentColor;
        }

        if (playerTransform == null && Camera.main != null)
        {
            playerTransform = Camera.main.transform.root;
            playerCamera = Camera.main.transform;
        }

        UpdateFaceSprite();
    }

    private void Update()
    {
        if (playerTransform == null || playerCamera == null || faceSpriteRenderer == null) return;

        UpdateFaceSprite();

        // 1. คำนวณระยะห่าง
        float distance = Vector3.Distance(playerTransform.position, transform.position);

        // 2. คำนวณทิศทางสายตาผู้เล่นว่ามองมาทางกระจกหรือไม่ (Dot Product)
        Vector3 dirToMirror = (transform.position - playerCamera.position).normalized;
        float dot = Vector3.Dot(playerCamera.forward, dirToMirror);

        // 3. ตรวจสอบเงื่อนไขการมองเห็น
        if (distance <= maxDistance && dot > lookThreshold)
        {
            float distanceFactor = Mathf.InverseLerp(maxDistance, minDistance, distance);
            float lookFactor = Mathf.InverseLerp(lookThreshold, 1f, dot);

            targetAlpha = distanceFactor * lookFactor;
        }
        else
        {
            targetAlpha = 0f; // ซ่อนหน้าถ้าอยู่ไกล หรือไม่ได้จ้องกระจก
        }

        // 4. ปรับค่า Alpha แบบนุ่มนวล
        currentColor.a = Mathf.Lerp(currentColor.a, targetAlpha, Time.deltaTime * fadeSpeed);
        faceSpriteRenderer.color = currentColor;
    }

    private void UpdateFaceSprite()
    {
        if (StressManager.Instance == null || faceSprites == null || faceSprites.Length < 3) return;

        int stageIndex = (int)StressManager.Instance.GetCurrentStage();

        if (faceSpriteRenderer.sprite != faceSprites[stageIndex])
        {
            faceSpriteRenderer.sprite = faceSprites[stageIndex];
        }
    }
}