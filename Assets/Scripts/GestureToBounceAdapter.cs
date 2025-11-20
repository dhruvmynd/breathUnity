using UnityEngine;
using UnityEngine.Events;

public class GestureToBounceAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SimplePhysicsController physicsController;
    [SerializeField] private Transform handBoneTransform;
    
    [Header("Velocity Calculation")]
    [SerializeField] private float velocityMultiplier = 1f;
    [SerializeField] private float forceMultiplier = 1f;
    [SerializeField] private float minimumForce = 0f;
    [SerializeField] private float maximumForce = 50f;
    [SerializeField] private float maximumVelocity = 20f;
    [SerializeField] private bool useFixedForce = false;
    [SerializeField] private float fixedForceValue = 5f;
    
    [Header("Direction Options")]
    [SerializeField] private VectorMode vectorMode = VectorMode.Raw;
    [SerializeField] private ProjectionAxis projectionAxis = ProjectionAxis.All;
    [SerializeField] private bool normalizeDirection = false;
    
    [Header("Position Tracking")]
    [SerializeField] private PositionSpace positionSpace = PositionSpace.World;
    
    public enum PositionSpace
    {
        World,
        Local,
        Camera
    }
    
    [Header("Debug")]
    [SerializeField] private bool debugLog = false;
    [SerializeField] private bool drawDebugGizmos = false;
    [SerializeField] private float gizmosDuration = 2f;
    
    public enum VectorMode
    {
        Raw,
        WorldAxisProjection,
        CameraAxisProjection
    }
    
    public enum ProjectionAxis
    {
        X,
        Y,
        Z,
        All
    }
    
    private Vector3 startPosition;
    private float startTime;
    private bool isTracking = false;
    
    // Debug visualization
    private Vector3 lastStartPos;
    private Vector3 lastEndPos;
    private Vector3 lastDirection;
    private float debugDrawStartTime;
    
    // Continuous tracking for debugging
    private Vector3 previousFramePosition;
    private float gestureStartRealTime;
    
    public void OnGestureStarted()
    {
        if (handBoneTransform == null)
        {
            Debug.LogWarning("GestureToBounceAdapter: No hand bone transform assigned!");
            return;
        }
        
        startPosition = GetPositionInSpace(handBoneTransform);
        startTime = Time.time;
        gestureStartRealTime = Time.realtimeSinceStartup;
        previousFramePosition = startPosition;
        isTracking = true;
        
        if (debugLog)
        {
            Debug.Log($"GestureToBounceAdapter: Started tracking at {startPosition} ({positionSpace} space)");
            Debug.Log($"GestureToBounceAdapter: Hand bone: {handBoneTransform.name}");
            Debug.Log($"GestureToBounceAdapter: World position: {handBoneTransform.position}");
            Debug.Log($"GestureToBounceAdapter: Local position: {handBoneTransform.localPosition}");
            if (Camera.main != null)
            {
                Vector3 cameraSpace = Camera.main.transform.InverseTransformPoint(handBoneTransform.position);
                Debug.Log($"GestureToBounceAdapter: Camera space position: {cameraSpace}");
            }
            Debug.Log($"GestureToBounceAdapter: Parent: {(handBoneTransform.parent != null ? handBoneTransform.parent.name : "None")}");
        }
    }
    
    public void OnGestureEnded()
    {
        if (!isTracking || handBoneTransform == null || physicsController == null)
        {
            if (physicsController == null)
                Debug.LogWarning("GestureToBounceAdapter: No physics controller assigned!");
            return;
        }
        
        Vector3 endPosition = GetPositionInSpace(handBoneTransform);
        float endTime = Time.time;
        float deltaTime = endTime - startTime;
        float realTimeDelta = Time.realtimeSinceStartup - gestureStartRealTime;
        
        if (debugLog)
        {
            Debug.Log($"GestureToBounceAdapter: Ended tracking at {endPosition} ({positionSpace} space)");
            Debug.Log($"GestureToBounceAdapter: World position: {handBoneTransform.position}");
            Debug.Log($"GestureToBounceAdapter: Local position: {handBoneTransform.localPosition}");
            if (Camera.main != null)
            {
                Vector3 cameraSpace = Camera.main.transform.InverseTransformPoint(handBoneTransform.position);
                Debug.Log($"GestureToBounceAdapter: Camera space position: {cameraSpace}");
            }
        }
        
        if (deltaTime <= 0.001f)
        {
            if (debugLog)
                Debug.Log("GestureToBounceAdapter: Gesture too short, no bounce");
            isTracking = false;
            return;
        }
        
        // Calculate displacement
        Vector3 displacement = endPosition - startPosition;
        
        // Calculate raw velocity
        Vector3 velocity = displacement / deltaTime;
        velocity *= velocityMultiplier;
        
        // Clamp velocity magnitude
        if (velocity.magnitude > maximumVelocity)
        {
            velocity = velocity.normalized * maximumVelocity;
        }
        
        if (debugLog)
        {
            Debug.Log($"GestureToBounceAdapter: Start->End: {startPosition} -> {endPosition}");
            Debug.Log($"GestureToBounceAdapter: Displacement = {displacement}, X={displacement.x:F3}, Y={displacement.y:F3}, Z={displacement.z:F3}");
            Debug.Log($"GestureToBounceAdapter: Time.deltaTime = {deltaTime:F3}, realTimeDelta = {realTimeDelta:F3}");
        }
        
        // Process direction based on mode
        Vector3 direction = ProcessDirection(velocity);
        
        if (normalizeDirection && direction.magnitude > 0.001f)
        {
            direction = direction.normalized;
        }
        
        // Calculate force magnitude
        float force;
        if (useFixedForce)
        {
            force = Mathf.Clamp(fixedForceValue, minimumForce, maximumForce);
        }
        else
        {
            force = velocity.magnitude * forceMultiplier;
            force = Mathf.Clamp(force, minimumForce, maximumForce);
        }
        
        if (debugLog)
        {
            Debug.Log($"GestureToBounceAdapter: Start: {startPosition}, End: {endPosition}");
            Debug.Log($"GestureToBounceAdapter: Raw Velocity: {velocity}, Processed Direction: {direction}, Force: {force}");
            Debug.Log($"GestureToBounceAdapter: Delta Time: {deltaTime:F3}s, Distance: {(endPosition - startPosition).magnitude:F3}");
        }
        
        // Store for debug visualization
        if (drawDebugGizmos)
        {
            lastStartPos = startPosition;
            lastEndPos = endPosition;
            lastDirection = direction.normalized;
            debugDrawStartTime = Time.time;
        }
        
        // Trigger the bounce
        physicsController.KickOffBounce(direction, force);
        
        isTracking = false;
    }
    
    private Vector3 ProcessDirection(Vector3 velocity)
    {
        switch (vectorMode)
        {
            case VectorMode.Raw:
                return velocity;
                
            case VectorMode.WorldAxisProjection:
                return ProjectToAxis(velocity, Vector3.right, Vector3.up, Vector3.forward);
                
            case VectorMode.CameraAxisProjection:
                if (Camera.main != null)
                {
                    Transform camTransform = Camera.main.transform;
                    return ProjectToAxis(velocity, camTransform.right, camTransform.up, camTransform.forward);
                }
                else
                {
                    Debug.LogWarning("GestureToBounceAdapter: No main camera found for camera axis projection");
                    return velocity;
                }
                
            default:
                return velocity;
        }
    }
    
    private Vector3 ProjectToAxis(Vector3 velocity, Vector3 xAxis, Vector3 yAxis, Vector3 zAxis)
    {
        switch (projectionAxis)
        {
            case ProjectionAxis.X:
                return xAxis * Vector3.Dot(velocity, xAxis);
                
            case ProjectionAxis.Y:
                return yAxis * Vector3.Dot(velocity, yAxis);
                
            case ProjectionAxis.Z:
                return zAxis * Vector3.Dot(velocity, zAxis);
                
            case ProjectionAxis.All:
                return velocity;
                
            default:
                return velocity;
        }
    }
    
    private Vector3 GetPositionInSpace(Transform transform)
    {
        switch (positionSpace)
        {
            case PositionSpace.World:
                return transform.position;
                
            case PositionSpace.Local:
                return transform.localPosition;
                
            case PositionSpace.Camera:
                if (Camera.main != null)
                {
                    // Convert world position to camera space
                    return Camera.main.transform.InverseTransformPoint(transform.position);
                }
                else
                {
                    Debug.LogWarning("GestureToBounceAdapter: No main camera found, using world position");
                    return transform.position;
                }
                
            default:
                return transform.position;
        }
    }
    
    private void OnValidate()
    {
        velocityMultiplier = Mathf.Max(0f, velocityMultiplier);
        forceMultiplier = Mathf.Max(0f, forceMultiplier);
        minimumForce = Mathf.Max(0f, minimumForce);
        maximumForce = Mathf.Max(minimumForce, maximumForce);
        maximumVelocity = Mathf.Max(0.1f, maximumVelocity);
        fixedForceValue = Mathf.Max(0f, fixedForceValue);
    }
    
    private void Update()
    {
        if (isTracking && handBoneTransform != null && debugLog)
        {
            Vector3 currentPos = GetPositionInSpace(handBoneTransform);
            Vector3 frameVelocity = (currentPos - previousFramePosition) / Time.deltaTime;
            
            if (frameVelocity.magnitude > 0.01f) // Only log if there's meaningful movement
            {
                Debug.Log($"GestureToBounceAdapter [Frame]: Pos={currentPos}, FrameVel={frameVelocity}, X={frameVelocity.x:F3} ({positionSpace})");
            }
            
            previousFramePosition = currentPos;
        }
    }
    
    private void OnDrawGizmos()
    {
        if (!drawDebugGizmos || Time.time - debugDrawStartTime > gizmosDuration)
            return;
            
        // Draw the path from start to end
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(lastStartPos, 0.02f);
        Gizmos.DrawLine(lastStartPos, lastEndPos);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(lastEndPos, 0.02f);
        
        // Draw the final bounce direction
        Gizmos.color = Color.red;
        Gizmos.DrawRay(lastEndPos, lastDirection * 0.5f);
        
        // Draw coordinate axes at end position for reference
        if (vectorMode == VectorMode.CameraAxisProjection && Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            Gizmos.color = Color.red;
            Gizmos.DrawRay(lastEndPos, cam.right * 0.2f);
            Gizmos.color = Color.green;
            Gizmos.DrawRay(lastEndPos, cam.up * 0.2f);
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(lastEndPos, cam.forward * 0.2f);
        }
    }
}