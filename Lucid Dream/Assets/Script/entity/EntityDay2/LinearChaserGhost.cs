using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LinearChaserGhost : MonoBehaviour
{
    [Header("🛤️ Path Settings (เส้นทางแบบทะลุกำแพง)")]
    [Tooltip("ลาก GameObject จุด Waypoints ที่ต้องการให้ผีวิ่งผ่านตามลำดับ")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [Tooltip("ความเร็วในการวิ่งของผี")]
    [SerializeField] private float moveSpeed = 12f;
    [Tooltip("ระยะห่างจากจุด Waypoint เพื่อเปลี่ยนไปจุดถัดไป")]
    [SerializeField] private float waypointReachThreshold = 0.5f;

    [Header("🎯 Target & Catch Settings")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("ระยะจับผู้เล่น (ถ้าผีเข้าใกล้ผู้เล่นในระยะนี้จะโดน Jumpscare ทันที)")]
    [SerializeField] private float catchDistance = 2.5f;

    [Header("🔊 Audio Settings ✨")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("ไฟล์เสียงนกหวีดเป่าดังขึ้นตอนสปอนต์")]
    [SerializeField] private AudioClip whistleClip;

    [Header("😱 Jumpscare Settings ✨")]
    [SerializeField] private Sprite[] jumpscareFrames;
    [SerializeField] private float jumpscareFrameRate = 12f;
    [SerializeField] private AudioClip jumpscareScreamClip;
    [SerializeField] private float jumpscareDuration = 1.5f;

    [Header("2.5D Sprite & Animation (Optional)")]
    [SerializeField] private SpriteRenderer ghostSprite;
    [SerializeField] private bool defaultFacingLeft = false;

    private int currentWaypointIndex = 0;
    private bool isActive = false;
    private bool isPlayerCaught = false;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // ปิดการทำงานของผีตัวนี้ไว้ก่อนตอนเริ่มเกม
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
    }

    /// <summary>
    /// เรียกทำงานจาก LinearGhostTrigger เมื่อผู้เล่นเหยียบทริกเกอร์
    /// </summary>
    public void ActivateGhost()
    {
        if (isActive) return;

        gameObject.SetActive(true);
        isActive = true;
        currentWaypointIndex = 0;

        // วางผีไว้ที่จุดเริ่มต้น (Waypoint ที่ 0)
        if (waypoints.Count > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
        }

        // เล่นเสียงนกหวีด
        if (audioSource != null && whistleClip != null)
        {
            audioSource.PlayOneShot(whistleClip, 1.0f);
        }

        Debug.Log("👻 [LinearChaserGhost] ผีถูกสปอนต์แล้ว! กำลังวิ่งตามเส้นทาง...");
    }

    private void Update()
    {
        if (!isActive || isPlayerCaught) return;

        // 1. ตรวจจับว่าเข้าใกล้ผู้เล่นจนจับได้หรือยัง
        CheckCatchPlayer();

        // 2. เคลื่อนที่ไปตามจุด Waypoint แบบทะลุกำแพง
        MoveAlongPath();
    }

    private void CheckCatchPlayer()
    {
        if (playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= catchDistance)
        {
            TriggerPlayerCaught();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // กรณีผีวิ่งชน Collider ของผู้เล่นโดยตรง
        if (!isPlayerCaught && isActive && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
        {
            TriggerPlayerCaught();
        }
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
        {
            // วิ่งครบทุกจุดตามเส้นทางแล้ว ➔ หายไปเลย (Despawn)
            DespawnGhost();
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        // คำนวณทิศทางและหมุนหน้าผี
        Vector3 targetPos = targetWaypoint.position;
        Vector3 moveDir = (targetPos - transform.position).normalized;

        // เคลื่อนที่ผ่าน Transform โดยไม่ใช้ NavMesh/Rigidbody physics เพื่อให้ทะลุกำแพงได้ 100%
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // กลับด้าน Sprite ตามทิศทางการวิ่ง
        if (ghostSprite != null && moveDir.x != 0)
        {
            ghostSprite.flipX = moveDir.x > 0 ? defaultFacingLeft : !defaultFacingLeft;
        }

        // ถ้าถึงจุด Waypoint ปัจจุบันแล้ว ให้เปลี่ยนเป้าหมายไปจุดถัดไป
        if (Vector3.Distance(transform.position, targetPos) <= waypointReachThreshold)
        {
            currentWaypointIndex++;
        }
    }

    private void TriggerPlayerCaught()
    {
        if (isPlayerCaught) return;
        isPlayerCaught = true;
        isActive = false;

        Debug.Log("<color=red>😱 [LinearChaserGhost] โดนผีใหญ่ชน/จับได้แล้ว! กำลังเริ่มเล่น Jumpscare...</color>");

        StartCoroutine(JumpscareAndRespawnRoutine());
    }

    private IEnumerator JumpscareAndRespawnRoutine()
    {
        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.ShowJumpscareAnimation(jumpscareFrames, jumpscareFrameRate, jumpscareScreamClip);
        }

        yield return new WaitForSeconds(jumpscareDuration);

        if (JumpscareUI.Instance != null)
        {
            JumpscareUI.Instance.HideJumpscare();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDiedInDream();
        }
        else
        {
            if (StressManager.Instance != null)
            {
                StressManager.Instance.IncreaseStressOnDeath();
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void DespawnGhost()
    {
        isActive = false;
        Debug.Log("👻 [LinearChaserGhost] ผีวิ่งสุดเส้นทางแล้ว และหายไปถาวร");
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        // วาดเส้นสีส้มแสดงเส้นทางของผีในหน้า Scene View
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.orange;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i + 1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                Gizmos.DrawWireSphere(waypoints[i].position, 0.3f);
            }
        }
        if (waypoints[waypoints.Count - 1] != null)
        {
            Gizmos.DrawWireSphere(waypoints[waypoints.Count - 1].position, 0.3f);
        }
    }
}