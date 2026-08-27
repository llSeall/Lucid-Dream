using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ProximityFadeSprite : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Transform ของผู้เล่น")]
    public Transform playerTransform;

    [Header("Distance Settings")]
    [Tooltip("ระยะห่างที่สไปร์ทเริ่มทยอยเลือนหาย (Alpha เริ่มลดลง)")]
    public float maxDistance = 8f;

    [Tooltip("ระยะห่างที่สไปร์ทเลือนหายจนหมดและปิด Object (Alpha = 0)")]
    public float minDistance = 2f;

    [Header("Options")]
    [Tooltip("ปิด GameObject ทันทีเมื่อเดินเข้าใกล้จนเลือนหายหมด")]
    public bool deactivateOnHidden = true;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;

        // ค้นหาวัตถุที่มี Tag "Player" อัตโนมัติหากไม่ได้ลากใส่ Inspector
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // คำนวณค่า Alpha ตามระยะห่าง (ยิ่งใกล้ ค่า alpha ยิ่งเข้าใกล้ 0)
        float alphaProgress = Mathf.InverseLerp(minDistance, maxDistance, distance);

        // อัปเดตค่าความโปร่งแสงของ Sprite
        Color newColor = originalColor;
        newColor.a = alphaProgress * originalColor.a;
        spriteRenderer.color = newColor;

        // เมื่อผู้เล่นเดินเข้าใกล้จนถึงระยะ minDistance ให้สั่งปิด Object
        if (distance <= minDistance && deactivateOnHidden)
        {
            gameObject.SetActive(false);
        }
    }

    // วาดวงกลมรัศมีในหน้า Scene View ช่วยให้มองเห็นระยะที่ตั้งไว้ได้ง่ายขึ้น
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow; // วงกลมเหลือง = ระยะที่เริ่มเลือนหาย
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        Gizmos.color = Color.red; // วงกลมแดง = ระยะที่หายไปสนิทและปิดออปเจกต์
        Gizmos.DrawWireSphere(transform.position, minDistance);
    }
}