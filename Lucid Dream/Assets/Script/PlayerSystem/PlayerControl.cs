using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController3D_InputAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform playerCamera;
    [SerializeField] Transform groundCheck;
    [SerializeField] InputActionAsset inputActionAsset;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float crouchSpeed = 2f;
    [SerializeField] float acceleration = 30f;

    [Header("Fall Stun Settings ✨")]
    [Tooltip("ระยะความสูงการตกขั้นต่ำที่ทำให้ตัวละครติดดีเลย์/ชะงัก (เมตร)")]
    [SerializeField] float minFallStunDistance = 5f;
    [Tooltip("ระยะเวลาชะงัก/ดีเลย์เดินไม่ได้เมื่อตกถึงพื้น (วินาที)")]
    [SerializeField] float fallStunDuration = 1.2f;
    [Tooltip("องศากล้องก้มลงเมื่อกระแทกพื้น")]
    [SerializeField] float landingImpactPitch = 15f;
    [Tooltip("ความเร็วในการคืนสภาพกล้องหลังจากกระแทก")]
    [SerializeField] float landingRecoverSpeed = 5f;

    [Header("Wall Grab / Shimmy Settings ✨")]
    [Tooltip("เปิดใช้งานระบบเกาะกำแพง")]
    [SerializeField] bool enableWallGrab = true;
    [Tooltip("ระยะความยาว Raycast ตรวจจับกำแพงด้านหน้า")]
    [SerializeField] float wallCheckDistance = 0.8f;
    [Tooltip("ความเร็วในการขยับซ้าย-ขวาบนกำแพง")]
    [SerializeField] float wallShimmySpeed = 2.5f;
    [Tooltip("Layer ของกำแพงที่สามารถเกาะได้")]
    [SerializeField] LayerMask wallLayer = ~0;
    [Tooltip("Tag ของกำแพงที่เกาะได้")]
    [SerializeField] string climbableWallTag = "Climbable";
    [Tooltip("องศาที่กล้องจะหัน (Yaw) ไปตามทิศทางที่เลื่อนตัว")]
    [SerializeField] float shimmyCameraPanAmount = 10f;
    [SerializeField] float shimmyCameraSmoothing = 6f;

    [Header("Wall Squeeze Settings ✨")]
    [SerializeField] float squeezeSpeed = 1.2f;
    [SerializeField] float squeezedRadius = 0.18f;
    [SerializeField] float squeezedCameraTiltZ = 12f;
    [SerializeField] Vector3 squeezedCameraOffset = new Vector3(0.2f, -0.1f, -0.2f);
    [SerializeField] float squeezedFOV = 50f;
    [SerializeField] float squeezeSmoothing = 8f;
    [SerializeField] float squeezeCamRotateSpeed = 6f;
    [SerializeField] string wallGapTag = "WallGap";
    [Tooltip("ความเร็วในการดัดตัวละครให้อยู่กึ่งกลางซอกกำแพง เพื่อไม่ให้ติดคอลไลเดอร์")]
    [SerializeField] float gapAlignmentSpeed = 8f;

    [Header("Footstep & Movement Sounds ✨")]
    [SerializeField] AudioSource footstepAudioSource;

    [Header("Clips - Ground Movement")]
    [Tooltip("เสียงเดินปกติ")]
    [SerializeField] AudioClip[] defaultFootstepClips;
    [Tooltip("เสียงเดินย่อ (Crouch)")]
    [SerializeField] AudioClip[] crouchFootstepClips;

    [Header("Clips - Wall Squeeze")]
    [Tooltip("เสียงจังหวะแทรกตัวเข้าซอกกำแพง (เกิดขึ้นครั้งเดียวตอนกดแทรก)")]
    [SerializeField] AudioClip squeezeEnterClip;
    [Tooltip("เสียงขยับตัวเดินในซอกกำแพง (เล่นตามจังหวะเดิน)")]
    [SerializeField] AudioClip[] squeezeMoveClips;

    [Header("Clips - Wall Grab & Shimmy")]
    [Tooltip("เสียงตอนเอามือแปะ/เกาะกำแพง (เกิดขึ้นครั้งเดียวตอนกดเกาะ)")]
    [SerializeField] AudioClip wallGrabEnterClip;
    [Tooltip("เสียงขยับตัวซ้าย-ขวาขณะเกาะกำแพง")]
    [SerializeField] AudioClip[] wallShimmyClips;

    [Header("Audio Tuning")]
    [SerializeField] float baseStepInterval = 0.5f;
    [SerializeField] float volumeWalk = 0.6f;
    [SerializeField] float volumeRun = 1.0f;
    [SerializeField] float volumeCrouch = 0.3f;
    [SerializeField] float volumeSqueezeEnter = 0.8f;
    [SerializeField] float volumeSqueezeMove = 0.4f;
    [SerializeField] float volumeWallGrabEnter = 0.8f;
    [SerializeField] float volumeWallShimmy = 0.5f;

    [Header("Stamina Settings ✨")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaDrainRate = 25f;
    [SerializeField] float staminaRegenRate = 15f;
    [SerializeField] float staminaRegenDelay = 1f;
    [SerializeField] RectTransform staminaFillRect;
    [SerializeField] CanvasGroup staminaCanvasGroup;
    [SerializeField] bool hideWhenFull = true;
    [SerializeField] float fadeSpeed = 5f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 7f;
    [SerializeField] int maxJumps = 1;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;
    [Range(0f, 1f)][SerializeField] float variableJumpMultiplier = 0.5f;

    [Header("Ground Check (No Layer Needed)")]
    [SerializeField] float groundCheckRadius = 0.2f;

    [Header("Crouch Settings")]
    [SerializeField] float crouchHeight = 1f;
    [SerializeField] float crouchCameraYOffset = 0.6f;
    [SerializeField] float crouchSmoothing = 10f;

    [Header("Mouse Look")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float pitchMin = -75f;
    [SerializeField] float pitchMax = 75f;
    [SerializeField] bool lockCursor = true;

    [Header("Head Bob")]
    [SerializeField] bool enableHeadBob = true;
    [SerializeField] float headBobFrequency = 1f;
    [SerializeField] float headBobAmount = 0.06f;
    [SerializeField] float headBobSmoothing = 4f;

    [Header("Camera Sway")]
    [SerializeField] bool enableCameraSway = true;
    [SerializeField] float swayAmount = 0.05f;
    [SerializeField] float swaySmoothing = 3f;

    // Input Action references
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    // Movement variables
    Rigidbody rb;
    CapsuleCollider capsuleCollider;
    Vector3 targetInput = Vector3.zero;
    Vector2 lookInput = Vector2.zero;
    int jumpsLeft;
    float lastGroundTime = -10f;
    float lastJumpPressedTime = -10f;
    bool grounded;
    float yaw = 0f;
    float pitch = 0f;

    // Fall Stun Variables ✨
    private float highestYPoint;
    private bool isStunned = false;
    private float stunTimer = 0f;
    private bool wasGroundedLastFrame = true;
    private float currentLandingImpact = 0f;

    // Wall Grab Variables ✨
    private bool isWallGrabbing = false;
    private bool isTouchWall = false;
    private RaycastHit wallHitInfo;
    private float currentShimmyPanY = 0f;

    // Stamina variables
    private float currentStamina;
    private float staminaRegenTimer;
    private bool isSprinting;

    // Crouch & Height variables
    private float defaultHeight;
    private float defaultCenterY;
    private float defaultRadius;
    private bool isCrouching;
    private Vector3 currentBaseCameraPos;

    // Wall Squeeze variables ✨
    private bool isSqueezing = false;
    private bool isInGapZone = false;
    private float defaultFOV;
    private float currentCameraTiltZ = 0f;
    private Camera camComponent;
    private Transform currentGapTransform;
    private float squeezeBaseYaw;
    private float squeezeTargetYaw;
    private bool isFacingReverseInGap = false;
    private bool sKeyPressedLastFrame = false;

    // Footstep Sound variables ✨
    private float stepTimer = 0f;

    // Head bob variables
    float bobTimer = 0f;
    Vector3 originalCameraPosition;
    Vector3 currentCameraOffset = Vector3.zero;
    Vector3 targetCameraOffset = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (footstepAudioSource == null)
        {
            footstepAudioSource = GetComponent<AudioSource>();
        }

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (groundCheck == null)
        {
            GameObject g = new GameObject("GroundCheck");
            g.transform.SetParent(transform);
            g.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            groundCheck = g.transform;
        }

        if (lockCursor) Cursor.lockState = CursorLockMode.Locked;

        if (capsuleCollider != null)
        {
            defaultHeight = capsuleCollider.height;
            defaultCenterY = capsuleCollider.center.y;
            defaultRadius = capsuleCollider.radius;
        }

        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.localPosition;
            currentBaseCameraPos = originalCameraPosition;

            camComponent = playerCamera.GetComponent<Camera>();
            if (camComponent != null)
            {
                defaultFOV = camComponent.fieldOfView;
                camComponent.nearClipPlane = 0.01f;
            }
        }

        currentStamina = maxStamina;

        if (staminaCanvasGroup != null && hideWhenFull)
        {
            staminaCanvasGroup.alpha = 0f;
        }

        SetupInputActions();
    }

    void SetupInputActions()
    {
        if (inputActionAsset == null)
        {
            Debug.LogError("InputActionAsset not assigned!");
            return;
        }

        playerActionMap = inputActionAsset.FindActionMap("Player");
        if (playerActionMap == null) return;

        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");
        jumpAction = playerActionMap.FindAction("Jump");
        sprintAction = playerActionMap.FindAction("Sprint");
        crouchAction = playerActionMap.FindAction("Crouch");

        if (jumpAction != null)
        {
            jumpAction.started += OnJumpPressed;
            jumpAction.canceled += OnJumpReleased;
        }

        playerActionMap.Enable();
    }

    void OnJumpPressed(InputAction.CallbackContext context)
    {
        if (DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive) return;
        if (isStunned) return;

        if (isWallGrabbing)
        {
            DetachFromWall();
            DoJump();
            return;
        }

        if (!isCrouching && !isSqueezing)
        {
            lastJumpPressedTime = Time.time;
        }
    }

    void OnJumpReleased(InputAction.CallbackContext context)
    {
        if (DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive) return;

        if (rb.linearVelocity.y > 0f)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y *= variableJumpMultiplier;
            rb.linearVelocity = vel;
        }
    }

    void Update()
    {
        if (DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive)
        {
            targetInput = Vector3.zero;
            lookInput = Vector2.zero;
            transform.eulerAngles = new Vector3(0f, yaw, 0f);
            return;
        }

        grounded = CheckGroundedNoLayer();

        HandleFallStunLogic();
        HandleWallGrabLogic();

        if (moveAction != null && !isStunned)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            targetInput = new Vector3(moveInput.x, 0f, moveInput.y);
        }
        else if (isStunned)
        {
            targetInput = Vector3.zero;
        }

        if (lookAction != null) lookInput = lookAction.ReadValue<Vector2>();

        HandleSqueezeInput();

        if (isSqueezing)
        {
            bool sKeyPressed = targetInput.z < -0.5f;
            if (sKeyPressed && !sKeyPressedLastFrame)
            {
                isFacingReverseInGap = !isFacingReverseInGap;
            }
            sKeyPressedLastFrame = sKeyPressed;

            squeezeTargetYaw = squeezeBaseYaw + (isFacingReverseInGap ? 180f : 0f);

            yaw = Mathf.LerpAngle(yaw, squeezeTargetYaw, Time.deltaTime * squeezeCamRotateSpeed);
            pitch = Mathf.Lerp(pitch, 0f, Time.deltaTime * squeezeCamRotateSpeed);
        }
        else if (isWallGrabbing)
        {
            pitch -= lookInput.y * (mouseSensitivity * 0.01f);
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }
        else
        {
            Vector2 mouse = lookInput * (mouseSensitivity * 0.01f);
            yaw += mouse.x;
            pitch -= mouse.y;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        transform.eulerAngles = new Vector3(0f, yaw, 0f);

        currentLandingImpact = Mathf.Lerp(currentLandingImpact, 0f, Time.deltaTime * landingRecoverSpeed);

        float targetShimmyPan = isWallGrabbing ? (targetInput.x * shimmyCameraPanAmount) : 0f;
        currentShimmyPanY = Mathf.Lerp(currentShimmyPanY, targetShimmyPan, Time.deltaTime * shimmyCameraSmoothing);

        if (playerCamera != null)
        {
            playerCamera.localEulerAngles = new Vector3(pitch + currentLandingImpact, currentShimmyPanY, currentCameraTiltZ);
        }

        if (grounded)
        {
            lastGroundTime = Time.time;
            jumpsLeft = maxJumps;
        }

        bool crouchKeyPressed = (crouchAction != null) && crouchAction.IsPressed();
        if (crouchKeyPressed && !isSqueezing && !isWallGrabbing)
        {
            isCrouching = true;
        }
        else if (!isSqueezing && !isWallGrabbing)
        {
            isCrouching = HasCeilingAbove();
        }
        else
        {
            isCrouching = false;
        }

        HandleCrouchingAndSqueezing();
        HandleStamina();
        HandleFootsteps();

        if (Time.time - lastJumpPressedTime <= jumpBufferTime && !isSqueezing && !isStunned)
        {
            if (Time.time - lastGroundTime <= coyoteTime || jumpsLeft > 0)
            {
                DoJump();
                lastJumpPressedTime = -10f;
            }
        }

        UpdateHeadBob();
    }

    #region ✨ Movement & Footstep Audio System
    private void HandleFootsteps()
    {
        if (!grounded && !isSqueezing && !isWallGrabbing) return;

        float currentSpeed = 0f;

        if (isWallGrabbing)
        {
            currentSpeed = Mathf.Abs(targetInput.x) * wallShimmySpeed;
        }
        else
        {
            Vector3 horizontalVel = rb.linearVelocity;
            horizontalVel.y = 0f;
            currentSpeed = horizontalVel.magnitude;
        }

        if (currentSpeed < 0.1f)
        {
            stepTimer = 0f;
            return;
        }

        float currentInterval = baseStepInterval;
        float volume = volumeWalk;
        AudioClip[] targetClips = defaultFootstepClips;

        if (isWallGrabbing)
        {
            currentInterval = baseStepInterval * 1.1f;
            volume = volumeWallShimmy;
            targetClips = (wallShimmyClips != null && wallShimmyClips.Length > 0) ? wallShimmyClips : defaultFootstepClips;
        }
        else if (isSqueezing)
        {
            currentInterval = baseStepInterval * 1.4f;
            volume = volumeSqueezeMove;
            targetClips = (squeezeMoveClips != null && squeezeMoveClips.Length > 0) ? squeezeMoveClips : defaultFootstepClips;
        }
        else if (isCrouching)
        {
            currentInterval = baseStepInterval * 1.6f;
            volume = volumeCrouch;
            targetClips = (crouchFootstepClips != null && crouchFootstepClips.Length > 0) ? crouchFootstepClips : defaultFootstepClips;
        }
        else if (isSprinting)
        {
            currentInterval = baseStepInterval * 0.65f;
            volume = volumeRun;
        }

        float speedMultiplier = isWallGrabbing ? (currentSpeed / wallShimmySpeed) : (currentSpeed / walkSpeed);
        stepTimer += Time.deltaTime * speedMultiplier;

        if (stepTimer >= currentInterval)
        {
            PlayRandomAudioClip(targetClips, volume);
            stepTimer = 0f;
        }
    }

    private void PlayRandomAudioClip(AudioClip[] clips, float volume)
    {
        if (footstepAudioSource == null || clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        footstepAudioSource.pitch = Random.Range(0.92f, 1.08f);
        footstepAudioSource.PlayOneShot(clip, volume);
    }

    private void PlaySingleSoundEffect(AudioClip clip, float volume)
    {
        if (footstepAudioSource == null || clip == null) return;

        footstepAudioSource.pitch = Random.Range(0.95f, 1.05f);
        footstepAudioSource.PlayOneShot(clip, volume);
    }
    #endregion

    #region ✨ Fall Stun System Logic
    private void HandleFallStunLogic()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
            }
        }

        if (!grounded && !isWallGrabbing)
        {
            if (wasGroundedLastFrame)
            {
                highestYPoint = transform.position.y;
            }
            else
            {
                highestYPoint = Mathf.Max(highestYPoint, transform.position.y);
            }
        }
        else if (!wasGroundedLastFrame && grounded)
        {
            float fallDistance = highestYPoint - transform.position.y;
            if (fallDistance >= minFallStunDistance)
            {
                TriggerFallStun();
            }
        }

        wasGroundedLastFrame = grounded;
    }

    private void TriggerFallStun()
    {
        isStunned = true;
        stunTimer = fallStunDuration;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);

        currentLandingImpact = landingImpactPitch;

        if (isWallGrabbing) DetachFromWall();
    }
    #endregion

    #region ✨ Wall Grab System Logic
    private void HandleWallGrabLogic()
    {
        if (!enableWallGrab || isSqueezing || isStunned)
        {
            if (isWallGrabbing) DetachFromWall();
            return;
        }

        bool eKeyPressed = false;
        if (Keyboard.current != null)
        {
            eKeyPressed = Keyboard.current.eKey.wasPressedThisFrame;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
        isTouchWall = Physics.Raycast(rayOrigin, transform.forward, out wallHitInfo, wallCheckDistance, wallLayer, QueryTriggerInteraction.Ignore);

        bool isValidWall = isTouchWall;
        if (isTouchWall && !string.IsNullOrEmpty(climbableWallTag))
        {
            isValidWall = wallHitInfo.collider.CompareTag(climbableWallTag);
        }

        if (!isWallGrabbing)
        {
            if (isValidWall && eKeyPressed)
            {
                isWallGrabbing = true;
                rb.useGravity = false;
                jumpsLeft = maxJumps;

                yaw = Quaternion.LookRotation(-wallHitInfo.normal).eulerAngles.y;

                // ✨ เล่นเสียงกดเริ่มเกาะกำแพง
                PlaySingleSoundEffect(wallGrabEnterClip, volumeWallGrabEnter);
            }
        }
        else
        {
            if (eKeyPressed || !isValidWall)
            {
                DetachFromWall();
            }
        }
    }

    private void DetachFromWall()
    {
        isWallGrabbing = false;
        rb.useGravity = true;
    }
    #endregion

    void HandleSqueezeInput()
    {
        bool eKeyPressed = false;
        if (Keyboard.current != null)
        {
            eKeyPressed = Keyboard.current.eKey.wasPressedThisFrame;
        }

        if (isInGapZone && eKeyPressed && !isSqueezing && !isStunned && !isWallGrabbing)
        {
            isSqueezing = true;

            if (currentGapTransform != null)
            {
                float gapYaw = currentGapTransform.eulerAngles.y;
                float angleDiff = Mathf.DeltaAngle(transform.eulerAngles.y, gapYaw);

                squeezeBaseYaw = (Mathf.Abs(angleDiff) > 90f) ? gapYaw + 180f : gapYaw;
            }
            else
            {
                squeezeBaseYaw = transform.eulerAngles.y;
            }

            squeezeTargetYaw = squeezeBaseYaw;
            isFacingReverseInGap = false;
            sKeyPressedLastFrame = false;

            // ✨ เล่นเสียงจังหวะกดเริ่มแทรกตัวเข้าซอกกำแพง
            PlaySingleSoundEffect(squeezeEnterClip, volumeSqueezeEnter);
        }

        if (!isInGapZone && isSqueezing)
        {
            isSqueezing = false;
        }
    }

    void HandleCrouchingAndSqueezing()
    {
        if (capsuleCollider == null) return;

        float targetRadius = isSqueezing ? squeezedRadius : defaultRadius;
        capsuleCollider.radius = Mathf.Lerp(capsuleCollider.radius, targetRadius, squeezeSmoothing * Time.deltaTime);

        float targetHeight = isCrouching ? crouchHeight : defaultHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchSmoothing * Time.deltaTime);

        float halfHeightDifference = (defaultHeight - capsuleCollider.height) / 2f;
        capsuleCollider.center = new Vector3(capsuleCollider.center.x, defaultCenterY - halfHeightDifference, capsuleCollider.center.z);

        if (playerCamera != null)
        {
            float targetCameraY = isCrouching ? (originalCameraPosition.y - crouchCameraYOffset) : originalCameraPosition.y;
            Vector3 targetOffset = isSqueezing ? squeezedCameraOffset : Vector3.zero;

            currentBaseCameraPos = Vector3.Lerp(currentBaseCameraPos, originalCameraPosition + targetOffset, squeezeSmoothing * Time.deltaTime);
            currentBaseCameraPos.y = Mathf.Lerp(currentBaseCameraPos.y, targetCameraY, crouchSmoothing * Time.deltaTime);

            float targetFOV = isSqueezing ? squeezedFOV : defaultFOV;
            float targetTilt = isSqueezing ? squeezedCameraTiltZ : 0f;

            if (camComponent != null)
            {
                camComponent.fieldOfView = Mathf.Lerp(camComponent.fieldOfView, targetFOV, squeezeSmoothing * Time.deltaTime);
            }

            currentCameraTiltZ = Mathf.Lerp(currentCameraTiltZ, targetTilt, squeezeSmoothing * Time.deltaTime);
        }
    }

    void HandleStamina()
    {
        bool wantsToSprint = (sprintAction != null) && sprintAction.IsPressed();
        bool isMoving = targetInput.sqrMagnitude > 0.01f;

        if (wantsToSprint && isMoving && !isCrouching && !isSqueezing && !isWallGrabbing && !isStunned && currentStamina > 0f)
        {
            isSprinting = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            isSprinting = false;

            if (staminaRegenTimer > 0f)
            {
                staminaRegenTimer -= Time.deltaTime;
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Min(currentStamina, maxStamina);
            }
        }

        if (staminaFillRect != null)
        {
            float staminaRatio = currentStamina / maxStamina;
            staminaFillRect.localScale = new Vector3(staminaRatio, 1f, 1f);
        }

        if (staminaCanvasGroup != null)
        {
            float targetAlpha = (hideWhenFull && currentStamina >= maxStamina) ? 0f : 1f;
            staminaCanvasGroup.alpha = Mathf.MoveTowards(staminaCanvasGroup.alpha, targetAlpha, fadeSpeed * Time.deltaTime);
        }
    }

    void FixedUpdate()
    {
        if (DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
            return;
        }

        if (isStunned)
        {
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isWallGrabbing)
        {
            float shimmyInput = targetInput.x;
            Vector3 shimmyVelocity = transform.right * shimmyInput * wallShimmySpeed;
            rb.linearVelocity = shimmyVelocity;
            return;
        }

        Vector3 desiredHorizontalVel;

        if (isSqueezing)
        {
            if (currentGapTransform != null)
            {
                Vector3 gapForward = currentGapTransform.forward;
                Vector3 gapCenter = currentGapTransform.position;
                Vector3 playerPos = rb.position;

                Vector3 diff = playerPos - gapCenter;
                Vector3 projectedOffset = Vector3.Project(diff, gapForward);
                Vector3 targetAlignedPos = gapCenter + projectedOffset;
                targetAlignedPos.y = playerPos.y;

                Vector3 newAlignedPos = Vector3.Lerp(playerPos, targetAlignedPos, gapAlignmentSpeed * Time.fixedDeltaTime);
                rb.MovePosition(newAlignedPos);
            }

            Vector3 forwardDir = Quaternion.Euler(0, squeezeTargetYaw, 0) * Vector3.forward;
            float moveForwardAmount = Mathf.Max(0f, targetInput.z);
            float speed = squeezeSpeed;

            if (InventoryManager.Instance != null)
            {
                speed *= InventoryManager.Instance.GetTotalSpeedMultiplier();
            }

            desiredHorizontalVel = forwardDir * moveForwardAmount * speed;
        }
        else
        {
            Vector3 cameraRight = (playerCamera != null) ? playerCamera.right : transform.right;
            Vector3 cameraForward = (playerCamera != null) ? playerCamera.forward : transform.forward;

            cameraRight.y = 0f;
            cameraForward.y = 0f;
            cameraRight.Normalize();
            cameraForward.Normalize();

            float speed = isCrouching ? crouchSpeed : (isSprinting ? runSpeed : walkSpeed);

            if (InventoryManager.Instance != null)
            {
                speed *= InventoryManager.Instance.GetTotalSpeedMultiplier();
            }
            desiredHorizontalVel = (cameraRight * targetInput.x + cameraForward * targetInput.z) * speed;
        }

        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 newHorizontalVel = Vector3.MoveTowards(horizontalVel, desiredHorizontalVel, acceleration * Time.fixedDeltaTime);

        Vector3 newVel = newHorizontalVel + Vector3.up * currentVel.y;
        rb.linearVelocity = newVel;
    }

    bool CheckGroundedNoLayer()
    {
        Collider[] hitColliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider col in hitColliders)
        {
            if (col.gameObject != gameObject && !col.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    bool HasCeilingAbove()
    {
        if (capsuleCollider == null) return false;

        float radius = capsuleCollider.radius * 0.85f;
        Vector3 origin = transform.position + Vector3.up * (crouchHeight - radius);
        float checkDistance = defaultHeight - crouchHeight;

        if (checkDistance <= 0f) return false;

        RaycastHit[] hits = Physics.SphereCastAll(origin, radius, Vector3.up, checkDistance, ~0, QueryTriggerInteraction.Ignore);

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    void DoJump()
    {
        if (jumpsLeft <= 0) return;
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        jumpsLeft--;
    }

    void UpdateHeadBob()
    {
        if (playerCamera == null) return;

        if (enableHeadBob && !isSqueezing && !isWallGrabbing && !isStunned)
        {
            Vector3 horizontalVel = rb.linearVelocity;
            horizontalVel.y = 0f;
            float speed = horizontalVel.magnitude;

            float currentMoveSpeedLimit = isCrouching ? crouchSpeed : walkSpeed;
            if (InventoryManager.Instance != null)
            {
                currentMoveSpeedLimit *= InventoryManager.Instance.GetTotalSpeedMultiplier();
            }

            if (speed > 0.1f)
            {
                bobTimer += Time.deltaTime * headBobFrequency * (speed / currentMoveSpeedLimit);
            }

            float bobX = Mathf.Sin(bobTimer * Mathf.PI * 2f) * swayAmount;
            float bobY = Mathf.Sin(bobTimer * Mathf.PI * 4f) * headBobAmount;

            targetCameraOffset = new Vector3(bobX, bobY, 0f);
        }
        else
        {
            targetCameraOffset = Vector3.zero;
        }

        currentCameraOffset = Vector3.Lerp(currentCameraOffset, targetCameraOffset, headBobSmoothing * Time.deltaTime);
        playerCamera.localPosition = currentBaseCameraPos + currentCameraOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(wallGapTag))
        {
            isInGapZone = true;
            currentGapTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(wallGapTag))
        {
            isInGapZone = false;
            currentGapTransform = null;
        }
    }

    void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpPressed;
            jumpAction.canceled -= OnJumpReleased;
        }
        if (playerActionMap != null) playerActionMap.Disable();
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        Gizmos.color = isTouchWall ? Color.cyan : Color.red;
        Vector3 rayOrigin = transform.position + Vector3.up * 1.0f;
        Gizmos.DrawRay(rayOrigin, transform.forward * wallCheckDistance);

        if (capsuleCollider != null)
        {
            Gizmos.color = isSqueezing ? Color.yellow : (isCrouching ? Color.red : Color.cyan);
            float radius = capsuleCollider.radius * 0.85f;
            Vector3 origin = transform.position + Vector3.up * (crouchHeight - radius);
            float checkDistance = defaultHeight - crouchHeight;
            Gizmos.DrawWireSphere(origin + Vector3.up * checkDistance, radius);
        }
    }
}