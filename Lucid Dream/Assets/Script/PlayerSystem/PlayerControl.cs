using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))] // บังคับให้มี CapsuleCollider สำหรับระบบย่อตัว
public class PlayerController3D_InputAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform playerCamera;
    [SerializeField] Transform groundCheck;
    [SerializeField] InputActionAsset inputActionAsset;

    [Header("Movement")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float crouchSpeed = 2f; // ✨ ความเร็วตอนย่อตัว
    [SerializeField] float acceleration = 30f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 7f;
    [SerializeField] int maxJumps = 1;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;
    [Range(0f, 1f)][SerializeField] float variableJumpMultiplier = 0.5f;

    [Header("Ground Check (No Layer Needed)")]
    [SerializeField] float groundCheckRadius = 0.2f;

    [Header("Crouch Settings ✨")]
    [SerializeField] float crouchHeight = 1f;       // ความสูงตอนย่อ (ปกติแคปซูลของ Unity สูง 2)
    [SerializeField] float crouchSmoothing = 10f;   // ความเร็วในการหมอบ/ลุก

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
    private InputAction crouchAction; // ✨

    // Movement variables
    Rigidbody rb;
    CapsuleCollider capsuleCollider; // ✨
    Vector3 targetInput = Vector3.zero;
    Vector2 lookInput = Vector2.zero;
    int jumpsLeft;
    float lastGroundTime = -10f;
    float lastJumpPressedTime = -10f;
    bool grounded;
    float yaw = 0f;
    float pitch = 0f;

    // Crouch & Height variables
    private float defaultHeight;
    private float defaultCenterY;
    private bool isCrouching;
    private Vector3 currentBaseCameraPos; // ตำแหน่งฐานกล้องที่คำนวณการย่อตัวแล้ว

    // Head bob variables
    float bobTimer = 0f;
    Vector3 originalCameraPosition;
    Vector3 currentCameraOffset = Vector3.zero;
    Vector3 targetCameraOffset = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>(); // ✨

        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (groundCheck == null)
        {
            GameObject g = new GameObject("GroundCheck");
            g.transform.SetParent(transform);
            g.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            groundCheck = g.transform;
        }

        if (lockCursor) Cursor.lockState = CursorLockMode.Locked;

        // บันทึกค่าเริ่มต้นของคอลไลเดอร์และการย่อตัว
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

        // Initialize Input Actions
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
        crouchAction = playerActionMap.FindAction("Crouch"); // ✨ ดึงค่าปุ่มย่อตัว

        if (moveAction == null) Debug.LogError("'Move' action not found!");
        if (lookAction == null) Debug.LogError("'Look' action not found!");
        if (jumpAction == null) Debug.LogError("'Jump' action not found!");
        if (sprintAction == null) Debug.LogError("'Sprint' action not found!");
        if (crouchAction == null) Debug.LogWarning("'Crouch' action not found! Please create it in Input Actions.");

        if (jumpAction != null)
        {
            jumpAction.started += OnJumpPressed;
            jumpAction.canceled += OnJumpReleased;
        }

        playerActionMap.Enable();
    }

    void OnJumpPressed(InputAction.CallbackContext context)
    {
        // ถ้าย่อตัวอยู่ จะไม่สามารถกระโดดได้ (หรือเอาเงื่อนไข !isCrouching ออกหากต้องการให้กระโดดตอนย่อได้)
        if (!isCrouching)
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

        // 🛡️ [แก้ปัญหาเลเยอร์] เรียกใช้ฟังก์ชันเช็คพื้นแบบไม่สนเลเยอร์
        grounded = CheckGroundedNoLayer();
        if (grounded)
        {
            lastGroundTime = Time.time;
            jumpsLeft = maxJumps;
        }

        // ✨ ระบบอัปเดตสถานะและการประมวลผลการย่อตัว (Crouch Logic)
        if (crouchAction != null)
        {
            isCrouching = crouchAction.IsPressed();
        }
        HandleCrouching();

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

    void FixedUpdate()
    {
        Vector3 cameraRight = (playerCamera != null) ? playerCamera.right : transform.right;
        Vector3 cameraForward = (playerCamera != null) ? playerCamera.forward : transform.forward;

        cameraRight.y = 0f;
        cameraForward.y = 0f;
        cameraRight.Normalize();
        cameraForward.Normalize();

        // เช็คการวิ่ง (จะวิ่งไม่ได้ถ้าย่อตัวอยู่)
        bool running = false;
        if (sprintAction != null && !isCrouching)
        {
            running = sprintAction.IsPressed();
        }

        // ✨ คำนวณความเร็วตามสถานะ ย่อตัว / วิ่ง / เดินปกติ
        float speed = isCrouching ? crouchSpeed : (running ? runSpeed : walkSpeed);

        Vector3 desiredHorizontalVel = (cameraRight * targetInput.x + cameraForward * targetInput.z) * speed;

        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 newHorizontalVel = Vector3.MoveTowards(horizontalVel, desiredHorizontalVel, acceleration * Time.fixedDeltaTime);

        Vector3 newVel = newHorizontalVel + Vector3.up * currentVel.y;
        rb.linearVelocity = newVel;
    }

    // 🛡️ ฟังก์ชันเช็คพื้นอัจฉริยะ: แตะอะไรก็ได้ที่เป็นของแข็งก็นับหมด ยกเว้นตัวผู้เล่นเอง
    bool CheckGroundedNoLayer()
    {
        // ยิงทรงกลมตรวจสอบวัตถุทั้งหมดรอบจุด groundCheck (~0 คือเลือกเอาทุกเลเยอร์ในเกม)
        Collider[] hitColliders = Physics.OverlapSphere(groundCheck.position, groundCheckRadius, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider col in hitColliders)
        {
            // ถ้าวัตถุที่เจอไม่ใช่ตัวเราเอง และไม่ใช่ object ลูกที่อยู่ในตัวเรา แปลว่าเรายืนอยู่บนพื้นจริง!
            if (col.gameObject != gameObject && !col.transform.IsChildOf(transform))
            {
                return true;
            }
        }
        return false;
    }

    // ✨ ฟังก์ชันจัดการสไลด์หดตัวคอลไลเดอร์และลดระดับกล้องลงตอนย่อตัว
    void HandleCrouching()
    {
        if (capsuleCollider == null) return;

        // 1. ค่อยๆ ปรับความสูงของ Capsule Collider
        float targetHeight = isCrouching ? crouchHeight : defaultHeight;
        capsuleCollider.height = Mathf.Lerp(capsuleCollider.height, targetHeight, crouchSmoothing * Time.deltaTime);

        // 2. ปรับค่า Center Y ของแคปซูลสอดคล้องกับความสูง เพื่อให้เท้าผู้เล่นติดพื้นพอดี (ไม่ลอยหรือจมดิน)
        float halfHeightDifference = (defaultHeight - capsuleCollider.height) / 2f;
        capsuleCollider.center = new Vector3(capsuleCollider.center.x, defaultCenterY - halfHeightDifference, capsuleCollider.center.z);

        // 3. ค่อยๆ ยุบตำแหน่งฐานของกล้องลงมาตามความสูงที่หายไป
        float targetCameraY = isCrouching ? originalCameraPosition.y - (defaultHeight - crouchHeight) * 0.5f : originalCameraPosition.y;
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
        if (!enableHeadBob || playerCamera == null) return;

        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        float speed = horizontalVel.magnitude;

        // อิงความถี่ตามความเร็วปัจจุบัน (ถ้าย่อเดิน head bob ก็จะช้าลงตามความเหมาะสม)
        float currentMoveSpeedLimit = isCrouching ? crouchSpeed : walkSpeed;
        if (speed > 0.1f)
        {
            bobTimer += Time.deltaTime * headBobFrequency * (speed / currentMoveSpeedLimit);
        }

        float bobX = Mathf.Sin(bobTimer * Mathf.PI * 2f) * swayAmount;
        float bobY = Mathf.Sin(bobTimer * Mathf.PI * 4f) * headBobAmount;

        targetCameraOffset = new Vector3(bobX, bobY, 0f);
        currentCameraOffset = Vector3.Lerp(currentCameraOffset, targetCameraOffset, headBobSmoothing * Time.deltaTime);

        // ✨ เปลี่ยนจากคำนวณอิง originalCameraPosition มาเป็น currentBaseCameraPos เพื่อให้ส่ายหัวขณะย่อตัวได้สมบูรณ์แบบ
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
    }
}