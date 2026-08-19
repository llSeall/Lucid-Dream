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

    [Header("Stamina Settings ✨")]
    [SerializeField] float maxStamina = 100f;
    [SerializeField] float staminaDrainRate = 25f;     // อัตรา Stamina ลดลงต่อวินาที
    [SerializeField] float staminaRegenRate = 15f;     // อัตรา Stamina ฟื้นฟูต่อวินาที
    [SerializeField] float staminaRegenDelay = 1f;     // เวลาคูลดาวน์ก่อนเริ่มฟื้นฟู Stamina
    [SerializeField] RectTransform staminaFillRect;     // ✨ RectTransform ของ "เนื้อหลอด" (Pivot X ต้องเป็น 0.5)
    [SerializeField] CanvasGroup staminaCanvasGroup;   // ✨ CanvasGroup ของ "กล่อง UI Stamina ทั้งหมด" ใช้คุมซ่อน/แสดง
    [SerializeField] bool hideWhenFull = true;         // ✨ ติ๊กถูกเพื่อซ่อนหลอดตอนเต็ม (รวมถึงตอนเริ่มเกม)
    [SerializeField] float fadeSpeed = 5f;             // ✨ ความเร็วในการจางเข้า-ออก ของหลอด UI

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

    // Stamina variables
    private float currentStamina;
    private float staminaRegenTimer;
    private bool isSprinting;

    // Crouch & Height variables
    private float defaultHeight;
    private float defaultCenterY;
    private bool isCrouching;
    private Vector3 currentBaseCameraPos;

    // Head bob variables
    float bobTimer = 0f;
    Vector3 originalCameraPosition;
    Vector3 currentCameraOffset = Vector3.zero;
    Vector3 targetCameraOffset = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

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
        }

        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.localPosition;
            currentBaseCameraPos = originalCameraPosition;
        }

        currentStamina = maxStamina; // Stamina เต็มเมื่อเริ่มต้น

        // ✨ สั่งให้ซ่อนหลอด UI ตั้งแต่เริ่มเกมทันที
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
            Debug.LogError("InputActionAsset not assigned! Please assign your Input Actions in the Inspector.");
            return;
        }

        playerActionMap = inputActionAsset.FindActionMap("Player");
        if (playerActionMap == null)
        {
            Debug.LogError("'Player' action map not found in InputActionAsset!");
            return;
        }

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

        if (!isCrouching)
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

        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            targetInput = new Vector3(moveInput.x, 0f, moveInput.y);
        }

        if (lookAction != null) lookInput = lookAction.ReadValue<Vector2>();

        Vector2 mouse = lookInput * (mouseSensitivity * 0.01f);
        yaw += mouse.x;
        pitch -= mouse.y;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.eulerAngles = new Vector3(0f, yaw, 0f);
        if (playerCamera != null) playerCamera.localEulerAngles = new Vector3(pitch, 0f, 0f);

        grounded = CheckGroundedNoLayer();
        if (grounded)
        {
            lastGroundTime = Time.time;
            jumpsLeft = maxJumps;
        }

        // Crouch
        bool crouchKeyPressed = (crouchAction != null) && crouchAction.IsPressed();
        if (crouchKeyPressed)
        {
            isCrouching = true;
        }
        else
        {
            isCrouching = HasCeilingAbove();
        }
        HandleCrouching();

        // Stamina & UI
        HandleStamina();

        // Jump buffer + coyote
        if (Time.time - lastJumpPressedTime <= jumpBufferTime)
        {
            if (Time.time - lastGroundTime <= coyoteTime || jumpsLeft > 0)
            {
                DoJump();
                lastJumpPressedTime = -10f;
            }
        }

        UpdateHeadBob();
    }

    // ✨ ฟังก์ชันคำนวณ Stamina, ยุบเนื้อหลอดเข้าตรงกลาง และควบคุมการซ่อน/แสดงแบบนุ่มนวล
    void HandleStamina()
    {
        bool wantsToSprint = (sprintAction != null) && sprintAction.IsPressed();
        bool isMoving = targetInput.sqrMagnitude > 0.01f;

        // จะลด Stamina ก็ต่อเมื่อ: กดวิ่ง + กำลังเดินขยับตัวอยู่ + ไม่ได้ย่อตัว + มี Stamina เหลืออยู่
        if (wantsToSprint && isMoving && !isCrouching && currentStamina > 0f)
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

        // 1. ปรับขนาดเฉพาะ "เนื้อหลอด (Fill)" ให้ยุบเข้าตรงกลาง
        if (staminaFillRect != null)
        {
            float staminaRatio = currentStamina / maxStamina;
            staminaFillRect.localScale = new Vector3(staminaRatio, 1f, 1f);
        }

        // 2. ควบคุมการซ่อน-แสดง UI แบบจางเข้า-ออก (Fade In / Fade Out)
        if (staminaCanvasGroup != null)
        {
            // ถ้าเลือกซ่อนตอนเต็ม และ Stamina เต็มพอดี -> เป้าหมายคือจางหายไป (Alpha = 0)
            // ถ้าระดับ Stamina ลดลง -> เป้าหมายคือแสดงขึ้นมา (Alpha = 1)
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
        Vector3 desiredHorizontalVel = (cameraRight * targetInput.x + cameraForward * targetInput.z) * speed;

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

    void HandleCrouching()
    {
        if (capsuleCollider == null) return;

        float targetHeight = isCrouching ? crouchHeight : defaultHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchSmoothing * Time.deltaTime);

        float halfHeightDifference = (defaultHeight - capsuleCollider.height) / 2f;
        capsuleCollider.center = new Vector3(capsuleCollider.center.x, defaultCenterY - halfHeightDifference, capsuleCollider.center.z);

        float targetCameraY = isCrouching ? (originalCameraPosition.y - crouchCameraYOffset) : originalCameraPosition.y;
        currentBaseCameraPos.y = Mathf.Lerp(currentBaseCameraPos.y, targetCameraY, crouchSmoothing * Time.deltaTime);
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

        if (enableHeadBob)
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

        if (capsuleCollider != null)
        {
            Gizmos.color = isCrouching ? Color.red : Color.cyan;
            float radius = capsuleCollider.radius * 0.85f;
            Vector3 origin = transform.position + Vector3.up * (crouchHeight - radius);
            float checkDistance = defaultHeight - crouchHeight;
            Gizmos.DrawWireSphere(origin + Vector3.up * checkDistance, radius);
        }
    }
}