using UnityEngine;

/// <summary>
/// A freecam-style character controller that uses Unity's Rigidbody for physics-based movement.
/// Supports WASD movement, QE for elevation, and mouse look.
/// Designed for smooth flying/floating movement.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class FreecamCharacterController : BaseCharacterController
{
    [Header("Movement Settings")]
    [Tooltip("Base movement speed in units per second")]
    [SerializeField] private float moveSpeed = 5f;
    
    [Tooltip("Speed multiplier when holding Shift (sprint)")]
    [SerializeField] private float sprintMultiplier = 2f;
    
    [Tooltip("Speed multiplier when holding Ctrl (slow mode)")]
    [SerializeField] private float slowMultiplier = 0.5f;

    [Header("Mouse Movement Settings")]
    [Tooltip("Mouse sensitivity for horizontal movement (left/right)")]
    [SerializeField] private float mouseSensitivityX = 2f;
    
    [Tooltip("Mouse sensitivity for vertical movement (up/down)")]
    [SerializeField] private float mouseSensitivityY = 2f;
    
    [Tooltip("Invert vertical mouse movement")]
    [SerializeField] private bool invertY = false;

    [Header("Input Settings")]
    [Tooltip("Key to hold for sprint mode")]
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    
    [Tooltip("Key to hold for slow mode")]
    [SerializeField] private KeyCode slowKey = KeyCode.LeftControl;
    
    [Tooltip("Key for upward movement (alternative to mouse)")]
    [SerializeField] private KeyCode upKey = KeyCode.E;
    
    [Tooltip("Key for downward movement (alternative to mouse)")]
    [SerializeField] private KeyCode downKey = KeyCode.Q;
    
    [Tooltip("Enable/disable mouse movement control")]
    [SerializeField] private bool enableMouseMovement = true;
    
    [Tooltip("Key to toggle mouse movement on/off")]
    [SerializeField] private KeyCode toggleMouseKey = KeyCode.M;
    
    [Tooltip("Use main camera's look direction as forward (camera-relative movement)")]
    [SerializeField] private bool useCameraForward = false;

    // Private variables
    private Camera playerCamera;
    
    // Input tracking
    private Vector2 movementInput;
    private Vector2 mouseInput;
    private float verticalInput;
    
    // Rotation tracking
    private float pitch = 0f;
    private float yaw = 0f;

    // Properties for external access
    public float MoveSpeed { get => moveSpeed; set => moveSpeed = Mathf.Max(0f, value); }
    public float MouseSensitivityX { get => mouseSensitivityX; set => mouseSensitivityX = Mathf.Max(0f, value); }
    public float MouseSensitivityY { get => mouseSensitivityY; set => mouseSensitivityY = Mathf.Max(0f, value); }
    public bool EnableMouseMovement
    {
        get => enableMouseMovement;
        set { enableMouseMovement = value; UpdateCursorState(); }
    }
    public bool UseCameraForward { get => useCameraForward; set => useCameraForward = value; }

    protected override void Start()
        {
        base.Start();

            playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogWarning("No main camera found for FreecamCharacterController.");
        }
        UpdateCursorState();
    }

    private void Update()
    {
        HandleInput();
        if (showDebug) LogDebugInfo();
    }

    private void HandleInput()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        
        verticalInput = 0f;
        if (Input.GetKey(upKey)) verticalInput += 1f;
        if (Input.GetKey(downKey)) verticalInput -= 1f;
        
        if (enableMouseMovement)
        {
            mouseInput.x = Input.GetAxis("Mouse X");
            mouseInput.y = Input.GetAxis("Mouse Y");
            
            yaw += mouseInput.x * mouseSensitivityX;
            pitch -= mouseInput.y * mouseSensitivityY;
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        
        if (Input.GetKeyDown(toggleMouseKey))
        {
            EnableMouseMovement = !EnableMouseMovement;
        }
    }

    protected override void UpdateMovement(float deltaTime)
    {
        // Don't accept user input during bounce state
        if (!CanAcceptInput)
        {
            targetVelocity = Vector3.zero;
            return;
        }

        float currentSpeedMultiplier = 1f;
        if (Input.GetKey(sprintKey)) currentSpeedMultiplier = sprintMultiplier;
        else if (Input.GetKey(slowKey)) currentSpeedMultiplier = slowMultiplier;
        
        float currentMoveSpeed = moveSpeed * currentSpeedMultiplier;

        Vector3 moveDirection = (transform.forward * movementInput.y + transform.right * movementInput.x).normalized;
        
        targetVelocity = moveDirection * currentMoveSpeed;
        targetVelocity.y += verticalInput * currentMoveSpeed;

        Vector3 force = (targetVelocity - rb.linearVelocity) * moveForce * deltaTime;
        
        // If gravity is off, we need to manually control vertical forces
        if (!enableGravity)
        {
             Vector3 verticalForce = Vector3.up * verticalInput * moveForce * deltaTime;
             force += verticalForce;
        }
       
        rb.AddForce(force);
    }

    protected override void LogDebugInfo()
    {
        base.LogDebugInfo(); // Call base class bounce debug info
        
        Debug.Log($"FreecamController - State: {CurrentMovementState}, Pos: {transform.position:F2}, Vel: {GetSpeed():F2}, " +
                  $"Input: WASD({movementInput.x:F1},{movementInput.y:F1}) QE({verticalInput:F1}) Mouse({mouseInput.x:F1},{mouseInput.y:F1})");
    }

    public void SetRotation(float newPitch, float newYaw)
    {
        pitch = Mathf.Clamp(newPitch, -90f, 90f);
        yaw = newYaw;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    public void ToggleMouseMovement() => EnableMouseMovement = !EnableMouseMovement;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        moveSpeed = Mathf.Max(0f, moveSpeed);
        sprintMultiplier = Mathf.Max(1f, sprintMultiplier);
        slowMultiplier = Mathf.Clamp(slowMultiplier, 0.1f, 1f);
        mouseSensitivityX = Mathf.Max(0f, mouseSensitivityX);
        mouseSensitivityY = Mathf.Max(0f, mouseSensitivityY);
    }
#endif

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && enableMouseMovement) UpdateCursorState();
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void UpdateCursorState()
    {
        Cursor.lockState = enableMouseMovement ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enableMouseMovement;
    }
} 