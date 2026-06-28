using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController3D_InputAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform playerCamera;
    [SerializeField] Transform groundCheck;
    [SerializeField] LayerMask groundLayer;
    [SerializeField] InputActionAsset inputActionAsset; // Assign your Input Actions here

    [Header("Movement")]
    [SerializeField] float walkSpeed = 4f;
    [SerializeField] float runSpeed = 8f;
    [SerializeField] float acceleration = 30f;

    [Header("Jump")]
    [SerializeField] float jumpForce = 7f;
    [SerializeField] int maxJumps = 1;
    [SerializeField] float coyoteTime = 0.12f;
    [SerializeField] float jumpBufferTime = 0.12f;
    [Range(0f, 1f)][SerializeField] float variableJumpMultiplier = 0.5f;

    [Header("Ground Check")]
    [SerializeField] float groundCheckRadius = 0.2f;

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

    // Movement variables
    Rigidbody rb;
    Vector3 targetInput = Vector3.zero;
    Vector2 lookInput = Vector2.zero;
    int jumpsLeft;
    float lastGroundTime = -10f;
    float lastJumpPressedTime = -10f;
    bool grounded;
    float yaw = 0f;
    float pitch = 0f;

    // Head bob variables
    float bobTimer = 0f;
    Vector3 originalCameraPosition;
    Vector3 currentCameraOffset = Vector3.zero;
    Vector3 targetCameraOffset = Vector3.zero;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;

        if (groundCheck == null)
        {
            GameObject g = new GameObject("GroundCheck");
            g.transform.SetParent(transform);
            g.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            groundCheck = g.transform;
        }

        if (lockCursor) Cursor.lockState = CursorLockMode.Locked;

        // Store original camera position
        if (playerCamera != null)
        {
            originalCameraPosition = playerCamera.localPosition;
        }

        // Initialize Input Actions
        SetupInputActions();
    }

    void SetupInputActions()
    {
        // Get or create input actions
        if (inputActionAsset == null)
        {
            Debug.LogError("InputActionAsset not assigned! Please assign your Input Actions in the Inspector.");
            return;
        }

        // Get the Player action map (adjust name if different)
        playerActionMap = inputActionAsset.FindActionMap("Player");
        if (playerActionMap == null)
        {
            Debug.LogError("'Player' action map not found in InputActionAsset!");
            return;
        }

        // Get individual actions
        moveAction = playerActionMap.FindAction("Move");
        lookAction = playerActionMap.FindAction("Look");
        jumpAction = playerActionMap.FindAction("Jump");
        sprintAction = playerActionMap.FindAction("Sprint");

        // Validate actions exist
        if (moveAction == null) Debug.LogError("'Move' action not found!");
        if (lookAction == null) Debug.LogError("'Look' action not found!");
        if (jumpAction == null) Debug.LogError("'Jump' action not found!");
        if (sprintAction == null) Debug.LogError("'Sprint' action not found!");

        // Subscribe to jump action
        if (jumpAction != null)
        {
            jumpAction.started += OnJumpPressed;
            jumpAction.canceled += OnJumpReleased;
        }

        // Enable the action map
        playerActionMap.Enable();
    }

    void OnJumpPressed(InputAction.CallbackContext context)
    {
        lastJumpPressedTime = Time.time;
    }

    void OnJumpReleased(InputAction.CallbackContext context)
    {
        // Variable jump height
        if (rb.linearVelocity.y > 0f)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y *= variableJumpMultiplier;
            rb.linearVelocity = vel;
        }
    }

    void Update()
    {
        // Get input values
        if (moveAction != null)
        {
            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            targetInput = new Vector3(moveInput.x, 0f, moveInput.y);
        }

        if (lookAction != null)
        {
            lookInput = lookAction.ReadValue<Vector2>();
        }

        // Mouse look (yaw rotates player around Y, pitch rotates camera)
        Vector2 mouse = lookInput * (mouseSensitivity * 0.01f);
        yaw += mouse.x;
        pitch -= mouse.y;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.eulerAngles = new Vector3(0f, yaw, 0f);
        if (playerCamera != null) playerCamera.localEulerAngles = new Vector3(pitch, 0f, 0f);

        // Ground check
        grounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer, QueryTriggerInteraction.Ignore);
        if (grounded)
        {
            lastGroundTime = Time.time;
            jumpsLeft = maxJumps;
        }

        // Jump buffer + coyote
        if (Time.time - lastJumpPressedTime <= jumpBufferTime)
        {
            if (Time.time - lastGroundTime <= coyoteTime || jumpsLeft > 0)
            {
                DoJump();
                lastJumpPressedTime = -10f;
            }
        }

        // Head bob update
        UpdateHeadBob();
    }

    void FixedUpdate()
    {
        // Get camera directions (horizontal plane)
        Vector3 cameraRight = (playerCamera != null) ? playerCamera.right : transform.right;
        Vector3 cameraForward = (playerCamera != null) ? playerCamera.forward : transform.forward;

        // Project to horizontal plane to avoid vertical tilt from camera pitch
        cameraRight.y = 0f;
        cameraForward.y = 0f;
        cameraRight.Normalize();
        cameraForward.Normalize();

        // Check sprint
        bool running = false;
        if (sprintAction != null)
        {
            running = sprintAction.IsPressed();
        }
        float speed = running ? runSpeed : walkSpeed;

        // Calculate desired velocity from input
        Vector3 desiredHorizontalVel = (cameraRight * targetInput.x + cameraForward * targetInput.z) * speed;

        // Preserve vertical velocity and apply movement with acceleration
        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(currentVel.x, 0f, currentVel.z);
        Vector3 newHorizontalVel = Vector3.MoveTowards(horizontalVel, desiredHorizontalVel, acceleration * Time.fixedDeltaTime);

        Vector3 newVel = newHorizontalVel + Vector3.up * currentVel.y;
        rb.linearVelocity = newVel;
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

        // Calculate horizontal velocity
        Vector3 horizontalVel = rb.linearVelocity;
        horizontalVel.y = 0f;
        float speed = horizontalVel.magnitude;

        // If moving, increment bob timer
        if (speed > 0.1f)
        {
            bobTimer += Time.deltaTime * headBobFrequency * (speed / walkSpeed);
        }

        // Calculate head bob offset
        float bobX = Mathf.Sin(bobTimer * Mathf.PI * 2f) * swayAmount;
        float bobY = Mathf.Sin(bobTimer * Mathf.PI * 4f) * headBobAmount;

        targetCameraOffset = new Vector3(bobX, bobY, 0f);

        // Smooth transition
        currentCameraOffset = Vector3.Lerp(currentCameraOffset, targetCameraOffset, headBobSmoothing * Time.deltaTime);

        // Apply offset to camera
        playerCamera.localPosition = originalCameraPosition + currentCameraOffset;
    }

    void OnDisable()
    {
        // Unsubscribe from events and disable actions
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpPressed;
            jumpAction.canceled -= OnJumpReleased;
        }

        if (playerActionMap != null)
        {
            playerActionMap.Disable();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}