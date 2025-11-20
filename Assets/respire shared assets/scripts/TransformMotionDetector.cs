using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

[System.Serializable]
public class DirectionEvent : UnityEvent<Vector3> { }

[System.Serializable]
public class MotionEvents
{
    [Header("Enter Events")]
    public DirectionEvent onEnterUp = new DirectionEvent();
    public DirectionEvent onEnterDown = new DirectionEvent();
    public DirectionEvent onEnterLeft = new DirectionEvent();
    public DirectionEvent onEnterRight = new DirectionEvent();
    public DirectionEvent onEnterForward = new DirectionEvent();
    public DirectionEvent onEnterBackward = new DirectionEvent();

    [Header("Leave Events")]
    public DirectionEvent onLeaveUp = new DirectionEvent();
    public DirectionEvent onLeaveDown = new DirectionEvent();
    public DirectionEvent onLeaveLeft = new DirectionEvent();
    public DirectionEvent onLeaveRight = new DirectionEvent();
    public DirectionEvent onLeaveForward = new DirectionEvent();
    public DirectionEvent onLeaveBackward = new DirectionEvent();
}

public enum MotionDirection
{
    None,
    Up,
    Down,
    Left, 
    Right,
    Forward,
    Backward
}

public class TransformMotionDetector : MonoBehaviour
{
    [Header("Tracking Settings")]
    [SerializeField] private Transform targetTransform;
    [SerializeField] private bool useWorldSpace = true;
    [SerializeField] private Transform referenceTransform; // For local space calculations

    [Header("Motion Detection")]
    [SerializeField] private float motionThreshold = 0.01f; // Minimum movement to detect
    [SerializeField] private float directionThreshold = 0.7f; // Dot product threshold for direction
    [SerializeField] private int smoothingFrames = 5; // Number of frames to smooth velocity over
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    [SerializeField] private bool drawDebugRays = false;

    [Header("Events")]
    public MotionEvents motionEvents = new MotionEvents();

    // Private variables
    private Vector3 lastPosition;
    private Vector3 currentVelocity;
    
    // Current active directions (simplified)
    private HashSet<MotionDirection> activeDirections = new HashSet<MotionDirection>();
    
    private Queue<Vector3> velocityHistory = new Queue<Vector3>();
    private bool isInitialized = false;

    // Reference directions for dot product calculations
    private readonly Vector3[] referenceDirections = new Vector3[6];

    private void Start()
    {
        // Get initial position
        Vector3 initialPos = GetTrackedPosition();
        if (initialPos != Vector3.zero)
        {
            lastPosition = initialPos;
            isInitialized = true;
        }
        
        if (enableDebugLogs)
        {
            if (targetTransform != null)
            {
                Debug.Log($"TransformMotionDetector: Initialized tracking for {targetTransform.name}");
            }
            else
                Debug.LogWarning("TransformMotionDetector: No target transform assigned!");
        }
    }

    private void Update()
    {
        if (targetTransform == null)
            return;

        Vector3 currentPosition = GetTrackedPosition();

        // Calculate velocity based on actual target transform movement
        Vector3 deltaPosition = currentPosition - lastPosition;
        currentVelocity = deltaPosition / Time.deltaTime;

        // Smooth velocity over multiple frames
        SmoothVelocity();

        // Detect directions using simplified approach
        Vector3 smoothedVelocity = GetSmoothedVelocity();
        DetectDirections(smoothedVelocity);

        // Store current position for next frame
        lastPosition = currentPosition;

        // Debug visualization
        if (drawDebugRays)
        {
            DrawDebugRays(currentPosition);
        }
    }

    private Vector3 GetTrackedPosition()
    {
        if (targetTransform == null) return Vector3.zero;
        
        if (useWorldSpace || referenceTransform == null)
        {
            // World space - use target's world position
            return targetTransform.position;
        }
        else
        {
            // Local space - calculate position relative to reference transform
            // This gives us the target's position in the reference transform's local coordinate system
            return referenceTransform.InverseTransformPoint(targetTransform.position);
        }
    }

    private void SmoothVelocity()
    {
        velocityHistory.Enqueue(currentVelocity);
        
        if (velocityHistory.Count > smoothingFrames)
        {
            velocityHistory.Dequeue();
        }
    }

    private Vector3 GetSmoothedVelocity()
    {
        if (velocityHistory.Count == 0) return Vector3.zero;
        
        Vector3 sum = Vector3.zero;
        foreach (Vector3 vel in velocityHistory)
        {
            sum += vel;
        }
        
        return sum / velocityHistory.Count;
    }

    private void DetectDirections(Vector3 velocity)
    {
        if (velocity.magnitude < motionThreshold)
        {
            // No motion - clear all directions
            ClearAllDirections();
            return;
        }

        // Update reference directions
        UpdateReferenceDirections();
        
        // Check each direction using dot products
        HashSet<MotionDirection> newActiveDirections = new HashSet<MotionDirection>();
        
        for (int i = 0; i < 6; i++)
        {
            MotionDirection direction = (MotionDirection)(i + 1); // Skip None (0)
            float dot = Vector3.Dot(velocity.normalized, referenceDirections[i]);
            
            if (dot >= directionThreshold)
            {
                newActiveDirections.Add(direction);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Direction {direction}: dot={dot:F3}, threshold={directionThreshold:F3}");
                }
            }
        }

        // Update active directions and trigger events
        UpdateActiveDirections(newActiveDirections);
    }

    private void UpdateReferenceDirections()
    {
        if (useWorldSpace || referenceTransform == null)
        {
            // World space directions
            referenceDirections[0] = Vector3.up;        // Up
            referenceDirections[1] = Vector3.down;      // Down
            referenceDirections[2] = Vector3.left;      // Left
            referenceDirections[3] = Vector3.right;     // Right
            referenceDirections[4] = Vector3.forward;   // Forward
            referenceDirections[5] = Vector3.back;      // Backward
        }
        else
        {
            // Local space directions relative to reference transform
            referenceDirections[0] = referenceTransform.up;           // Up
            referenceDirections[1] = -referenceTransform.up;          // Down
            referenceDirections[2] = -referenceTransform.right;       // Left
            referenceDirections[3] = referenceTransform.right;        // Right
            referenceDirections[4] = referenceTransform.forward;      // Forward
            referenceDirections[5] = -referenceTransform.forward;     // Backward
        }
    }

    private void UpdateActiveDirections(HashSet<MotionDirection> newActiveDirections)
    {
        // Find directions that were active but are no longer
        foreach (MotionDirection direction in activeDirections)
        {
            if (!newActiveDirections.Contains(direction))
            {
                TriggerLeaveEvent(direction);
                
                if (enableDebugLogs)
                    Debug.Log($"TransformMotionDetector: Left direction {direction}");
            }
        }

        // Find directions that are newly active
        foreach (MotionDirection direction in newActiveDirections)
        {
            if (!activeDirections.Contains(direction))
            {
                TriggerEnterEvent(direction);
                
                if (enableDebugLogs)
                    Debug.Log($"TransformMotionDetector: Entered direction {direction}");
            }
        }

        // Update active directions
        activeDirections = new HashSet<MotionDirection>(newActiveDirections);
    }

    private void ClearAllDirections()
    {
        foreach (MotionDirection direction in activeDirections)
        {
            TriggerLeaveEvent(direction);
            
            if (enableDebugLogs)
                Debug.Log($"TransformMotionDetector: Left direction {direction}");
        }
        
        activeDirections.Clear();
    }

    #region Event Triggers

    private void TriggerEnterEvent(MotionDirection direction)
    {
        Vector3 velocity = GetSmoothedVelocity();
        
        switch (direction)
        {
            case MotionDirection.Up:
                motionEvents.onEnterUp.Invoke(velocity);
                break;
            case MotionDirection.Down:
                motionEvents.onEnterDown.Invoke(velocity);
                break;
            case MotionDirection.Left:
                motionEvents.onEnterLeft.Invoke(velocity);
                break;
            case MotionDirection.Right:
                motionEvents.onEnterRight.Invoke(velocity);
                break;
            case MotionDirection.Forward:
                motionEvents.onEnterForward.Invoke(velocity);
                break;
            case MotionDirection.Backward:
                motionEvents.onEnterBackward.Invoke(velocity);
                break;
        }
    }

    private void TriggerLeaveEvent(MotionDirection direction)
    {
        Vector3 velocity = GetSmoothedVelocity();
        
        switch (direction)
        {
            case MotionDirection.Up:
                motionEvents.onLeaveUp.Invoke(velocity);
                break;
            case MotionDirection.Down:
                motionEvents.onLeaveDown.Invoke(velocity);
                break;
            case MotionDirection.Left:
                motionEvents.onLeaveLeft.Invoke(velocity);
                break;
            case MotionDirection.Right:
                motionEvents.onLeaveRight.Invoke(velocity);
                break;
            case MotionDirection.Forward:
                motionEvents.onLeaveForward.Invoke(velocity);
                break;
            case MotionDirection.Backward:
                motionEvents.onLeaveBackward.Invoke(velocity);
                break;
        }
    }

    #endregion

    #region Debug & Visualization

    private void DrawDebugRays(Vector3 position)
    {
        Vector3 velocity = GetSmoothedVelocity();
        
        if (velocity.magnitude > motionThreshold)
        {
            // Draw current velocity
            Debug.DrawRay(position, velocity * 0.5f, Color.white, Time.deltaTime);
        }

        // Draw active directions
        DrawActiveDirections(position);

        // Draw reference directions
        UpdateReferenceDirections();
        Color[] colors = { Color.green, Color.red, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        
        for (int i = 0; i < referenceDirections.Length; i++)
        {
            Debug.DrawRay(position, referenceDirections[i] * 0.1f, colors[i], Time.deltaTime);
        }
    }

    private void DrawActiveDirections(Vector3 position)
    {
        foreach (MotionDirection direction in activeDirections)
        {
            Vector3 rayDirection = Vector3.zero;
            Color color = Color.white;
            
            switch (direction)
            {
                case MotionDirection.Up:
                    rayDirection = Vector3.up;
                    color = Color.green;
                    break;
                case MotionDirection.Down:
                    rayDirection = Vector3.down;
                    color = Color.red;
                    break;
                case MotionDirection.Left:
                    rayDirection = Vector3.left;
                    color = Color.blue;
                    break;
                case MotionDirection.Right:
                    rayDirection = Vector3.right;
                    color = Color.yellow;
                    break;
                case MotionDirection.Forward:
                    rayDirection = Vector3.forward;
                    color = Color.cyan;
                    break;
                case MotionDirection.Backward:
                    rayDirection = Vector3.back;
                    color = Color.magenta;
                    break;
            }
            
            Debug.DrawRay(position, rayDirection * 0.3f, color, Time.deltaTime);
        }
    }

    #endregion

    #region Public API

    // Get all currently active directions
    public List<MotionDirection> GetActiveDirections()
    {
        return new List<MotionDirection>(activeDirections);
    }

    // Check if a specific direction is active
    public bool IsDirectionActive(MotionDirection direction)
    {
        return activeDirections.Contains(direction);
    }

    public Vector3 GetCurrentVelocity() => GetSmoothedVelocity();
    public Vector3 GetCurrentPosition() => GetTrackedPosition();
    public bool IsMoving() => GetSmoothedVelocity().magnitude > motionThreshold;
    
    // Debug helper methods
    public Vector3 GetTargetWorldPosition() => targetTransform != null ? targetTransform.position : Vector3.zero;
    public Vector3 GetReferenceWorldPosition() => referenceTransform != null ? referenceTransform.position : Vector3.zero;

    #endregion

    private void OnValidate()
    {
        motionThreshold = Mathf.Max(0f, motionThreshold);
        directionThreshold = Mathf.Clamp01(directionThreshold);
        smoothingFrames = Mathf.Max(1, smoothingFrames);
    }
} 