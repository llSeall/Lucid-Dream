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

    [Header("Arm Sprites & Procedural Animation ✨")]
    [SerializeField] Transform leftArmTransform;
    [SerializeField] Transform rightArmTransform;
    [SerializeField] SpriteRenderer leftArmSpriteRenderer;
    [SerializeField] SpriteRenderer rightArmSpriteRenderer;

    [Header("Arm Sprites - Walk / Crouch / Sprint / Grab ✨")]
    [SerializeField] Sprite walkLeftArmSprite;
    [SerializeField] Sprite walkRightArmSprite;
    [SerializeField] Sprite sprintLeftArmSprite;
    [SerializeField] Sprite sprintRightArmSprite;
    [SerializeField] Sprite wallGrabLeftArmSprite;
    [SerializeField] Sprite wallGrabRightArmSprite;

    [Header("Arm Pose Settings ✨")]
    [SerializeField] Vector3 idleLoweredOffset = new Vector3(0f, -0.6f, -0.1f);
    [SerializeField] Vector3 raisedArmOffset = new Vector3(0f, 0.1f, 0.05f);
    [SerializeField] Vector3 wallGrabArmOffset = new Vector3(0f, 0.15f, 0.1f);
    [SerializeField] float armLerpSpeed = 12f;

    [Header("Arm Swing Settings ✨")]
    [SerializeField] float runArmSwingAmount = 0.08f;
    [SerializeField] float runArmRotationAmount = 15f;
    [SerializeField] float crouchArmSwingAmount = 0.03f;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float crouchSpeed = 2f;
    [SerializeField] float acceleration = 30f;
    private float walkCyclePhase = 0f;

    [Header("Fall Stun Settings ✨")]
    [SerializeField] float minFallStunDistance = 5f;
    [SerializeField] float fallStunDuration = 1.2f;
    [SerializeField] float landingImpactPitch = 15f;
    [SerializeField] float landingRecoverSpeed = 5f;

    [Header("1. Vault & Climb System (Trigger Tag: Climbable) ✨")]
    [SerializeField] bool enableClimbVault = true;
    [SerializeField] string climbableWallTag = "Climbable";
    [SerializeField] float climbUpHeight = 1.8f;
    [SerializeField] float climbForwardDistance = 1.0f;
    [SerializeField] float climbSpeed = 3.5f;
    [Tooltip("ระยะเผื่อความสูงจากขอบบนสุดของ Trigger (หากยืนบนขอบจะไม่ทำงาน)")]
    [SerializeField] float ledgeGrabMaxHeightOffset = 0.3f;

    [Header("2. Edge Shimmy System (Trigger Tag: WallShimmy) ✨")]
    [SerializeField] bool enableEdgeShimmy = true;
    [SerializeField] string shimmyWallTag = "WallShimmy";
    [SerializeField] float wallShimmySpeed = 2.2f;
    [SerializeField] float shimmyPitchMin = -15f;
    [SerializeField] float shimmyPitchMax = 35f;

    [Header("3. Wall Squeeze Settings (Trigger Tag: WallGap) ✨")]
    [SerializeField] float squeezeSpeed = 1.2f;
    [SerializeField] float squeezedRadius = 0.18f;
    [SerializeField] float squeezedCameraTiltZ = 12f;
    [SerializeField] Vector3 squeezedCameraOffset = new Vector3(0.2f, -0.1f, -0.2f);
    [SerializeField] float squeezedFOV = 50f;
    [SerializeField] float squeezeSmoothing = 8f;
    [SerializeField] float squeezeCamRotateSpeed = 6f;
    [SerializeField] string wallGapTag = "WallGap";
    [SerializeField] float gapAlignmentSpeed = 8f;

    [Header("Audio Settings ✨")]
    [SerializeField] AudioSource footstepAudioSource;
    [SerializeField] AudioClip[] defaultFootstepClips;
    [SerializeField] AudioClip[] crouchFootstepClips;
    [SerializeField] AudioClip squeezeEnterClip;
    [SerializeField] AudioClip[] squeezeMoveClips;
    [SerializeField] AudioClip wallGrabEnterClip;
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

    [Header("Jump & Physics")]
    [SerializeField] float jumpForce = 7f;
    [SerializeField] int maxJumps = 1;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;
    [Range(0f, 1f)][SerializeField] float variableJumpMultiplier = 0.5f;
    [SerializeField] float groundCheckRadius = 0.2f;

    private float jumpCooldownTimer = 0f;

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

    // Input Action references
    private InputActionMap playerActionMap;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction crouchAction;

    // Core States
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

    // Fall Stun
    private float highestYPoint;
    private bool isStunned = false;
    private float stunTimer = 0f;
    private bool wasGroundedLastFrame = true;
    private float currentLandingImpact = 0f;

    // Wall Vault (Trigger Mode)
    private bool isClimbingVault = false;
    private bool isGrabbingLedge = false;
    private bool requireFreshWPress = false;
    private Transform currentClimbTrigger;
    private float grabTargetYaw = 0f;
    private float climbCooldownTimer = 0f;

    // Edge Shimmy (Trigger Mode)
    private bool isEdgeShimmying = false;
    private Transform currentShimmyTrigger;
    private Vector3 shimmyWallNormal;

    // Stamina
    private float currentStamina;
    private float staminaRegenTimer;
    private bool isSprinting;

    // Crouch
    private float defaultHeight;
    private float defaultCenterY;
    private float defaultRadius;
    private bool isCrouching;
    private Vector3 currentBaseCameraPos;

    // Squeeze
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

    public bool IsSqueezing => isSqueezing;

    // Audio & Bob
    private float nextStepPhase = Mathf.PI;
    float bobTimer = 0f;
    Vector3 originalCameraPosition;
    Vector3 currentCameraOffset = Vector3.zero;
    Vector3 targetCameraOffset = Vector3.zero;

    // Procedural Arms
    private Vector3 leftArmDefaultLocalPos;
    private Vector3 rightArmDefaultLocalPos;
    private Quaternion leftArmDefaultLocalRot;
    private Quaternion rightArmDefaultLocalRot;

    // ✨ สำหรับระบบ Settings (เก็บค่าเริ่มต้นแท้จริงจาก Inspector)
    private float initialSensitivity;
    private float initialFOV;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        // 1. ค้นหากล้องและบันทึกค่ามาตรฐานจาก Inspector ก่อนเสมอ
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.localPosition;
            currentBaseCameraPos = originalCameraPosition;

            camComponent = playerCamera.GetComponent<Camera>();
            if (camComponent != null)
            {
                initialFOV = camComponent.fieldOfView; // บันทึกค่า FOV ตั้งต้นของกล้อง
                defaultFOV = initialFOV;
                camComponent.nearClipPlane = 0.01f;
            }
        }
        else
        {
            initialFOV = 60f;
            defaultFOV = 60f;
        }

        initialSensitivity = mouseSensitivity; // บันทึกค่า Sensitivity ตั้งต้นจาก Inspector

        // 2. โหลดค่าความไวเมาส์และ FOV ที่เคยเซฟไว้ (ถ้ามี)
        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity");
        }

        if (PlayerPrefs.HasKey("CameraFOV"))
        {
            defaultFOV = PlayerPrefs.GetFloat("CameraFOV");
            if (camComponent != null)
            {
                camComponent.fieldOfView = defaultFOV;
            }
        }

        if (footstepAudioSource == null) footstepAudioSource = GetComponent<AudioSource>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

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

        if (leftArmTransform != null)
        {
            leftArmDefaultLocalPos = leftArmTransform.localPosition;
            leftArmDefaultLocalRot = leftArmTransform.localRotation;
            if (leftArmSpriteRenderer == null) leftArmSpriteRenderer = leftArmTransform.GetComponent<SpriteRenderer>();
        }
        if (rightArmTransform != null)
        {
            rightArmDefaultLocalPos = rightArmTransform.localPosition;
            rightArmDefaultLocalRot = rightArmTransform.localRotation;
            if (rightArmSpriteRenderer == null) rightArmSpriteRenderer = rightArmTransform.GetComponent<SpriteRenderer>();
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
        if (inputActionAsset == null) return;

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
        if (PlayerWakeUpEffect.Instance != null && PlayerWakeUpEffect.Instance.IsWakingUp) return;
        if (isStunned || isClimbingVault) return;

        if (isEdgeShimmying)
        {
            DetachEdgeShimmy();
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
        if (rb.linearVelocity.y > 0f)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y *= variableJumpMultiplier;
            rb.linearVelocity = vel;
        }
    }
    void Update()
    {
        if (PauseMenuManager.Instance != null && PauseMenuManager.Instance.IsPaused) return;
        if (ComputerInteraction.IsInteractingWithPC) return;

        if (climbCooldownTimer > 0f)
        {
            climbCooldownTimer -= Time.deltaTime;
        }

        // 1. นัยนับถอยหลัง Cooldown กระโดด
        if (jumpCooldownTimer > 0f)
        {
            jumpCooldownTimer -= Time.deltaTime;
        }

        // ✨ [แก้ไขจุดซ้ำซ้อน] เช็กพื้นแบบผ่าน Filter เพียงจุดเดียวที่ต้น Update
        // (ห้ามมี grounded = CheckGroundedNoLayer(); บรรทัดเดี่ยวๆ อีก)
        grounded = (jumpCooldownTimer <= 0f) && (rb.linearVelocity.y <= 0.1f) && CheckGroundedNoLayer();

        if ((DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive) ||
            (PlayerWakeUpEffect.Instance != null && PlayerWakeUpEffect.Instance.IsWakingUp))
        {
            targetInput = Vector3.zero;
            lookInput = Vector2.zero;
            transform.eulerAngles = new Vector3(0f, yaw, 0f);
            UpdateProceduralArmAnimation();
            return;
        }

        HandleFallStunLogic();
        HandleWallClimbAndShimmyLogic();

        if (moveAction != null && !isStunned && !isClimbingVault)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            targetInput = new Vector3(moveInput.x, 0f, moveInput.y);
        }
        else if (isStunned || isClimbingVault)
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
        else if (isGrabbingLedge)
        {
            if (currentClimbTrigger != null)
            {
                Collider col = currentClimbTrigger.GetComponent<Collider>();
                Vector3 camPos = (playerCamera != null) ? playerCamera.position : transform.position;
                Vector3 targetPoint = (col != null) ? col.ClosestPoint(camPos) : currentClimbTrigger.position;

                Vector3 dirToTargetHorizontal = targetPoint - transform.position;
                dirToTargetHorizontal.y = 0f;

                if (dirToTargetHorizontal.sqrMagnitude > 0.04f)
                {
                    grabTargetYaw = Quaternion.LookRotation(dirToTargetHorizontal).eulerAngles.y;
                }

                Vector3 dirToTargetCam = targetPoint - camPos;
                if (dirToTargetCam.sqrMagnitude > 0.04f)
                {
                    float targetPitch = Quaternion.LookRotation(dirToTargetCam).eulerAngles.x;
                    if (targetPitch > 180f) targetPitch -= 360f;
                    pitch = Mathf.Lerp(pitch, Mathf.Clamp(targetPitch, pitchMin, pitchMax), Time.deltaTime * 12f);
                }
            }

            yaw = Mathf.LerpAngle(yaw, grabTargetYaw, Time.deltaTime * 12f);
        }
        else if (isEdgeShimmying)
        {
            Vector2 mouse = lookInput * (mouseSensitivity * 0.05f);
            pitch -= mouse.y;
            pitch = Mathf.Clamp(pitch, shimmyPitchMin, shimmyPitchMax);

            float backToWallYaw = (currentShimmyTrigger != null) ? currentShimmyTrigger.eulerAngles.y : Quaternion.LookRotation(shimmyWallNormal).eulerAngles.y;

            if (targetInput.x < -0.1f)
            {
                yaw = Mathf.LerpAngle(yaw, backToWallYaw - 80f, Time.deltaTime * 10f);
            }
            else if (targetInput.x > 0.1f)
            {
                yaw = Mathf.LerpAngle(yaw, backToWallYaw + 80f, Time.deltaTime * 10f);
            }
            else
            {
                yaw = Mathf.LerpAngle(yaw, backToWallYaw, Time.deltaTime * 10f);
            }
        }
        else if (!isClimbingVault)
        {
            Vector2 mouse = lookInput * (mouseSensitivity * 0.05f);
            yaw += mouse.x;
            pitch -= mouse.y;
            pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        }

        transform.eulerAngles = new Vector3(0f, yaw, 0f);

        currentLandingImpact = Mathf.Lerp(currentLandingImpact, 0f, Time.deltaTime * landingRecoverSpeed);

        if (playerCamera != null)
        {
            playerCamera.localEulerAngles = new Vector3(pitch + currentLandingImpact, 0f, currentCameraTiltZ);
        }

        // ✨ เมื่อผ่าน Filter มาแล้ว ถึงค่อยอนุญาตให้รีเซ็ตโควตากระโดด
        if (grounded)
        {
            lastGroundTime = Time.time;
            jumpsLeft = maxJumps;
        }

        bool crouchKeyPressed = (crouchAction != null) && crouchAction.IsPressed();
        if (crouchKeyPressed && !isSqueezing && !isEdgeShimmying && !isClimbingVault)
        {
            isCrouching = true;
        }
        else if (!isSqueezing && !isEdgeShimmying && !isClimbingVault)
        {
            isCrouching = HasCeilingAbove();
        }
        else
        {
            isCrouching = false;
        }

        // ✨ (ลบบรรทัด 330-334 เก่าออก เพราะย้ายไปคำนวณไว้ด้านบนสุดแล้ว)

        HandleCrouchingAndSqueezing();
        HandleStamina();
        HandleFootsteps();

        if (Time.time - lastJumpPressedTime <= jumpBufferTime && !isSqueezing && !isStunned && !isClimbingVault)
        {
            if (Time.time - lastGroundTime <= coyoteTime || jumpsLeft > 0)
            {
                DoJump();
                lastJumpPressedTime = -10f;
            }
        }

        UpdateHeadBob();
        UpdateProceduralArmAnimation();
    }

    #region Procedural Arm Animation & Visibility
    private void UpdateProceduralArmAnimation()
    {
        bool isWakingUp = PlayerWakeUpEffect.Instance != null && PlayerWakeUpEffect.Instance.IsWakingUp;
        bool shouldHideArms = isWakingUp || isSqueezing || isEdgeShimmying;

        if (leftArmSpriteRenderer != null) leftArmSpriteRenderer.enabled = !shouldHideArms;
        if (rightArmSpriteRenderer != null) rightArmSpriteRenderer.enabled = !shouldHideArms;

        if (shouldHideArms) return;

        UpdateArmSprites();

        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        float currentSpeed = isEdgeShimmying ? (Mathf.Abs(targetInput.x) * wallShimmySpeed) : horizontalVel.magnitude;
        bool isMoving = currentSpeed > 0.1f && targetInput.sqrMagnitude > 0.01f;

        Vector3 targetLeftPos = leftArmDefaultLocalPos;
        Vector3 targetRightPos = rightArmDefaultLocalPos;
        Quaternion targetLeftRot = leftArmDefaultLocalRot;
        Quaternion targetRightRot = rightArmDefaultLocalRot;

        if (isGrabbingLedge || isClimbingVault || isEdgeShimmying)
        {
            targetLeftPos += wallGrabArmOffset;
            targetRightPos += wallGrabArmOffset;

            float shimmySwing = Mathf.Sin(walkCyclePhase) * 0.03f;
            targetLeftPos.y += shimmySwing;
            targetRightPos.y -= shimmySwing;
        }
        else if (isSprinting)
        {
            float armSwing = Mathf.Sin(walkCyclePhase);
            float leftSwingY = armSwing * runArmSwingAmount;
            float leftSwingX = Mathf.Cos(walkCyclePhase) * (runArmSwingAmount * 0.5f);
            float rightSwingY = -armSwing * runArmSwingAmount;
            float rightSwingX = -Mathf.Cos(walkCyclePhase) * (runArmSwingAmount * 0.5f);

            targetLeftPos += raisedArmOffset + new Vector3(leftSwingX, leftSwingY, leftSwingY * 0.5f);
            targetRightPos += raisedArmOffset + new Vector3(rightSwingX, rightSwingY, rightSwingY * 0.5f);

            float rotZ = armSwing * runArmRotationAmount;
            targetLeftRot *= Quaternion.Euler(0, 0, rotZ);
            targetRightRot *= Quaternion.Euler(0, 0, -rotZ);
        }
        else if (isCrouching)
        {
            float armSwing = isMoving ? Mathf.Sin(walkCyclePhase) * crouchArmSwingAmount : 0f;
            targetLeftPos += raisedArmOffset + new Vector3(0, armSwing, 0);
            targetRightPos += raisedArmOffset + new Vector3(0, -armSwing, 0);
        }
        else
        {
            targetLeftPos += idleLoweredOffset;
            targetRightPos += idleLoweredOffset;
        }

        if (leftArmTransform != null)
        {
            leftArmTransform.localPosition = Vector3.Lerp(leftArmTransform.localPosition, targetLeftPos, Time.deltaTime * armLerpSpeed);
            leftArmTransform.localRotation = Quaternion.Slerp(leftArmTransform.localRotation, targetLeftRot, Time.deltaTime * armLerpSpeed);
        }

        if (rightArmTransform != null)
        {
            rightArmTransform.localPosition = Vector3.Lerp(rightArmTransform.localPosition, targetRightPos, Time.deltaTime * armLerpSpeed);
            rightArmTransform.localRotation = Quaternion.Slerp(rightArmTransform.localRotation, targetRightRot, Time.deltaTime * armLerpSpeed);
        }
    }

    private void UpdateArmSprites()
    {
        Sprite targetLeft = walkLeftArmSprite;
        Sprite targetRight = walkRightArmSprite;

        if (isGrabbingLedge || isClimbingVault || isEdgeShimmying)
        {
            if (wallGrabLeftArmSprite != null) targetLeft = wallGrabLeftArmSprite;
            if (wallGrabRightArmSprite != null) targetRight = wallGrabRightArmSprite;
        }
        else if (isSprinting)
        {
            if (sprintLeftArmSprite != null) targetLeft = sprintLeftArmSprite;
            if (sprintRightArmSprite != null) targetRight = sprintRightArmSprite;
        }

        if (leftArmSpriteRenderer != null && targetLeft != null && leftArmSpriteRenderer.sprite != targetLeft)
        {
            leftArmSpriteRenderer.sprite = targetLeft;
        }

        if (rightArmSpriteRenderer != null && targetRight != null && rightArmSpriteRenderer.sprite != targetRight)
        {
            rightArmSpriteRenderer.sprite = targetRight;
        }
    }
    #endregion

    #region Trigger-Based Climb & Shimmy Logic
    private void HandleWallClimbAndShimmyLogic()
    {
        if (isClimbingVault || isSqueezing || isStunned) return;

        if (isGrabbingLedge)
        {
            rb.linearVelocity = Vector3.zero;

            if (grounded)
            {
                isGrabbingLedge = false;
                rb.useGravity = true;
                return;
            }

            if (targetInput.z <= 0.1f)
            {
                requireFreshWPress = false;
            }

            bool jumpPressed = (jumpAction != null && jumpAction.WasPressedThisFrame());
            bool freshWPressed = (targetInput.z > 0.1f && !requireFreshWPress);

            if (freshWPressed || jumpPressed)
            {
                StartCoroutine(Routine_VaultClimbUp());
                return;
            }
            else if (targetInput.z < -0.1f)
            {
                isGrabbingLedge = false;
                rb.useGravity = true;
            }
        }
    }

    private IEnumerator Routine_VaultClimbUp()
    {
        isClimbingVault = true;
        isGrabbingLedge = false;
        requireFreshWPress = false;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * climbUpHeight + transform.forward * climbForwardDistance;

        PlaySingleSoundEffect(wallGrabEnterClip, volumeWallGrabEnter);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * climbSpeed;
            rb.MovePosition(Vector3.Lerp(startPos, targetPos, t));
            yield return null;
        }

        rb.useGravity = true;
        isClimbingVault = false;
        jumpsLeft = maxJumps;
        climbCooldownTimer = 0.4f;
    }

    private void DetachEdgeShimmy()
    {
        isEdgeShimmying = false;
        rb.useGravity = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(wallGapTag))
        {
            isInGapZone = true;
            currentGapTransform = other.transform;
        }

        if (enableEdgeShimmy && other.CompareTag(shimmyWallTag) && !isEdgeShimmying && !isClimbingVault)
        {
            isEdgeShimmying = true;
            currentShimmyTrigger = other.transform;
            shimmyWallNormal = other.transform.forward;
            rb.useGravity = false;
            jumpsLeft = maxJumps;

            PlaySingleSoundEffect(wallGrabEnterClip, volumeWallGrabEnter);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (enableClimbVault && other.CompareTag(climbableWallTag))
        {
            if (grounded || climbCooldownTimer > 0f)
            {
                if (isGrabbingLedge)
                {
                    isGrabbingLedge = false;
                    rb.useGravity = true;
                }
                return;
            }

            if (!isGrabbingLedge && !isClimbingVault)
            {
                float topOfTriggerY = other.bounds.max.y;
                float playerFeetY = (groundCheck != null) ? groundCheck.position.y : transform.position.y;

                if (playerFeetY < topOfTriggerY - ledgeGrabMaxHeightOffset)
                {
                    isGrabbingLedge = true;
                    currentClimbTrigger = other.transform;
                    rb.linearVelocity = Vector3.zero;
                    rb.useGravity = false;

                    if (targetInput.z > 0.1f)
                    {
                        requireFreshWPress = true;
                    }
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(wallGapTag))
        {
            isInGapZone = false;
            currentGapTransform = null;
        }

        if (other.CompareTag(shimmyWallTag) && isEdgeShimmying)
        {
            DetachEdgeShimmy();
        }

        if (other.CompareTag(climbableWallTag) && isGrabbingLedge)
        {
            isGrabbingLedge = false;
            rb.useGravity = true;
        }
    }
    #endregion

    #region Movement, Audio & Core Mechanics
    private void HandleFootsteps()
    {
        if (!grounded && !isSqueezing && !isEdgeShimmying) return;

        float currentSpeed = 0f;
        if (isEdgeShimmying)
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
            walkCyclePhase = 0f;
            nextStepPhase = Mathf.PI;
            return;
        }

        float stepInterval = baseStepInterval;
        float volume = volumeWalk;
        AudioClip[] targetClips = defaultFootstepClips;

        if (isEdgeShimmying)
        {
            stepInterval = baseStepInterval * 1.1f;
            volume = volumeWallShimmy;
            targetClips = (wallShimmyClips != null && wallShimmyClips.Length > 0) ? wallShimmyClips : defaultFootstepClips;
        }
        else if (isSqueezing)
        {
            stepInterval = baseStepInterval * 1.4f;
            volume = volumeSqueezeMove;
            targetClips = (squeezeMoveClips != null && squeezeMoveClips.Length > 0) ? squeezeMoveClips : defaultFootstepClips;
        }
        else if (isCrouching)
        {
            stepInterval = baseStepInterval * 1.6f;
            volume = volumeCrouch;
            targetClips = (crouchFootstepClips != null && crouchFootstepClips.Length > 0) ? crouchFootstepClips : defaultFootstepClips;
        }
        else if (isSprinting)
        {
            stepInterval = baseStepInterval * 0.78f;
            volume = volumeRun;
        }

        float speedMultiplier = isEdgeShimmying ? (currentSpeed / wallShimmySpeed) : (currentSpeed / walkSpeed);
        walkCyclePhase += (Time.deltaTime * speedMultiplier / stepInterval) * Mathf.PI;

        if (walkCyclePhase >= nextStepPhase)
        {
            PlayRandomAudioClip(targetClips, volume);
            nextStepPhase += Mathf.PI;
        }

        if (walkCyclePhase >= Mathf.PI * 2f)
        {
            walkCyclePhase -= Mathf.PI * 2f;
            nextStepPhase -= Mathf.PI * 2f;
        }
    }

    private void PlayRandomAudioClip(AudioClip[] clips, float volume)
    {
        if (footstepAudioSource == null || clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        footstepAudioSource.pitch = Random.Range(0.85f, 0.95f);
        footstepAudioSource.PlayOneShot(clip, volume);
    }

    private void PlaySingleSoundEffect(AudioClip clip, float volume)
    {
        if (footstepAudioSource == null || clip == null) return;
        footstepAudioSource.pitch = Random.Range(0.95f, 1.05f);
        footstepAudioSource.PlayOneShot(clip, volume);
    }
    private void HandleSqueezeInput()
    {
        // ✨ [แก้ไขบั๊ก] เช็กว่า Trigger ที่แคบยัง active หรือหลุดออกไปแล้วหรือไม่
        if (currentGapTransform != null && (!currentGapTransform.gameObject.activeInHierarchy || !currentGapTransform.GetComponent<Collider>().enabled))
        {
            isInGapZone = false;
            currentGapTransform = null;
        }

        if (isInGapZone && !isSqueezing && !isStunned && !isEdgeShimmying && targetInput.z > 0.1f)
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

            PlaySingleSoundEffect(squeezeEnterClip, volumeSqueezeEnter);
        }

        // ✨ หากไม่ได้อยู่ในเขต หรือ Collider หายไป ให้ยกเลิกการลอดกำแพงทันที
        if (!isInGapZone && isSqueezing)
        {
            isSqueezing = false;
        }
    }

    private void HandleFallStunLogic()
    {
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
        }

        if (!grounded && !isEdgeShimmying && !isGrabbingLedge)
        {
            if (wasGroundedLastFrame) highestYPoint = transform.position.y;
            else highestYPoint = Mathf.Max(highestYPoint, transform.position.y);
        }
        else if (!wasGroundedLastFrame && grounded)
        {
            float fallDistance = highestYPoint - transform.position.y;
            if (fallDistance >= minFallStunDistance) TriggerFallStun();
        }

        wasGroundedLastFrame = grounded;
    }

    private void TriggerFallStun()
    {
        isStunned = true;
        stunTimer = fallStunDuration;
        rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
        currentLandingImpact = landingImpactPitch;
        if (isEdgeShimmying) DetachEdgeShimmy();
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

        if (wantsToSprint && isMoving && !isCrouching && !isSqueezing && !isEdgeShimmying && !isStunned && currentStamina > 0f)
        {
            isSprinting = true;
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
            staminaRegenTimer = staminaRegenDelay;
        }
        else
        {
            isSprinting = false;

            if (staminaRegenTimer > 0f) staminaRegenTimer -= Time.deltaTime;
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
        if ((DialogueUIController.Instance != null && DialogueUIController.Instance.IsDialogueActive) ||
            (PlayerWakeUpEffect.Instance != null && PlayerWakeUpEffect.Instance.IsWakingUp) || isClimbingVault)
        {
            if (!isClimbingVault) rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            return;
        }

        if (isStunned || isGrabbingLedge)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (isEdgeShimmying)
        {
            Vector3 shimmyRight = (currentShimmyTrigger != null) ? currentShimmyTrigger.right : Vector3.Cross(Vector3.up, shimmyWallNormal).normalized;
            Vector3 shimmyVelocity = shimmyRight * targetInput.x * wallShimmySpeed;
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

            if (InventoryManager.Instance != null) speed *= InventoryManager.Instance.GetTotalSpeedMultiplier();

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

            if (InventoryManager.Instance != null) speed *= InventoryManager.Instance.GetTotalSpeedMultiplier();
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
            if (col.gameObject != gameObject && !col.transform.IsChildOf(transform)) return true;
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
            if (hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform)) return true;
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

        // ✨ [แก้ไขบั๊ก Double Jump สมบูรณ์]
        jumpsLeft--;
        grounded = false;
        jumpCooldownTimer = 0.2f; // หน่วงการเช็กพื้น 0.2 วินาที ป้องกันการรีเซ็ตสถานะแตะพื้นทันที
        lastGroundTime = -10f;    // ตัดสิทธิ์ Coyote Time ทันที
    }

    void UpdateHeadBob()
    {
        if (playerCamera == null) return;

        if (enableHeadBob && !isSqueezing && !isEdgeShimmying && !isClimbingVault && !isStunned)
        {
            Vector3 horizontalVel = rb.linearVelocity;
            horizontalVel.y = 0f;
            float speed = horizontalVel.magnitude;

            float currentMoveSpeedLimit = isCrouching ? crouchSpeed : walkSpeed;
            if (InventoryManager.Instance != null) currentMoveSpeedLimit *= InventoryManager.Instance.GetTotalSpeedMultiplier();

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
    #endregion

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
    }

    public void SetMouseSensitivity(float newSens)
    {
        mouseSensitivity = newSens;
    }

    public void SetFOV(float newFOV)
    {
        defaultFOV = newFOV;
        if (camComponent != null)
        {
            camComponent.fieldOfView = newFOV;
        }
    }

    // ✨ ดึงค่าความไวเมาส์มาตรฐานเดิมที่ตั้งไว้ใน Inspector (สำหรับให้ SettingsManager จัดวาง Slider ตรงกลาง)
    public float GetDefaultSensitivity() => initialSensitivity > 0 ? initialSensitivity : 2f;

    // ✨ ดึงค่า FOV มาตรฐานเดิมที่ตั้งไว้ใน Inspector/กล้อง (สำหรับให้ SettingsManager จัดวาง Slider ตรงกลาง)
    public float GetDefaultFOV() => initialFOV > 0 ? initialFOV : 60f;
}