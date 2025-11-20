using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace SFUBreathing.VR
{
    /// <summary>
    /// Detection mode for controller push gestures.
    /// </summary>
    public enum PushDetectionMode
    {
        /// <summary>Quick gesture detection - detects fast forward movements</summary>
        QuickPush,
        /// <summary>Sustained push detection - detects continuous forward movement</summary>
        SustainedPush,
        /// <summary>Hybrid mode - detects both quick and sustained pushes</summary>
        Both
    }

    /// <summary>
    /// Coordinate space for push detection calculations.
    /// </summary>
    public enum PushDetectionSpace
    {
        /// <summary>Relative to head/camera forward direction</summary>
        Head,
        /// <summary>Relative to controller's own forward direction</summary>
        Controller,
        /// <summary>World space forward (Z-axis)</summary>
        World
    }

    /// <summary>
    /// Current state of push detection.
    /// </summary>
    public enum PushDetectionState
    {
        Idle,
        Detecting,
        QuickPushDetected,
        SustainedPushActive,
        Cooldown
    }

    /// <summary>
    /// Unity Events for push detection callbacks.
    /// </summary>
    [System.Serializable]
    public class PushDetectionEvents
    {
        [Header("Push Detection Events")]
        [Tooltip("Triggered when a quick push gesture is detected")]
        public UnityEvent<Vector3> OnQuickPush = new UnityEvent<Vector3>();
        
        [Tooltip("Triggered when a sustained push begins")]
        public UnityEvent<Vector3> OnPushStart = new UnityEvent<Vector3>();
        
        [Tooltip("Triggered continuously during sustained push")]
        public UnityEvent<Vector3> OnPushSustained = new UnityEvent<Vector3>();
        
        [Tooltip("Triggered when a sustained push ends")]
        public UnityEvent<Vector3> OnPushEnd = new UnityEvent<Vector3>();

        [Header("Debug Events")]
        [Tooltip("Triggered when any movement is detected (for debugging)")]
        public UnityEvent<Vector3> OnMovementDetected = new UnityEvent<Vector3>();
    }

    /// <summary>
    /// Sophisticated controller push detection system inspired by Enhanced Grab Move.
    /// Detects forward controller movements with configurable sensitivity and filtering.
    /// Provides Unity Events for easy integration with bounce systems.
    /// </summary>
    [AddComponentMenu("XR/SFU Breathing/Controller Push Detector")]
    public class ControllerPushDetector : MonoBehaviour
    {
        [Header("Controller Setup")]
        [SerializeField]
        [Tooltip("Controller transform to track. If null, uses this transform.")]
        Transform m_ControllerTransform;
        
        [SerializeField]
        [Tooltip("Head/camera transform for head space calculations. If null, auto-detects main camera.")]
        Transform m_HeadTransform;

        [Header("Detection Mode")]
        [SerializeField]
        [Tooltip("Type of push detection to perform")]
        PushDetectionMode m_DetectionMode = PushDetectionMode.Both;
        
        [SerializeField]
        [Tooltip("Coordinate space for direction calculations")]
        PushDetectionSpace m_DetectionSpace = PushDetectionSpace.Head;

        [Header("Quick Push Detection")]
        [SerializeField]
        [Tooltip("Minimum velocity (m/s) for quick push detection")]
        [Range(0.5f, 5.0f)]
        float m_QuickPushThreshold = 2.0f;
        
        [SerializeField]
        [Tooltip("Time window (seconds) for quick push detection")]
        [Range(0.1f, 1.0f)]
        float m_QuickPushWindow = 0.3f;
        
        [SerializeField]
        [Tooltip("Enable immediate trigger on acceleration (doesn't wait for sustained movement)")]
        bool m_EnableAccelerationTrigger = true;
        
        [SerializeField]
        [Tooltip("Minimum acceleration (m/s²) for immediate trigger")]
        [Range(5f, 50f)]
        float m_AccelerationThreshold = 15f;

        [Header("Sustained Push Detection")]
        [SerializeField]
        [Tooltip("Minimum velocity (m/s) for sustained push detection")]
        [Range(0.1f, 2.0f)]
        float m_SustainedPushThreshold = 0.5f;
        
        [SerializeField]
        [Tooltip("Minimum duration (seconds) for sustained push")]
        [Range(0.1f, 1.0f)]
        float m_SustainedPushDuration = 0.2f;

        [Header("Movement Filtering")]
        [SerializeField]
        [Tooltip("Minimum distance (meters) to register as movement")]
        [Range(0.01f, 0.2f)]
        float m_MinimumDistance = 0.05f;
        
        [SerializeField]
        [Tooltip("Maximum drift velocity (m/s) - below this is ignored")]
        [Range(0.05f, 0.5f)]
        float m_MaxDriftSpeed = 0.1f;
        
        [SerializeField]
        [Tooltip("Direction tolerance (degrees) - deviation from forward allowed")]
        [Range(10f, 90f)]
        float m_DirectionTolerance = 30f;

        [Header("Smoothing & Performance")]
        [SerializeField]
        [Tooltip("Number of frames to average velocity over")]
        [Range(1, 20)]
        int m_SmoothingFrames = 5;
        
        [SerializeField]
        [Tooltip("Cooldown period (seconds) after detection to prevent rapid re-triggers")]
        [Range(0.0f, 1.0f)]
        float m_CooldownPeriod = 0.1f;

        [Header("Axis Controls")]
        [SerializeField]
        [Tooltip("Allow movement on X-axis (left/right). Disable to prevent grab move interference.")]
        bool m_EnableXAxis = false;
        
        [SerializeField]
        [Tooltip("Allow movement on Y-axis (up/down). Disable to prevent grab move interference.")]
        bool m_EnableYAxis = false;
        
        [SerializeField]
        [Tooltip("Allow movement on Z-axis (forward/backward). This is the primary push axis.")]
        bool m_EnableZAxis = true;

        [Header("Integration")]
        [SerializeField]
        [Tooltip("Only detect pushes when not grabbing (requires grab interactor on same GameObject)")]
        bool m_RequireNotGrabbing = true;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Enable debug logging and visualization")]
        bool m_EnableDebug = false;
        
        [SerializeField]
        [Tooltip("Draw debug rays showing movement direction")]
        bool m_DrawDebugRays = false;

        [Header("Events")]
        public PushDetectionEvents Events = new PushDetectionEvents();

        // Private state
        private PushDetectionState m_CurrentState = PushDetectionState.Idle;
        private Vector3 m_PreviousPosition;
        private Vector3 m_CurrentVelocity;
        private Vector3 m_PreviousVelocity;
        private Vector3 m_CurrentAcceleration;
        private Queue<Vector3> m_VelocityHistory = new Queue<Vector3>();
        private Queue<float> m_SpeedHistory = new Queue<float>();
        private Queue<Vector3> m_AccelerationHistory = new Queue<Vector3>();
        
        // Detection timing
        private float m_DetectionStartTime;
        private float m_LastDetectionTime;
        private float m_SustainedPushStartTime;
        private Vector3 m_TotalMovement;
        
        // Components
        private UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor m_GrabInteractor;
        
        // Public properties
        public Transform controllerTransform => m_ControllerTransform != null ? m_ControllerTransform : transform;
        public Transform headTransform => m_HeadTransform;
        public PushDetectionState currentState => m_CurrentState;
        public Vector3 currentVelocity => GetSmoothedVelocity();
        public Vector3 currentAcceleration => GetSmoothedAcceleration();
        public bool isDetecting => m_CurrentState != PushDetectionState.Idle && m_CurrentState != PushDetectionState.Cooldown;
        
        // Configuration properties
        public PushDetectionMode detectionMode { get => m_DetectionMode; set => m_DetectionMode = value; }
        public PushDetectionSpace detectionSpace { get => m_DetectionSpace; set => m_DetectionSpace = value; }
        public float quickPushThreshold { get => m_QuickPushThreshold; set => m_QuickPushThreshold = value; }
        public float sustainedPushThreshold { get => m_SustainedPushThreshold; set => m_SustainedPushThreshold = value; }

        void Start()
        {
            Initialize();
        }

        void Update()
        {
            if (!IsDetectionEnabled()) return;
            
            UpdateMovementTracking();
            UpdateDetectionState();
            
            if (m_DrawDebugRays)
                DrawDebugVisualization();
        }

        void Initialize()
        {
            // Auto-detect head transform if not set
            if (m_HeadTransform == null)
                AutoDetectHeadTransform();
            
            // Find grab interactor for integration
            if (m_RequireNotGrabbing)
                m_GrabInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor>();
            
            // Initialize position tracking
            m_PreviousPosition = controllerTransform.position;
            m_CurrentState = PushDetectionState.Idle;
            
            if (m_EnableDebug)
                Debug.Log($"ControllerPushDetector initialized on {gameObject.name}");
        }

        void AutoDetectHeadTransform()
        {
            var mainCamera = Camera.main;
            if (mainCamera != null)
            {
                m_HeadTransform = mainCamera.transform;
                return;
            }
            
            // Fallback: look for camera in scene
            var camera = FindObjectOfType<Camera>();
            if (camera != null)
                m_HeadTransform = camera.transform;
        }

        bool IsDetectionEnabled()
        {
            // Check if we're in cooldown
            if (m_CurrentState == PushDetectionState.Cooldown)
            {
                if (Time.time - m_LastDetectionTime >= m_CooldownPeriod)
                    m_CurrentState = PushDetectionState.Idle;
                else
                    return false;
            }
            
            // Check grab state if required
            if (m_RequireNotGrabbing && m_GrabInteractor != null)
            {
                if (m_GrabInteractor.hasSelection)
                    return false;
            }
            
            return true;
        }

        void UpdateMovementTracking()
        {
            Vector3 currentPosition = controllerTransform.position;
            Vector3 deltaPosition = currentPosition - m_PreviousPosition;
            
            // Calculate velocity
            m_PreviousVelocity = m_CurrentVelocity;
            m_CurrentVelocity = deltaPosition / Time.deltaTime;
            
            // Calculate acceleration
            Vector3 deltaVelocity = m_CurrentVelocity - m_PreviousVelocity;
            m_CurrentAcceleration = deltaVelocity / Time.deltaTime;
            
            // Update history for smoothing
            m_VelocityHistory.Enqueue(m_CurrentVelocity);
            m_SpeedHistory.Enqueue(m_CurrentVelocity.magnitude);
            m_AccelerationHistory.Enqueue(m_CurrentAcceleration);
            
            if (m_VelocityHistory.Count > m_SmoothingFrames)
            {
                m_VelocityHistory.Dequeue();
                m_SpeedHistory.Dequeue();
                m_AccelerationHistory.Dequeue();
            }
            
            // Update position for next frame
            m_PreviousPosition = currentPosition;
        }

        void UpdateDetectionState()
        {
            Vector3 smoothedVelocity = GetSmoothedVelocity();
            float speed = smoothedVelocity.magnitude;
            
            // Trigger movement event for debugging
            if (speed > m_MaxDriftSpeed)
                Events.OnMovementDetected.Invoke(smoothedVelocity);
            
            switch (m_CurrentState)
            {
                case PushDetectionState.Idle:
                    CheckForPushStart(smoothedVelocity, speed);
                    break;
                    
                case PushDetectionState.Detecting:
                    UpdateDetection(smoothedVelocity, speed);
                    break;
                    
                case PushDetectionState.SustainedPushActive:
                    UpdateSustainedPush(smoothedVelocity, speed);
                    break;
            }
        }

        void CheckForPushStart(Vector3 velocity, float speed)
        {
            // Check for immediate acceleration trigger
            if (m_EnableAccelerationTrigger && CheckAccelerationTrigger(velocity))
                return;
            
            if (!IsValidPushMovement(velocity, speed))
                return;
            
            // Start detection
            m_CurrentState = PushDetectionState.Detecting;
            m_DetectionStartTime = Time.time;
            m_TotalMovement = Vector3.zero;
            
            if (m_EnableDebug)
                Debug.Log($"Push detection started - Speed: {speed:F2} m/s");
        }

        bool CheckAccelerationTrigger(Vector3 velocity)
        {
            Vector3 acceleration = GetSmoothedAcceleration();
            float accelerationMagnitude = acceleration.magnitude;
            
            // Check if acceleration is high enough and in valid direction
            if (accelerationMagnitude >= m_AccelerationThreshold && 
                IsValidPushMovement(velocity, velocity.magnitude))
            {
                TriggerQuickPush(velocity);
                
                if (m_EnableDebug)
                    Debug.Log($"Acceleration trigger! Acceleration: {accelerationMagnitude:F1} m/s²");
                
                return true;
            }
            
            return false;
        }

        void UpdateDetection(Vector3 velocity, float speed)
        {
            float detectionTime = Time.time - m_DetectionStartTime;
            m_TotalMovement += velocity * Time.deltaTime;
            
            // Check for quick push
            if ((m_DetectionMode == PushDetectionMode.QuickPush || m_DetectionMode == PushDetectionMode.Both) &&
                speed >= m_QuickPushThreshold)
            {
                TriggerQuickPush(velocity);
                return;
            }
            
            // Check for sustained push
            if ((m_DetectionMode == PushDetectionMode.SustainedPush || m_DetectionMode == PushDetectionMode.Both) &&
                speed >= m_SustainedPushThreshold && detectionTime >= m_SustainedPushDuration)
            {
                StartSustainedPush(velocity);
                return;
            }
            
            // Check if detection window expired or movement stopped
            if (detectionTime > m_QuickPushWindow || speed < m_MaxDriftSpeed)
            {
                if (m_TotalMovement.magnitude >= m_MinimumDistance)
                {
                    if (m_EnableDebug)
                        Debug.Log($"Push detection ended - Total movement: {m_TotalMovement.magnitude:F3}m");
                }
                
                m_CurrentState = PushDetectionState.Idle;
            }
        }

        void UpdateSustainedPush(Vector3 velocity, float speed)
        {
            if (speed >= m_SustainedPushThreshold && IsValidPushMovement(velocity, speed))
            {
                // Continue sustained push
                Events.OnPushSustained.Invoke(velocity);
            }
            else
            {
                // End sustained push
                EndSustainedPush(velocity);
            }
        }

        void TriggerQuickPush(Vector3 velocity)
        {
            m_CurrentState = PushDetectionState.QuickPushDetected;
            m_LastDetectionTime = Time.time;
            
            Events.OnQuickPush.Invoke(velocity);
            
            if (m_EnableDebug)
                Debug.Log($"Quick push detected! Velocity: {velocity.magnitude:F2} m/s");
            
            EnterCooldown();
        }

        void StartSustainedPush(Vector3 velocity)
        {
            m_CurrentState = PushDetectionState.SustainedPushActive;
            m_SustainedPushStartTime = Time.time;
            
            Events.OnPushStart.Invoke(velocity);
            
            if (m_EnableDebug)
                Debug.Log($"Sustained push started! Velocity: {velocity.magnitude:F2} m/s");
        }

        void EndSustainedPush(Vector3 velocity)
        {
            Events.OnPushEnd.Invoke(velocity);
            
            if (m_EnableDebug)
            {
                float duration = Time.time - m_SustainedPushStartTime;
                Debug.Log($"Sustained push ended! Duration: {duration:F2}s");
            }
            
            EnterCooldown();
        }

        void EnterCooldown()
        {
            m_CurrentState = PushDetectionState.Cooldown;
            m_LastDetectionTime = Time.time;
        }

        bool IsValidPushMovement(Vector3 velocity, float speed)
        {
            // Check minimum speed (drift filter)
            if (speed < m_MaxDriftSpeed)
                return false;
            
            // Transform velocity to detection space
            Vector3 localVelocity = TransformVelocityToDetectionSpace(velocity);
            
            // Apply axis constraints
            if (!m_EnableXAxis) localVelocity.x = 0f;
            if (!m_EnableYAxis) localVelocity.y = 0f;
            if (!m_EnableZAxis) localVelocity.z = 0f;
            
            // Check if we have valid movement after constraints
            if (localVelocity.magnitude < m_MaxDriftSpeed)
                return false;
            
            // Check direction tolerance (primarily forward movement)
            Vector3 forwardDirection = Vector3.forward; // In detection space, forward is +Z
            float angle = Vector3.Angle(localVelocity.normalized, forwardDirection);
            
            return angle <= m_DirectionTolerance;
        }

        Vector3 TransformVelocityToDetectionSpace(Vector3 worldVelocity)
        {
            switch (m_DetectionSpace)
            {
                case PushDetectionSpace.Head:
                    return m_HeadTransform != null ? 
                        m_HeadTransform.InverseTransformDirection(worldVelocity) : 
                        worldVelocity;
                        
                case PushDetectionSpace.Controller:
                    return controllerTransform.InverseTransformDirection(worldVelocity);
                    
                case PushDetectionSpace.World:
                default:
                    return worldVelocity;
            }
        }

        Vector3 GetSmoothedVelocity()
        {
            if (m_VelocityHistory.Count == 0)
                return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (Vector3 vel in m_VelocityHistory)
                sum += vel;
            
            return sum / m_VelocityHistory.Count;
        }

        float GetSmoothedSpeed()
        {
            if (m_SpeedHistory.Count == 0)
                return 0f;
            
            float sum = 0f;
            foreach (float speed in m_SpeedHistory)
                sum += speed;
            
            return sum / m_SpeedHistory.Count;
        }

        Vector3 GetSmoothedAcceleration()
        {
            if (m_AccelerationHistory.Count == 0)
                return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (Vector3 accel in m_AccelerationHistory)
                sum += accel;
            
            return sum / m_AccelerationHistory.Count;
        }

        void DrawDebugVisualization()
        {
            Vector3 position = controllerTransform.position;
            Vector3 velocity = GetSmoothedVelocity();
            
            if (velocity.magnitude > m_MaxDriftSpeed)
            {
                // Draw current velocity (white)
                Debug.DrawRay(position, velocity * 0.5f, Color.white, Time.deltaTime);
                
                // Draw detection space forward direction
                Vector3 forwardDir = GetDetectionSpaceForward();
                Debug.DrawRay(position, forwardDir * 0.3f, Color.green, Time.deltaTime);
                
                // Draw transformed velocity in detection space
                Vector3 localVel = TransformVelocityToDetectionSpace(velocity);
                Vector3 worldLocalVel = TransformDetectionSpaceToWorld(localVel);
                Debug.DrawRay(position + Vector3.up * 0.1f, worldLocalVel * 0.5f, Color.cyan, Time.deltaTime);
            }
            
            // Draw state-specific indicators
            Color stateColor = GetStateColor();
            Debug.DrawRay(position + Vector3.up * 0.2f, Vector3.up * 0.1f, stateColor, Time.deltaTime);
        }

        Vector3 GetDetectionSpaceForward()
        {
            switch (m_DetectionSpace)
            {
                case PushDetectionSpace.Head:
                    return m_HeadTransform != null ? m_HeadTransform.forward : Vector3.forward;
                case PushDetectionSpace.Controller:
                    return controllerTransform.forward;
                case PushDetectionSpace.World:
                default:
                    return Vector3.forward;
            }
        }

        Vector3 TransformDetectionSpaceToWorld(Vector3 localVector)
        {
            switch (m_DetectionSpace)
            {
                case PushDetectionSpace.Head:
                    return m_HeadTransform != null ? 
                        m_HeadTransform.TransformDirection(localVector) : 
                        localVector;
                case PushDetectionSpace.Controller:
                    return controllerTransform.TransformDirection(localVector);
                case PushDetectionSpace.World:
                default:
                    return localVector;
            }
        }

        Color GetStateColor()
        {
            switch (m_CurrentState)
            {
                case PushDetectionState.Idle: return Color.gray;
                case PushDetectionState.Detecting: return Color.yellow;
                case PushDetectionState.QuickPushDetected: return Color.red;
                case PushDetectionState.SustainedPushActive: return Color.blue;
                case PushDetectionState.Cooldown: return Color.magenta;
                default: return Color.white;
            }
        }

        // Public API for runtime control
        public void SetDetectionEnabled(bool enabled)
        {
            this.enabled = enabled;
            if (!enabled)
                m_CurrentState = PushDetectionState.Idle;
        }

        public void ResetDetection()
        {
            m_CurrentState = PushDetectionState.Idle;
            m_VelocityHistory.Clear();
            m_SpeedHistory.Clear();
            m_AccelerationHistory.Clear();
        }

        public void TriggerManualPush(Vector3 velocity)
        {
            Events.OnQuickPush.Invoke(velocity);
            if (m_EnableDebug)
                Debug.Log($"Manual push triggered with velocity: {velocity.magnitude:F2} m/s");
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            // Validate thresholds
            m_QuickPushThreshold = Mathf.Max(m_QuickPushThreshold, m_SustainedPushThreshold);
            m_SustainedPushThreshold = Mathf.Max(m_SustainedPushThreshold, m_MaxDriftSpeed);
            m_MinimumDistance = Mathf.Max(m_MinimumDistance, 0.001f);
            m_SmoothingFrames = Mathf.Max(m_SmoothingFrames, 1);
            
            // Ensure Z-axis is enabled for forward push detection
            if (!m_EnableXAxis && !m_EnableYAxis && !m_EnableZAxis)
                m_EnableZAxis = true;
        }
#endif
    }
}