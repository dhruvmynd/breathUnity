using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

namespace SFUBreathing.Locomotion
{
    /// <summary>
    /// Detects hand movement direction while a static gesture is active and fires Unity events.
    /// Useful for triggering movement or actions based on hand swipes while holding a gesture.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Hand Gesture Movement Detector", 13)]
    public class HandGestureMovementDetector : MonoBehaviour
    {
        [Header("Hand Transforms")]
        [SerializeField]
        [Tooltip("Transform that represents the left hand position (typically from XR Hand prefab).")]
        Transform m_LeftHandTransform;
        
        [SerializeField]
        [Tooltip("Transform that represents the right hand position (typically from XR Hand prefab).")]
        Transform m_RightHandTransform;
        
        [Header("Movement Detection")]
        [SerializeField]
        [Tooltip("Minimum movement distance to register as a swipe.")]
        float m_MovementThreshold = 0.1f;
        
        [SerializeField]
        [Tooltip("Minimum velocity (meters/second) to trigger swipe events.")]
        float m_VelocityThreshold = 0.5f;
        
        [SerializeField]
        [Tooltip("How often to check for movement (seconds).")]
        float m_UpdateInterval = 0.1f;
        
        [SerializeField]
        [Tooltip("Smooth velocity over multiple frames to reduce noise.")]
        bool m_SmoothVelocity = true;
        
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Smoothing factor for velocity calculation (0 = no smoothing, 1 = max smoothing).")]
        float m_VelocitySmoothingFactor = 0.3f;
        
        [Header("Movement Events")]
        [Tooltip("Fired when hand swipes left (relative to camera).")]
        public UnityEvent<Vector3> onSwipeLeft = new UnityEvent<Vector3>();
        
        [Tooltip("Fired when hand swipes right (relative to camera).")]
        public UnityEvent<Vector3> onSwipeRight = new UnityEvent<Vector3>();
        
        [Tooltip("Fired when hand swipes forward (relative to camera).")]
        public UnityEvent<Vector3> onSwipeForward = new UnityEvent<Vector3>();
        
        [Tooltip("Fired when hand swipes backward (relative to camera).")]
        public UnityEvent<Vector3> onSwipeBackward = new UnityEvent<Vector3>();
        
        [Tooltip("Fired when hand swipes up.")]
        public UnityEvent<Vector3> onSwipeUp = new UnityEvent<Vector3>();
        
        [Tooltip("Fired when hand swipes down.")]
        public UnityEvent<Vector3> onSwipeDown = new UnityEvent<Vector3>();
        
        [Tooltip("Fired for any horizontal swipe (left or right). Useful for KickOffBounceHorizontal.")]
        public UnityEvent<Vector3> onHorizontalSwipe = new UnityEvent<Vector3>();
        
        [Header("Debug")]
        [SerializeField]
        [Tooltip("Enable debug logging to see movement detection in console.")]
        bool m_EnableDebugLogging = false;
        
        // Gesture state tracking
        bool m_LeftGestureActive;
        bool m_RightGestureActive;
        Vector3 m_LastLeftHandPosition;
        Vector3 m_LastRightHandPosition;
        float m_LastLeftUpdateTime;
        float m_LastRightUpdateTime;
        
        // Velocity smoothing
        Vector3 m_SmoothedLeftVelocity;
        Vector3 m_SmoothedRightVelocity;
        
        /// <summary>
        /// Left hand transform.
        /// </summary>
        public Transform leftHandTransform
        {
            get => m_LeftHandTransform;
            set => m_LeftHandTransform = value;
        }
        
        /// <summary>
        /// Right hand transform.
        /// </summary>
        public Transform rightHandTransform
        {
            get => m_RightHandTransform;
            set => m_RightHandTransform = value;
        }
        
        void Start()
        {
            // Initialize positions
            if (m_LeftHandTransform != null)
                m_LastLeftHandPosition = m_LeftHandTransform.position;
            if (m_RightHandTransform != null)
                m_LastRightHandPosition = m_RightHandTransform.position;
                
            m_LastLeftUpdateTime = Time.time;
            m_LastRightUpdateTime = Time.time;
        }
        
        void Update()
        {
            // Check left hand movement
            if (m_LeftGestureActive && m_LeftHandTransform != null)
            {
                CheckHandMovement(m_LeftHandTransform, ref m_LastLeftHandPosition, 
                                 ref m_LastLeftUpdateTime, ref m_SmoothedLeftVelocity, "Left");
            }
            
            // Check right hand movement
            if (m_RightGestureActive && m_RightHandTransform != null)
            {
                CheckHandMovement(m_RightHandTransform, ref m_LastRightHandPosition, 
                                 ref m_LastRightUpdateTime, ref m_SmoothedRightVelocity, "Right");
            }
        }
        
        void CheckHandMovement(Transform handTransform, ref Vector3 lastPosition, 
                              ref float lastUpdateTime, ref Vector3 smoothedVelocity, string handName)
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - lastUpdateTime;
            
            // Only check at update interval
            if (deltaTime < m_UpdateInterval)
                return;
                
            Vector3 currentPosition = handTransform.position;
            Vector3 movement = currentPosition - lastPosition;
            
            // Check if movement is significant
            if (movement.magnitude < m_MovementThreshold)
            {
                lastUpdateTime = currentTime;
                return;
            }
            
            // Calculate velocity
            Vector3 velocity = movement / deltaTime;
            
            // Apply smoothing if enabled
            if (m_SmoothVelocity)
            {
                smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, 1f - m_VelocitySmoothingFactor);
                velocity = smoothedVelocity;
            }
            
            // Check if velocity meets threshold
            if (velocity.magnitude >= m_VelocityThreshold)
            {
                DetectSwipeDirection(velocity, handName);
            }
            
            // Update tracking
            lastPosition = currentPosition;
            lastUpdateTime = currentTime;
        }
        
        void DetectSwipeDirection(Vector3 velocity, string handName)
        {
            if (Camera.main == null)
            {
                Debug.LogWarning("HandGestureMovementDetector: No main camera found for direction calculation!");
                return;
            }
            
            // Get camera-relative directions
            Vector3 cameraForward = Camera.main.transform.forward;
            Vector3 cameraRight = Camera.main.transform.right;
            cameraForward.y = 0; // Project to horizontal plane
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            // Calculate directional components
            float forwardComponent = Vector3.Dot(velocity, cameraForward);
            float rightComponent = Vector3.Dot(velocity, cameraRight);
            float upComponent = velocity.y;
            
            // Determine primary direction
            float absForward = Mathf.Abs(forwardComponent);
            float absRight = Mathf.Abs(rightComponent);
            float absUp = Mathf.Abs(upComponent);
            
            if (m_EnableDebugLogging)
            {
                Debug.Log($"[HandGestureMovement] {handName} hand - Velocity: {velocity.magnitude:F2} m/s, " +
                         $"Forward: {forwardComponent:F2}, Right: {rightComponent:F2}, Up: {upComponent:F2}");
            }
            
            // Fire events based on primary direction
            if (absRight > absForward && absRight > absUp)
            {
                // Horizontal movement is dominant
                if (rightComponent > 0)
                {
                    onSwipeRight.Invoke(velocity);
                    if (m_EnableDebugLogging) Debug.Log($"[HandGestureMovement] {handName} hand - Swipe RIGHT");
                }
                else
                {
                    onSwipeLeft.Invoke(velocity);
                    if (m_EnableDebugLogging) Debug.Log($"[HandGestureMovement] {handName} hand - Swipe LEFT");
                }
                
                // Also fire horizontal swipe event
                onHorizontalSwipe.Invoke(velocity);
            }
            else if (absForward > absRight && absForward > absUp)
            {
                // Forward/backward movement is dominant
                if (forwardComponent > 0)
                {
                    onSwipeForward.Invoke(velocity);
                    if (m_EnableDebugLogging) Debug.Log($"[HandGestureMovement] {handName} hand - Swipe FORWARD");
                }
                else
                {
                    onSwipeBackward.Invoke(velocity);
                    if (m_EnableDebugLogging) Debug.Log($"[HandGestureMovement] {handName} hand - Swipe BACKWARD");
                }
            }
            else if (absUp > absForward && absUp > absRight)
            {
                // Vertical movement is dominant
                if (upComponent > 0)
                {
                    onSwipeUp.Invoke(velocity);
                    if (m_EnableDebugLogging) Debug.Log($"[HandGestureMovement] {handName} hand - Swipe UP");
                }
                else
                {
                    onSwipeDown.Invoke(velocity);
                    if (m_EnableDebugLogging) Debug.Log($"[HandGestureMovement] {handName} hand - Swipe DOWN");
                }
            }
        }
        
        /// <summary>
        /// Call this when the left hand gesture is performed.
        /// Connect this to StaticHandGesture.gesturePerformed event.
        /// </summary>
        public void OnLeftGestureStarted()
        {
            m_LeftGestureActive = true;
            if (m_LeftHandTransform != null)
            {
                m_LastLeftHandPosition = m_LeftHandTransform.position;
                m_SmoothedLeftVelocity = Vector3.zero;
            }
            m_LastLeftUpdateTime = Time.time;
            
            if (m_EnableDebugLogging)
                Debug.Log("[HandGestureMovement] Left gesture started - movement detection active");
        }
        
        /// <summary>
        /// Call this when the left hand gesture ends.
        /// Connect this to StaticHandGesture.gestureEnded event.
        /// </summary>
        public void OnLeftGestureEnded()
        {
            m_LeftGestureActive = false;
            m_SmoothedLeftVelocity = Vector3.zero;
            
            if (m_EnableDebugLogging)
                Debug.Log("[HandGestureMovement] Left gesture ended - movement detection stopped");
        }
        
        /// <summary>
        /// Call this when the right hand gesture is performed.
        /// Connect this to StaticHandGesture.gesturePerformed event.
        /// </summary>
        public void OnRightGestureStarted()
        {
            m_RightGestureActive = true;
            if (m_RightHandTransform != null)
            {
                m_LastRightHandPosition = m_RightHandTransform.position;
                m_SmoothedRightVelocity = Vector3.zero;
            }
            m_LastRightUpdateTime = Time.time;
            
            if (m_EnableDebugLogging)
                Debug.Log("[HandGestureMovement] Right gesture started - movement detection active");
        }
        
        /// <summary>
        /// Call this when the right hand gesture ends.
        /// Connect this to StaticHandGesture.gestureEnded event.
        /// </summary>
        public void OnRightGestureEnded()
        {
            m_RightGestureActive = false;
            m_SmoothedRightVelocity = Vector3.zero;
            
            if (m_EnableDebugLogging)
                Debug.Log("[HandGestureMovement] Right gesture ended - movement detection stopped");
        }
        
#if UNITY_EDITOR
        void OnValidate()
        {
            m_MovementThreshold = Mathf.Max(0.001f, m_MovementThreshold);
            m_VelocityThreshold = Mathf.Max(0.01f, m_VelocityThreshold);
            m_UpdateInterval = Mathf.Max(0.01f, m_UpdateInterval);
            m_VelocitySmoothingFactor = Mathf.Clamp01(m_VelocitySmoothingFactor);
        }
#endif
    }
}