using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class StalkerFootstepsZone : MonoBehaviour
{
    [Header("🛤️ Corridor Points (กำหนดหัว-ท้ายทางเดิน)")]
    [Tooltip("จุดเริ่มต้นทางเดิน (ด้านต้นทาง - จุดที่เสียงจะตามมาจากด้านนี้)")]
    [SerializeField] private Transform startPoint;
    [Tooltip("จุดปลายทางทางเดิน (ด้านปลายทาง - ทิศทางที่ผู้เล่นกำลังเดินไป)")]
    [SerializeField] private Transform endPoint;

    [Header("👁️ Player & Camera References")]
    [Tooltip("กล้องของผู้เล่น (หากเว้นว่างไว้ ระบบจะดึง Camera.main ให้อัตโนมัติ)")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private string playerTag = "Player";

    [Header("🔊 Footstep Sound Settings")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] footstepClips;
    [Tooltip("ระยะห่างที่เสียงเท้าจะเดินตามหลังผู้เล่น (เมตร)")]
    [SerializeField] private float followDistance = 2.0f;
    [Tooltip("ระยะทางเดินสะสม (เมตร) ต่อการเกิดเสียงก้าวเท้า 1 ครั้ง (แนะนำ 1.2 - 1.8)")]
    [SerializeField] private float stepDistance = 1.5f;
    [Range(0f, 1f)][SerializeField] private float volume = 1.0f;

    [Header("⚙️ Eye Detection Settings (เกณฑ์การเช็กสายตา)")]
    [Tooltip("เกณฑ์การมองไปทาง End: ยิ่งเข้าใกล้ 1 ยิ่งต้องมองตรงไปข้างหน้าเท่านั้น ถ้าหันหน้ากลับมามองทาง Start เสียงจะหยุดทันที (ค่าแนะนำ: -0.1)")]
    [Range(-1f, 1f)][SerializeField] private float lookThreshold = -0.1f;

    [Header("🧪 Debug & Test Options")]
    [Tooltip("ติ๊กถูกช่องนี้เพื่อบังคับเล่นเสียงแบบ 2D (สำหรับทดสอบ)")]
    [SerializeField] private bool force2DTest = false;
    [SerializeField] private bool enableDebugLogs = true;

    private Transform playerTransform;
    private Vector3 lastPlayerPos;
    private bool isPlayerInZone = false;
    private float accumulatedDistance = 0f;

    private void Awake()
    {
        GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (footstepAudioSource != null)
        {
            footstepAudioSource.playOnAwake = false;
            SetupAudioSettings();
        }
    }

    private void SetupAudioSettings()
    {
        if (footstepAudioSource == null) return;

        if (force2DTest)
        {
            footstepAudioSource.spatialBlend = 0f; // 2D Sound
        }
        else
        {
            footstepAudioSource.spatialBlend = 1.0f; // 3D Sound
            footstepAudioSource.minDistance = 1.0f;
            footstepAudioSource.maxDistance = 30f;
            footstepAudioSource.rolloffMode = AudioRolloffMode.Linear;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        CheckAndSetPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isPlayerInZone || playerTransform == null)
        {
            CheckAndSetPlayer(other);
        }
    }

    private void CheckAndSetPlayer(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            isPlayerInZone = true;
            playerTransform = other.transform.root;
            lastPlayerPos = playerTransform.position;
            accumulatedDistance = 0f;
            SetupAudioSettings();

            if (playerCamera == null && Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }

            if (enableDebugLogs) Debug.Log("👟 [StalkerZone] ตรวจพบตัวละครหลักเข้าโซนแล้ว!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) || other.transform.root.CompareTag(playerTag))
        {
            isPlayerInZone = false;
            accumulatedDistance = 0f;
            if (enableDebugLogs) Debug.Log("❌ [StalkerZone] ผู้เล่นออกจากโซน");
        }
    }

    private void Update()
    {
        if (!isPlayerInZone || playerTransform == null || startPoint == null || endPoint == null)
            return;

        // 1. คำนวณระยะทางที่เคลื่อนที่ในเฟรมนี้
        Vector3 currentPos = playerTransform.position;
        currentPos.y = 0;
        Vector3 lastPos = lastPlayerPos;
        lastPos.y = 0;

        float distMoved = Vector3.Distance(currentPos, lastPos);

        // 2. หาเวกเตอร์ทิศทางทางเดิน Start -> End
        Vector3 corridorDir = (endPoint.position - startPoint.position).normalized;
        corridorDir.y = 0;
        corridorDir.Normalize();

        // 3. เช็กทิศทางสายตาผู้เล่น
        Transform cam = playerCamera != null ? playerCamera : playerTransform;
        Vector3 camForward = cam.forward;
        camForward.y = 0;
        camForward.Normalize();

        bool isLookingTowardsEnd = Vector3.Dot(camForward, corridorDir) >= lookThreshold;

        if (distMoved > 0.001f)
        {
            Vector3 moveDir = (currentPos - lastPos).normalized;
            bool isMovingTowardsEnd = Vector3.Dot(moveDir, corridorDir) >= -0.6f;

            // 4. เงื่อนไขครบ: เดินมุ่งหน้าไป End + ไม่ได้หันกลับมาจ้องมอง Start
            if (isMovingTowardsEnd && isLookingTowardsEnd)
            {
                accumulatedDistance += distMoved;

                // อัปเดตตำแหน่ง AudioSource ให้ตามหลังผู้เล่น
                if (footstepAudioSource != null)
                {
                    footstepAudioSource.transform.position = playerTransform.position - (corridorDir * followDistance);
                }

                // เมื่อก้าวเดินสะสมระยะทางครบ ให้เกิดเสียงก้าวเท้า
                if (accumulatedDistance >= stepDistance)
                {
                    PlayFootstepSound();
                    accumulatedDistance = 0f;
                }
            }
            else
            {
                accumulatedDistance = 0f;

                if (enableDebugLogs && !isLookingTowardsEnd)
                {
                    Debug.LogWarning("👁️ [StalkerZone] ผู้เล่นหันกลับมามองด้านหลัง เสียงก้าวเท้าจึงหยุด!");
                }
            }

            lastPlayerPos = playerTransform.position;
        }
    }

    private void PlayFootstepSound()
    {
        if (footstepAudioSource == null || footstepClips == null || footstepClips.Length == 0)
        {
            if (enableDebugLogs) Debug.LogError("❌ ลืมใส่ AudioSource หรือ AudioClip ใน Inspector!");
            return;
        }

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip != null)
        {
            footstepAudioSource.pitch = Random.Range(0.85f, 1.15f);
            footstepAudioSource.PlayOneShot(clip, volume);
            if (enableDebugLogs) Debug.Log("🔊 [StalkerZone] เล่นเสียงก้าวเท้าสำเร็จ!");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (startPoint != null && endPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(startPoint.position, endPoint.position);
            Gizmos.DrawWireSphere(startPoint.position, 0.3f);
            Gizmos.DrawWireSphere(endPoint.position, 0.3f);
        }
    }
}