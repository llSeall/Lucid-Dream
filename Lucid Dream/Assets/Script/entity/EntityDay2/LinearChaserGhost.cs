using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LinearChaserGhost : MonoBehaviour
{
    public enum GhostPhase
    {
        Phase1,
        Phase2,
        Phase3
    }

    [Header("🎭 Ghost Phase Settings (เลือกเฟสการทำงานของผี) ✨")]
    [Tooltip("เลือกเฟสปัจจุบันของผี (Phase1 / Phase2 / Phase3)")]
    [SerializeField] private GhostPhase currentPhase = GhostPhase.Phase1;

    public GhostPhase CurrentPhase => currentPhase;

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

    [Header("🎥 Chase Camera Zoom & Proximity Shake ✨")]
    [Tooltip("เปิดใช้งานการซูมออกตลอดช่วงหนีผี + สั่นเมื่อผีเข้าใกล้")]
    [SerializeField] private bool enableChaseCameraEffect = true;

    [Tooltip("ค่า FOV ที่ซูมออกเพิ่มตลอดเวลาหลังเป่านกหวีด (ยิ่งเยอะยิ่งซูมไกล แนะนำ 18 - 30)")]
    [Range(0f, 50f)]
    [SerializeField] private float chaseZoomOutFOV = 20f;

    [Tooltip("ระยะที่กล้องเริ่มสั่นเมื่อผีเข้าใกล้ (เมตร)")]
    [SerializeField] private float shakeMaxDistance = 16f;
    [Tooltip("ระยะที่กล้องสั่นแรงสูงสุดก่อนโดนจับ (เมตร)")]
    [SerializeField] private float shakeMinDistance = 3f;
    [Tooltip("แรงสั่นตำแหน่งกล้องสูงสุด (แนะนำ 0.01 - 0.04 เพื่อไม่ให้เวียนหัว)")]
    [Range(0f, 0.15f)]
    [SerializeField] private float maxShakePositionAmount = 0.025f;
    [Tooltip("แรงเอียง/สั่นมุมกล้องสูงสุดเป็นองศา (แนะนำ 0.3 - 1.2 เพื่อไม่ให้เป็น Motion Sickness)")]
    [Range(0f, 5f)]
    [SerializeField] private float maxShakeRotationAmount = 0.75f;
    [Tooltip("ความถี่การสั่น (ยิ่งต่ำยิ่งนุ่มนวล แนะนำ 5 - 10)")]
    [Range(1f, 25f)]
    [SerializeField] private float shakeFrequency = 8f;

    [Header("🔊 Whistle & Chase Music Settings ✨")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("ไฟล์เสียงนกหวีดเป่าดังขึ้นตอนสปอนต์")]
    [SerializeField] private AudioClip whistleClip;
    [Tooltip("รอนกหวีดจบก่อนเริ่มซูมไกล / สเตมิน่าไม่ลด / เปิดเพลงระทึก (ถ้าปิดจะเริ่มพร้อมเสียงนกหวีดทันที)")]
    [SerializeField] private bool waitWhistleEndForChaseStart = true;

    [Space(6)]
    [Tooltip("ไฟล์เพลงระทึกที่จะเปิดวนลูปตลอดช่วงวิ่งหนีผี")]
    [SerializeField] private AudioClip chaseMusicClip;
    [Tooltip("ระดับความดังสูงสุดของเพลงระทึก")]
    [Range(0f, 1f)]
    [SerializeField] private float chaseMusicVolume = 0.75f;
    [Tooltip("เวลาในการค่อยๆ เร่งเสียงเพลงขึ้น (วินาที)")]
    [SerializeField] private float musicFadeInDuration = 0.4f;
    [Tooltip("เวลาในการค่อยๆ เฟดเสียงเพลงดับลงเมื่อหนีพ้น/เหยียบทริกเกอร์ปิดผี (วินาที)")]
    [SerializeField] private float musicFadeOutDuration = 1.0f;

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
    private bool isChaseStarted = false; // จะเป็น true หลังนกหวีดทำงาน
    private PlayerController3D_InputAction playerController;
    private Coroutine whistleSequenceCoroutine;
    private AudioSource chaseMusicSource;
    private float noiseSeed;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        noiseSeed = Random.Range(0f, 100f);

        // ปิดการทำงานของผีตัวนี้ไว้ก่อนตอนเริ่มเกม
        gameObject.SetActive(false);
    }

    private void Start()
    {
        ResolvePlayerReferences();
    }

    private void ResolvePlayerReferences()
    {
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        if (playerController == null)
        {
            if (playerTransform != null)
                playerController = playerTransform.GetComponent<PlayerController3D_InputAction>();

            if (playerController == null)
                playerController = PlayerController3D_InputAction.Instance != null
                    ? PlayerController3D_InputAction.Instance
                    : FindAnyObjectByType<PlayerController3D_InputAction>();
        }
    }

    /// <summary>
    /// เปลี่ยนเฟสของผีจากภายนอกหรือทริกเกอร์
    /// </summary>
    public void SetPhase(GhostPhase newPhase)
    {
        currentPhase = newPhase;
        Debug.Log($"👻 [LinearChaserGhost] เปลี่ยนเฟสผีเป็น: {currentPhase}");
    }

    /// <summary>
    /// เรียกทำงานจาก LinearGhostTrigger เมื่อผู้เล่นเหยียบทริกเกอร์
    /// </summary>
    public void ActivateGhost()
    {
        if (isActive) return;

        gameObject.SetActive(true);
        isActive = true;
        isChaseStarted = false;
        currentWaypointIndex = 0;

        ResolvePlayerReferences();

        // วางผีไว้ที่จุดเริ่มต้น (Waypoint ที่ 0)
        if (waypoints.Count > 0 && waypoints[0] != null)
        {
            transform.position = waypoints[0].position;
        }

        // เล่นเสียงนกหวีด -> เริ่มโหมดวิ่งไม่ลดสเตมิน่า + ซูมกล้องไกล + เปิดเพลงระทึก
        if (whistleSequenceCoroutine != null)
        {
            StopCoroutine(whistleSequenceCoroutine);
        }
        whistleSequenceCoroutine = StartCoroutine(WhistleAndStartChaseRoutine());

        Debug.Log($"👻 [LinearChaserGhost] ผีถูกสปอนต์แล้ว ({currentPhase})! กำลังวิ่งตามเส้นทาง...");
    }

    private IEnumerator WhistleAndStartChaseRoutine()
    {
        if (audioSource != null && whistleClip != null)
        {
            audioSource.PlayOneShot(whistleClip, 1.0f);

            if (waitWhistleEndForChaseStart && whistleClip.length > 0f)
            {
                yield return new WaitForSeconds(whistleClip.length);
            }
        }

        if (!isActive || isPlayerCaught) yield break;

        isChaseStarted = true;

        ResolvePlayerReferences();
        if (playerController != null)
        {
            playerController.SetChaseInfiniteStamina(true);
        }

        StartChaseMusic();
        Debug.Log("⚡ [LinearChaserGhost] เริ่มช่วงวิ่งหนี: สเตมิน่าไม่ลด + ซูมกล้องไกล + เปิดเพลงระทึก!");
    }

    private void StartChaseMusic()
    {
        if (chaseMusicClip == null) return;

        // สร้าง AudioSource แยกอิสระ (2D ไม่ตกตามระยะทาง) เพื่อให้เพลงระทึกดังชัดเจนตลอดการวิ่งหนี
        GameObject musicObj = new GameObject($"ChaseMusic_{gameObject.name}");
        chaseMusicSource = musicObj.AddComponent<AudioSource>();
        chaseMusicSource.clip = chaseMusicClip;
        chaseMusicSource.loop = true;
        chaseMusicSource.spatialBlend = 0f; // 2D Audio
        chaseMusicSource.playOnAwake = false;
        chaseMusicSource.volume = 0f;
        chaseMusicSource.Play();

        StartCoroutine(FadeInMusicRoutine(chaseMusicSource, chaseMusicVolume, musicFadeInDuration));
    }

    private IEnumerator FadeInMusicRoutine(AudioSource source, float targetVol, float duration)
    {
        if (source == null) yield break;

        if (duration <= 0f)
        {
            source.volume = targetVol;
            yield break;
        }

        float timer = 0f;
        while (timer < duration && source != null)
        {
            timer += Time.deltaTime;
            source.volume = Mathf.Lerp(0f, targetVol, timer / duration);
            yield return null;
        }

        if (source != null) source.volume = targetVol;
    }

    private void StopAndFadeOutChaseMusic()
    {
        if (chaseMusicSource == null) return;

        AudioSource sourceToFade = chaseMusicSource;
        chaseMusicSource = null;

        // ย้าย Coroutine การเฟดออกไว้ที่ตัว AudioSource เอง เพื่อไม่ให้โดนตัดทิ้งตอน GameObject ผีถูก Destroy
        ChaseMusicFader fader = sourceToFade.gameObject.AddComponent<ChaseMusicFader>();
        fader.FadeOutAndDestroy(sourceToFade, musicFadeOutDuration);
    }

    private void Update()
    {
        if (!isActive || isPlayerCaught) return;

        // 1. ตรวจจับว่าเข้าใกล้ผู้เล่นจนจับได้หรือยัง
        CheckCatchPlayer();

        // 2. ทำงานตามเฟสที่ติ๊กเลือกไว้
        HandlePhaseBehavior();

        // 3. อัปเดตกล้องซูมออกตลอดช่วงหนี + สั่นเมื่อผีเข้าใกล้
        UpdateChaseCameraEffects();
    }

    /// <summary>
    /// แยกการทำงานของผีทั้ง 3 เฟส (สามารถเติมพฤติกรรมเฉพาะของแต่ละเฟสเพิ่มได้ที่นี่)
    /// </summary>
    private void HandlePhaseBehavior()
    {
        switch (currentPhase)
        {
            case GhostPhase.Phase1:
                MoveAlongPath();
                break;

            case GhostPhase.Phase2:
                MoveAlongPath();
                break;

            case GhostPhase.Phase3:
                MoveAlongPath();
                break;
        }
    }

    private void UpdateChaseCameraEffects()
    {
        if (!enableChaseCameraEffect || !isChaseStarted || playerTransform == null) return;

        ResolvePlayerReferences();
        if (playerController == null) return;

        // 1. ซูมออกไกลตลอดเวลาหลังเป่านกหวีด (ไม่ขึ้นกับระยะห่างผี)
        float fovBoost = chaseZoomOutFOV;

        // 2. กล้องสั่นเฉพาะตอนที่ผีเข้าใกล้ในระยะ shakeMaxDistance
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        float rawShakeIntensity = Mathf.InverseLerp(shakeMaxDistance, shakeMinDistance, dist);
        float smoothShakeIntensity = Mathf.SmoothStep(0f, 1f, rawShakeIntensity);

        Vector3 posOffset = Vector3.zero;
        Vector3 rotOffset = Vector3.zero;

        if (smoothShakeIntensity > 0.001f)
        {
            float t = Time.time * shakeFrequency;
            float nx = (Mathf.PerlinNoise(noiseSeed, t) - 0.5f) * 2f;
            float ny = (Mathf.PerlinNoise(noiseSeed + 31.7f, t) - 0.5f) * 2f;
            float nz = (Mathf.PerlinNoise(noiseSeed + 73.3f, t) - 0.5f) * 2f;

            posOffset = new Vector3(nx, ny, 0f) * (maxShakePositionAmount * smoothShakeIntensity);
            rotOffset = new Vector3(ny * 0.6f, nx * 0.6f, nz) * (maxShakeRotationAmount * smoothShakeIntensity);
        }

        playerController.SetExternalCameraEffects(fovBoost, posOffset, rotOffset);
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
        if (!isPlayerCaught && isActive && (other.CompareTag("Player") || other.transform.root.CompareTag("Player")))
        {
            TriggerPlayerCaught();
        }
    }

    private void MoveAlongPath()
    {
        if (waypoints == null || waypoints.Count == 0 || currentWaypointIndex >= waypoints.Count)
        {
            DespawnGhost();
            return;
        }

        Transform targetWaypoint = waypoints[currentWaypointIndex];
        if (targetWaypoint == null) return;

        Vector3 targetPos = targetWaypoint.position;
        Vector3 moveDir = (targetPos - transform.position).normalized;

        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        if (ghostSprite != null && moveDir.x != 0)
        {
            ghostSprite.flipX = moveDir.x > 0 ? defaultFacingLeft : !defaultFacingLeft;
        }

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

        CleanupPlayerChaseState(immediateMusicStop: true);

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
        CleanupPlayerChaseState(immediateMusicStop: false);
        Debug.Log("👻 [LinearChaserGhost] ผีวิ่งสุดเส้นทางแล้ว และหายไปถาวร");
        Destroy(gameObject);
    }

    private void CleanupPlayerChaseState(bool immediateMusicStop)
    {
        isChaseStarted = false;

        if (whistleSequenceCoroutine != null)
        {
            StopCoroutine(whistleSequenceCoroutine);
            whistleSequenceCoroutine = null;
        }

        if (immediateMusicStop)
        {
            if (chaseMusicSource != null)
            {
                Destroy(chaseMusicSource.gameObject);
                chaseMusicSource = null;
            }
        }
        else
        {
            StopAndFadeOutChaseMusic();
        }

        ResolvePlayerReferences();
        if (playerController != null)
        {
            playerController.SetChaseInfiniteStamina(false);
            playerController.ClearExternalCameraEffects();
        }
    }

    private void OnDestroy()
    {
        // กรณีโดนทำลายจาก GhostDespawnTrigger ให้คืนค่าสเตมิน่า กล้อง และเฟดเพลงระทึกออกทันที
        CleanupPlayerChaseState(immediateMusicStop: false);
    }

    private void OnDrawGizmos()
    {
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

    /// <summary>
    /// ตัวช่วยเฟดเสียงเพลงระทึกดับลงนุ่มนวลแม้ Object ผีจะถูกทำลายไปแล้ว
    /// </summary>
    private class ChaseMusicFader : MonoBehaviour
    {
        public void FadeOutAndDestroy(AudioSource source, float duration)
        {
            StartCoroutine(FadeRoutine(source, duration));
        }

        private IEnumerator FadeRoutine(AudioSource source, float duration)
        {
            if (source == null)
            {
                Destroy(gameObject);
                yield break;
            }

            float startVol = source.volume;
            if (duration > 0f)
            {
                float timer = 0f;
                while (timer < duration && source != null)
                {
                    timer += Time.deltaTime;
                    source.volume = Mathf.Lerp(startVol, 0f, timer / duration);
                    yield return null;
                }
            }

            if (source != null) source.Stop();
            Destroy(gameObject);
        }
    }
}