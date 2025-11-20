using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Simple physics-based controller that provides bounce functionality.
/// All movement is handled through Rigidbody physics for natural force combination.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SimplePhysicsController : MonoBehaviour
{
    [Header("Physics")]
    [Tooltip("The Rigidbody component to control.")]
    [SerializeField] protected Rigidbody rb;
    
    [Tooltip("Maximum velocity magnitude. Set to 0 for no limit.")]
    [SerializeField] protected float maxSpeed = 20f;
    
    [Tooltip("Whether to apply velocity clamping.")]
    [SerializeField] protected bool clampVelocity = true;

    [Header("Bounce Settings")]
    [Tooltip("Default bounce force magnitude.")]
    [SerializeField] protected float bounceForce = 10f;
    
    [Tooltip("Apply angular velocity on bounce for spinning effect.")]
    [SerializeField] protected bool applyAngularVelocityOnBounce = false;
    
    [Tooltip("Angular velocity magnitude to apply on bounce.")]
    [SerializeField] protected float bounceAngularVelocity = 5f;

    [Header("Bounce Events")]
    [Tooltip("Called when a bounce is triggered.")]
    public UnityEvent<Vector3> OnBounce = new UnityEvent<Vector3>();

    [Header("Debug")]
    [SerializeField] protected bool showDebug = false;

    // Public properties
    public Rigidbody Rigidbody => rb;
    public float BounceForce { get => bounceForce; set => bounceForce = Mathf.Max(0f, value); }
    public float MaxSpeed { get => maxSpeed; set => maxSpeed = Mathf.Max(0f, value); }
    public bool ClampVelocity { get => clampVelocity; set => clampVelocity = value; }

    protected virtual void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        // Ensure the Rigidbody doesn't rotate from physics
        rb.freezeRotation = true;
    }

    protected virtual void FixedUpdate()
    {
        // Clamp velocity if enabled
        if (clampVelocity && maxSpeed > 0f)
        {
            ClampVelocityToMaxSpeed();
        }

        if (showDebug)
        {
            LogDebugInfo();
        }
    }

    protected virtual void ClampVelocityToMaxSpeed()
    {
        if (rb.linearVelocity.magnitude > maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }

    /// <summary>
    /// Apply a bounce force in the specified direction.
    /// </summary>
    /// <param name="direction">Direction to bounce (will be normalized).</param>
    /// <param name="force">Force magnitude. If 0, uses default bounceForce.</param>
    public virtual void KickOffBounce(Vector3 direction, float force = 0f)
    {
        if (direction.magnitude < 0.01f)
        {
            if (showDebug)
                Debug.LogWarning("KickOffBounce: Direction vector is too small!");
            return;
        }

        float finalForce = force > 0f ? force : bounceForce;
        Vector3 bounceVector = direction.normalized * finalForce;
        
        // Apply the bounce as an impulse
        rb.AddForce(bounceVector, ForceMode.Impulse);
        
        // Apply angular velocity if enabled
        if (applyAngularVelocityOnBounce)
        {
            Vector3 randomAxis = Random.onUnitSphere;
            rb.angularVelocity = randomAxis * bounceAngularVelocity;
        }
        
        // Trigger bounce event
        OnBounce.Invoke(bounceVector);
        
        if (showDebug)
        {
            Debug.Log($"Bounce applied: Direction={direction.normalized}, Force={finalForce:F2}");
        }
    }

    /// <summary>
    /// Bounce in a random direction.
    /// </summary>
    /// <param name="force">Force magnitude. If 0, uses default bounceForce.</param>
    public virtual void BounceInRandomDirection(float force = 0f)
    {
        Vector3 randomDirection = Random.onUnitSphere;
        KickOffBounce(randomDirection, force);
    }

    /// <summary>
    /// Bounce in a random horizontal direction (no vertical component).
    /// </summary>
    /// <param name="force">Force magnitude. If 0, uses default bounceForce.</param>
    public virtual void BounceInRandomHorizontalDirection(float force = 0f)
    {
        Vector2 random2D = Random.insideUnitCircle.normalized;
        Vector3 randomDirection = new Vector3(random2D.x, 0f, random2D.y);
        KickOffBounce(randomDirection, force);
    }

    #region Camera-Relative Bounce Methods

    /// <summary>
    /// Bounce backward relative to the main camera's view direction.
    /// </summary>
    public virtual void KickOffBounceBackward(Vector3 velocity)
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("KickOffBounceBackward: No main camera found!");
            return;
        }
        
        Vector3 direction = -Camera.main.transform.forward;
        direction.y = 0f; // Keep it horizontal
        KickOffBounce(direction);
    }

    /// <summary>
    /// Bounce left relative to the main camera's view direction.
    /// </summary>
    public virtual void KickOffBounceLeft(Vector3 velocity)
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("KickOffBounceLeft: No main camera found!");
            return;
        }
        
        Vector3 direction = -Camera.main.transform.right;
        direction.y = 0f; // Keep it horizontal
        KickOffBounce(direction);
    }

    /// <summary>
    /// Bounce right relative to the main camera's view direction.
    /// </summary>
    public virtual void KickOffBounceRight(Vector3 velocity)
    {
        if (Camera.main == null)
        {
            Debug.LogWarning("KickOffBounceRight: No main camera found!");
            return;
        }
        
        Vector3 direction = Camera.main.transform.right;
        direction.y = 0f; // Keep it horizontal
        KickOffBounce(direction);
    }

    /// <summary>
    /// Bounce up in world space.
    /// </summary>
    public virtual void KickOffBounceUp()
    {
        KickOffBounce(Vector3.up);
    }

    /// <summary>
    /// Bounce down in world space.
    /// </summary>
    public virtual void KickOffBounceDown()
    {
        KickOffBounce(Vector3.down);
    }

    #endregion

    #region ScriptableObject Overloads for UnityEvents

    public virtual void KickOffBounceBackward(ScriptableObject so)
    {
        KickOffBounceBackward(Vector3.zero);
    }

    public virtual void KickOffBounceLeft(ScriptableObject so)
    {
        KickOffBounceLeft(Vector3.zero);
    }

    public virtual void KickOffBounceRight(ScriptableObject so)
    {
        KickOffBounceRight(Vector3.zero);
    }

    #endregion

    /// <summary>
    /// Reset the controller's velocity.
    /// </summary>
    public virtual void ResetVelocity()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Set the position and reset velocity.
    /// </summary>
    public virtual void SetPosition(Vector3 position)
    {
        if (rb != null)
        {
            rb.position = position;
            ResetVelocity();
        }
    }

    // Public getters
    public Vector3 GetVelocity() => rb != null ? rb.linearVelocity : Vector3.zero;
    public float GetSpeed() => rb != null ? rb.linearVelocity.magnitude : 0f;
    public Vector3 GetAngularVelocity() => rb != null ? rb.angularVelocity : Vector3.zero;

    protected virtual void LogDebugInfo()
    {
        Debug.Log($"SimplePhysicsController - Pos: {transform.position:F2}, Vel: {GetSpeed():F2}, AngVel: {GetAngularVelocity().magnitude:F2}");
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        bounceForce = Mathf.Max(0f, bounceForce);
        maxSpeed = Mathf.Max(0f, maxSpeed);
        bounceAngularVelocity = Mathf.Max(0f, bounceAngularVelocity);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (!showDebug || rb == null) return;
        
        // Draw velocity vector
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, rb.linearVelocity * 0.1f);
        
        // Draw angular velocity
        if (rb.angularVelocity.magnitude > 0.01f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(transform.position, rb.angularVelocity.normalized * 0.5f);
        }
    }
#endif
}