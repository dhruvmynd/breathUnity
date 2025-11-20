using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands.Gestures;

public enum MovementState
{
    Normal,
    Bouncing
}

public enum BounceType
{
    Impulse,
    GentleForce
}

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public abstract class BaseCharacterController : MonoBehaviour
{
    [Header("Components")]
    [Tooltip("The Rigidbody component to move.")]
    [SerializeField] protected Rigidbody rb;
    public Rigidbody Rigidbody => rb;

    [Header("Common Movement Settings")]
    [Tooltip("The force applied to move the character.")]
    [SerializeField] protected float moveForce = 200f;
    [Tooltip("Maximum speed the character can reach. The character's velocity will be clamped to this value.")]
    [SerializeField] protected float maxSpeed = 10f;
    
    [Header("Common Physics Settings")]
    [Tooltip("Whether to apply gravity via the Rigidbody.")]
    [SerializeField] protected bool enableGravity = true;

    [Header("Collision Settings")]
    [Tooltip("Enable a 'bounce' effect upon collision.")]
    [SerializeField] protected bool enableCollisionBounce = true;
    [Tooltip("The force of the bounce-back effect.")]
    [SerializeField] protected float bounceForce = 10f;
    [Tooltip("Type of bounce force to apply: Impulse for instant bounce, GentleForce for gradual bounce.")]
    [SerializeField] protected BounceType bounceType = BounceType.Impulse;
    [Tooltip("Bounce direction is based on the camera's look direction instead of the collision angle.")]
    [SerializeField] protected bool bounceOffCameraLook = false;

    [Tooltip("Multiplier for the horizontal bounce force (for continuous bouncing).")]
    [SerializeField] protected float bounceHorizontalMultiplier = 0.1f;

    [Header("Bounce State Settings")]
    [Tooltip("Speed below which the character exits bounce state and allows user input again.")]
    [SerializeField] protected float exitBounceMinSpeed = 2f;
    [Tooltip("Time in seconds before bounce state can be exited (prevents rapid state changes).")]
    [SerializeField] protected float minimumBounceTime = 0.2f;
    [Tooltip("Allow continuous bouncing - new bounces can be triggered even when already in bounce state.")]
    [SerializeField] protected bool allowContinuousBounce = false;
    [Tooltip("Allow breathing/other movement during bounce state (additive instead of exclusive).")]
    [SerializeField] protected bool allowMovementDuringBounce = true;
    [Tooltip("Scale factor for other movement during bounce (0 = disabled, 1 = full strength).")]
    [Range(0f, 1f)]
    [SerializeField] protected float movementScaleDuringBounce = 0.3f;
    
    [Header("Bounce/Breath Interaction")]
    [Tooltip("Allow breath movement to continue at full strength during bounce.")]
    [SerializeField] protected bool allowBreathMovementDuringBounce = true;
    [Tooltip("Scale factor specifically for breath movement during bounce (0 = disabled, 1 = full strength).")]
    [Range(0f, 1f)]
    [SerializeField] protected float breathMovementScaleDuringBounce = 1.0f;

    [Header("Ground Check")]
    [Tooltip("Layer mask for what is considered ground.")]
    [SerializeField] protected LayerMask groundLayer;
    [Tooltip("How far below the character to cast for ground detection.")]
    [SerializeField] protected float groundCheckDistance = 0.2f;
    protected bool isGrounded;

    [Header("Debugging")]
    [Tooltip("Show debug information")]
    [SerializeField] protected bool showDebug = false;

    [Header("Bounce State Events")]
    [Tooltip("Called when entering bounce state")]
    public UnityEvent<Vector3> OnBounceEnter = new UnityEvent<Vector3>();
    [Tooltip("Called while staying in bounce state (each FixedUpdate during bounce)")]
    public UnityEvent<float> OnBounceStay = new UnityEvent<float>();
    [Tooltip("Called when exiting bounce state")]
    public UnityEvent OnBounceExit = new UnityEvent();

    protected Vector3 targetVelocity; // Used by children to specify desired movement direction and speed
    
    // Bounce state tracking
    protected MovementState currentState = MovementState.Normal;
    protected float bounceStateStartTime;
    protected Vector3 lastBounceForce;
    protected Vector3 gentleBounceForce;

    public MovementState CurrentMovementState => currentState;
    public virtual bool CanAcceptInput => currentState == MovementState.Normal || allowMovementDuringBounce;
    public BounceType BounceType { get => bounceType; set => bounceType = value; }
    public bool AllowContinuousBounce { get => allowContinuousBounce; set => allowContinuousBounce = value; }
    public bool AllowMovementDuringBounce { get => allowMovementDuringBounce; set => allowMovementDuringBounce = value; }
    public float MovementScaleDuringBounce { get => movementScaleDuringBounce; set => movementScaleDuringBounce = value; }
    public bool AllowBreathMovementDuringBounce { get => allowBreathMovementDuringBounce; set => allowBreathMovementDuringBounce = value; }
    public float BreathMovementScaleDuringBounce { get => breathMovementScaleDuringBounce; set => breathMovementScaleDuringBounce = value; }


    private bool isInCollision = false;
    public bool IsInCollision => isInCollision;

    protected virtual void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        // Ensure the Rigidbody doesn't have its own rotation physics interfering
        rb.freezeRotation = true;
    }

    protected virtual void Start()
    {
        rb.useGravity = enableGravity;
    }

    protected virtual void OnEnable()
    {
        // When this controller is enabled, exit bounce state to reset any ongoing bounce effects
        if (currentState == MovementState.Bouncing)
        {
            ExitBounceState();
        }
    }

    protected virtual void OnDisable()
    {
        // When this controller is disabled, reset its state and velocity influence
        if (currentState == MovementState.Bouncing)
        {
            ExitBounceState();
        }
        targetVelocity = Vector3.zero;
    }

    protected virtual void FixedUpdate()
    {
        // Only process if this is the active controller
        if (!IsActiveController()) return;
        
        HandleGroundCheck();
        HandleBounceState();
        HandleGentleBounce();
        UpdateMovement(Time.fixedDeltaTime);
        ClampSpeed();
    }
    
    protected abstract void UpdateMovement(float deltaTime);

    protected virtual void HandleGroundCheck()
    {
        // Use a sphere cast for more reliable ground detection
        isGrounded = Physics.SphereCast(
            transform.position + Vector3.up * (groundCheckDistance * 2), 
            GetComponent<CapsuleCollider>().radius, 
            Vector3.down, 
            out _, 
            groundCheckDistance * 3, 
            groundLayer);
    }

    protected virtual void HandleBounceState()
    {
        if (currentState == MovementState.Bouncing)
        {
            float timeSinceBounce = Time.time - bounceStateStartTime;
            float currentSpeed = rb.linearVelocity.magnitude;
            
            // Call OnBounceStay event with current speed
            OnBounceStay.Invoke(currentSpeed);
            
            // Exit bounce state if enough time has passed and speed is low enough
            if (timeSinceBounce >= minimumBounceTime && currentSpeed <= exitBounceMinSpeed)
            {
                ExitBounceState();
            }
        }
    }

    protected virtual void HandleGentleBounce()
    {
        if (currentState == MovementState.Bouncing && bounceType == BounceType.GentleForce && gentleBounceForce.magnitude > 0.1f)
        {
            // Apply gentle force over time
            rb.AddForce(gentleBounceForce * Time.fixedDeltaTime);
            
            // Gradually reduce the gentle bounce force
            gentleBounceForce = Vector3.Lerp(gentleBounceForce, Vector3.zero, Time.fixedDeltaTime * 2f);
        }
    }
    
    protected virtual bool IsActiveController()
    {
        // Check if this is the only enabled character controller, or if multiple are enabled, 
        // prioritize based on component order (first enabled wins)
        BaseCharacterController[] controllers = GetComponents<BaseCharacterController>();
        foreach (var controller in controllers)
        {
            if (controller.enabled)
            {
                return controller == this;
            }
        }
        return false;
    }
    
    protected virtual void ClampSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    protected virtual void EnterBounceState(Vector3 bounceForce)
    {
        currentState = MovementState.Bouncing;
        bounceStateStartTime = Time.time;
        lastBounceForce = bounceForce;
        
        // Call OnBounceEnter event with bounce force
        OnBounceEnter.Invoke(bounceForce);
        
        if (showDebug)
        {
            Debug.Log($"Entered bounce state with force: {bounceForce.magnitude:F2}");
        }
    }

    protected virtual void ExitBounceState()
    {

        currentState = MovementState.Normal;
        
        // Reset gentle bounce force when exiting bounce state
        gentleBounceForce = Vector3.zero;
        
        // Call OnBounceExit event
        OnBounceExit.Invoke();
        
        if (showDebug)
        {
            Debug.Log("Exited bounce state - user input enabled");
        }
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        isInCollision = true;
        // Only handle collision if this is the active controller
        if (!IsActiveController()) return;
        
        if (!enableCollisionBounce || bounceForce <= 0f) return;

        // Ignore collisions with non-static objects to prevent bouncing off of small physics props
        if (collision.rigidbody != null) return;

        Vector3 bounceDirection;
        if (bounceOffCameraLook && Camera.main != null)
        {
            // Bounce directly away from where the camera is looking
            bounceDirection = -Camera.main.transform.forward;
        }
        else
        {
            // Average the normals of all contact points for a more stable bounce direction
            bounceDirection = Vector3.zero;
            foreach (var contact in collision.contacts)
            {
                bounceDirection += contact.normal;
            }
            bounceDirection.Normalize();
        }

        Vector3 finalBounceForce = bounceDirection * bounceForce;
        
        // Apply bounce force based on type
        if (bounceType == BounceType.Impulse)
        {
            // Apply an instant force impulse for the bounce
            rb.AddForce(finalBounceForce, ForceMode.Impulse);
        }
        else if (bounceType == BounceType.GentleForce)
        {
            // Store force for gradual application
            gentleBounceForce = finalBounceForce * 100f; // Scale up for gentle force
        }
        
        // Always enter bounce state on collision
        EnterBounceState(finalBounceForce);

    }

    protected virtual void OnCollisionExit(Collision collision)
    {
        isInCollision = false;
    }

    protected virtual void LogDebugInfo() 
    { 
        if (showDebug && currentState == MovementState.Bouncing)
        {
            float timeSinceBounce = Time.time - bounceStateStartTime;
            Debug.Log($"Bounce State - Time: {timeSinceBounce:F2}s, Speed: {rb.linearVelocity.magnitude:F2}, Force: {lastBounceForce.magnitude:F2}");
        }
    }

    public void SetPosition(Vector3 position)
    {
        if (rb != null)
        {
            rb.position = position;
            ResetVelocity();
        }
    }

    public virtual void ResetVelocity()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        targetVelocity = Vector3.zero;
        
        // Reset bounce state when velocity is manually reset
        if (currentState == MovementState.Bouncing)
        {
            ExitBounceState();
        }
    }

    public virtual void KickOffBounceBackward(ScriptableObject so){

        var handShape = so as XRHandShape;
        // Use the current rigidbody velocity for the bounce backward call
        Vector3 currentVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        KickOffBounceBackward(currentVelocity);
    }

    public virtual void KickOffBounceHorizontal(Vector3 velocity){
        if (Camera.main == null)
        {
            Debug.LogWarning("KickOffBounceHorizontal: No main camera found!");
            return;
        }
        
        // Calculate horizontal velocity component relative to camera
        Vector3 cameraRight = Camera.main.transform.right;
        float horizontalDot = Vector3.Dot(velocity, cameraRight);
        
        // For palm swipes: swipe right-to-left should bounce right, swipe left-to-right should bounce left
        // So we invert the direction - negative dot (leftward swipe) bounces right, positive dot (rightward swipe) bounces left
        if (Mathf.Abs(horizontalDot) > 0.1f) // Threshold to avoid tiny movements
        {
            if (horizontalDot < 0) // Swipe left (negative), bounce right
            {
                KickOffBounceRight(velocity * bounceHorizontalMultiplier);
            }
            else // Swipe right (positive), bounce left  
            {
                KickOffBounceLeft(velocity * bounceHorizontalMultiplier);
            }
            
            if (showDebug)
            {
                string swipeDirection = horizontalDot < 0 ? "left" : "right";
                string bounceDirection = horizontalDot < 0 ? "right" : "left";
                Debug.Log($"Palm swipe {swipeDirection} detected (dot: {horizontalDot:F2}) → bouncing {bounceDirection}");
            }
        }
        else if (showDebug)
        {
            Debug.Log($"Horizontal velocity too small to trigger bounce: {horizontalDot:F2}");
        }
    }

    /// <summary>
    /// Base method for bouncing the character in a specific direction relative to the main camera.
    /// </summary>
    /// <param name="cameraRelativeDirection">Direction relative to camera (e.g., transform.right for right, -transform.forward for backward)</param>
    /// <param name="velocity">The velocity that triggered this bounce</param>
    /// <param name="directionName">Name of the direction for debug logging</param>
    protected virtual void KickOffBounceInDirection(System.Func<Transform, Vector3> cameraRelativeDirection, Vector3 velocity, string directionName, ForceMode forceMode = ForceMode.Impulse)
    {
        // Only handle if this is the active controller
        if (!IsActiveController()) return;
        
        // Check if we can bounce (either not bouncing, or continuous bouncing is allowed)
        if (!allowContinuousBounce && currentState == MovementState.Bouncing)
        {
            if (showDebug)
            {
                Debug.Log($"KickOffBounce{directionName} blocked - already in bounce state (continuous bounce disabled)");
            }
            return;
        }
        
        if (Camera.main == null)
        {
            Debug.LogWarning($"KickOffBounce{directionName}: No main camera found!");
            return;
        }

        Vector3 bounceDirection = cameraRelativeDirection(Camera.main.transform);
        Vector3 finalBounceForce = bounceDirection * bounceForce;

        
        // Apply bounce force based on type
        if (bounceType == BounceType.Impulse)
        {
            rb.AddForce(finalBounceForce, forceMode);
        }
        else if (bounceType == BounceType.GentleForce)
        {
            // For continuous bouncing, add to existing gentle force instead of replacing
            if (allowContinuousBounce && currentState == MovementState.Bouncing)
            {
                gentleBounceForce += finalBounceForce * 100f;
            }
            else
            {
                gentleBounceForce = finalBounceForce * 100f;
            }
        }
        
        // Enter bounce state (or refresh it if already bouncing)
        EnterBounceState(finalBounceForce);
        
        if (showDebug)
        {
            string stateMsg = currentState == MovementState.Bouncing && allowContinuousBounce ? " (continuous)" : "";
            Debug.Log($"KickOffBounce{directionName} triggered with velocity: {velocity.magnitude:F2}{stateMsg}");
        }
    }

    /// <summary>
    /// Bounces the character to the left relative to the main camera's view direction.
    /// Intended to be called by TransformMotionDetector's onEnterLeft event.
    /// </summary>
    public virtual void KickOffBounceLeft(Vector3 velocity)
    {
        KickOffBounceInDirection(cam => -cam.right, velocity, "Left", ForceMode.VelocityChange);
    }

    /// <summary>
    /// Bounces the character to the right relative to the main camera's view direction.
    /// Intended to be called by TransformMotionDetector's onEnterRight event.
    /// </summary>
    public virtual void KickOffBounceRight(Vector3 velocity)
    {
        KickOffBounceInDirection(cam => cam.right, velocity, "Right", ForceMode.VelocityChange);
    }

    /// <summary>
    /// Bounces the character backwards relative to the main camera's view direction.
    /// Intended to be called by TransformMotionDetector's onEnterForward event.
    /// </summary>
    public virtual void KickOffBounceBackward(Vector3 velocity)
    {
        KickOffBounceInDirection(cam => -cam.forward, velocity, "Backward", ForceMode.Impulse);
    }

    public bool IsGrounded() => isGrounded;
    public Vector3 GetVelocity() => rb != null ? rb.linearVelocity : Vector3.zero;
    public float GetSpeed() => rb != null ? rb.linearVelocity.magnitude : 0f;
    
    /// <summary>
    /// Gets the current movement scale factor based on bounce state.
    /// Returns 1.0 when not bouncing, or movementScaleDuringBounce when bouncing.
    /// </summary>
    /// <param name="isBreathMovement">Whether this is breath-induced movement</param>
    public virtual float GetMovementScaleFactor(bool isBreathMovement = false)
    {
        if (currentState == MovementState.Bouncing)
        {
            if (isBreathMovement && allowBreathMovementDuringBounce)
                return breathMovementScaleDuringBounce;
            else if (allowMovementDuringBounce)
                return movementScaleDuringBounce;
            else
                return 0f;
        }
        return 1f;
    }
    
    /// <summary>
    /// Gets the bounce intensity (0-1) based on current bounce state and timing.
    /// Useful for scaling other effects during bounce.
    /// </summary>
    public virtual float GetBounceIntensity()
    {
        if (currentState != MovementState.Bouncing)
            return 0f;
            
        float timeSinceBounce = Time.time - bounceStateStartTime;
        float bounceProgress = Mathf.Clamp01(timeSinceBounce / minimumBounceTime);
        
        // Start high and decay over time
        return Mathf.Lerp(1f, 0.2f, bounceProgress);
    }

    public virtual void KickOffBounceLeft(ScriptableObject so){
        // Use the current rigidbody velocity for the bounce left call
        Vector3 currentVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        KickOffBounceLeft(currentVelocity);
    }

    public virtual void KickOffBounceRight(ScriptableObject so){
        // Use the current rigidbody velocity for the bounce right call
        Vector3 currentVelocity = rb != null ? rb.linearVelocity : Vector3.zero;
        KickOffBounceRight(currentVelocity);
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb != null) rb.useGravity = enableGravity;
        
        if (moveForce < 0f) moveForce = 0f;
        if (maxSpeed < 0f) maxSpeed = 0f;
        if (bounceForce < 0f) bounceForce = 0f;
        if (exitBounceMinSpeed < 0f) exitBounceMinSpeed = 0f;
        if (minimumBounceTime < 0f) minimumBounceTime = 0f;
        movementScaleDuringBounce = Mathf.Clamp01(movementScaleDuringBounce);
    }
#endif
} 