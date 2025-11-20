using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.Samples.GestureSample;

namespace RespireSharedAssets
{
    /// <summary>
    /// A wrapper around StaticHandGesture that provides dynamic gesture detection with velocity tracking.
    /// Tracks hand movement during gesture periods and provides velocity data on gesture exit.
    /// </summary>
    [RequireComponent(typeof(StaticHandGesture))]
    public class DynamicHandGesture : MonoBehaviour
    {
        [System.Serializable]
        public class VelocityEvent : UnityEvent<Vector3> { }

        [SerializeField]
        [Tooltip("Event fired when the gesture starts.")]
        UnityEvent m_GestureEnter;

        [SerializeField]
        [Tooltip("Event fired when the gesture ends.")]
        UnityEvent m_GestureExit;

        [SerializeField]
        [Tooltip("Event fired when the gesture ends, providing the average velocity during the gesture period.")]
        VelocityEvent m_GestureExitWithVelocity;

        [SerializeField]
        [Tooltip("Event fired continuously while the gesture is being performed.")]
        UnityEvent m_GestureStay;

        [SerializeField]
        [Tooltip("Event fired continuously while the gesture is being performed, providing the current velocity.")]
        VelocityEvent m_GestureStayWithVelocity;

        [SerializeField]
        [Tooltip("The hand joint to track for velocity calculation (default is Palm).")]
        XRHandJointID m_TrackedJoint = XRHandJointID.Palm;

        [SerializeField]
        [Tooltip("Minimum number of position samples required to calculate meaningful velocity.")]
        int m_MinimumSamples = 3;

        [SerializeField]
        [Tooltip("Enable automatic gesture exit after a timeout period.")]
        bool m_ForceExitEnabled = false;

        [SerializeField]
        [Tooltip("Timeout in seconds after which the gesture will be forced to exit.")]
        float m_ForceExitTimeout = 5.0f;

        [SerializeField]
        [Tooltip("Interval in seconds between gesture stay events (0 = every frame).")]
        float m_GestureStayInterval = 0.1f;

        [SerializeField]
        [Tooltip("Number of recent samples to use for stay velocity calculation (1 = instantaneous, higher = smoother).")]
        int m_StayVelocitySamples = 3;

        StaticHandGesture m_StaticHandGesture;
        bool m_IsGestureActive;
        List<Vector3> m_PositionSamples;
        List<float> m_TimeSamples;
        Vector3 m_LastPosition;
        bool m_HasValidLastPosition;
        float m_GestureStartTime;
        float m_LastGestureStayTime;

        /// <summary>
        /// Event fired when the gesture starts.
        /// </summary>
        public UnityEvent gestureEnter
        {
            get => m_GestureEnter;
            set => m_GestureEnter = value;
        }

        /// <summary>
        /// Event fired when the gesture ends.
        /// </summary>
        public UnityEvent gestureExit
        {
            get => m_GestureExit;
            set => m_GestureExit = value;
        }

        /// <summary>
        /// Event fired when the gesture ends, providing the average velocity during the gesture period.
        /// </summary>
        public VelocityEvent gestureExitWithVelocity
        {
            get => m_GestureExitWithVelocity;
            set => m_GestureExitWithVelocity = value;
        }

        /// <summary>
        /// Event fired continuously while the gesture is being performed.
        /// </summary>
        public UnityEvent gestureStay
        {
            get => m_GestureStay;
            set => m_GestureStay = value;
        }

        /// <summary>
        /// Event fired continuously while the gesture is being performed, providing the current velocity.
        /// </summary>
        public VelocityEvent gestureStayWithVelocity
        {
            get => m_GestureStayWithVelocity;
            set => m_GestureStayWithVelocity = value;
        }

        /// <summary>
        /// The hand joint to track for velocity calculation.
        /// </summary>
        public XRHandJointID trackedJoint
        {
            get => m_TrackedJoint;
            set => m_TrackedJoint = value;
        }

        /// <summary>
        /// Gets the current average velocity if a gesture is active, zero otherwise.
        /// </summary>
        public Vector3 currentVelocity
        {
            get
            {
                if (!m_IsGestureActive || m_PositionSamples.Count < 2)
                    return Vector3.zero;

                return CalculateAverageVelocity();
            }
        }

        /// <summary>
        /// Returns true if a gesture is currently being performed.
        /// </summary>
        public bool isGestureActive => m_IsGestureActive;

        /// <summary>
        /// Enable or disable automatic gesture exit after timeout.
        /// </summary>
        public bool forceExitEnabled
        {
            get => m_ForceExitEnabled;
            set => m_ForceExitEnabled = value;
        }

        /// <summary>
        /// Timeout in seconds after which the gesture will be forced to exit.
        /// </summary>
        public float forceExitTimeout
        {
            get => m_ForceExitTimeout;
            set => m_ForceExitTimeout = Mathf.Max(0.1f, value); // Minimum 0.1 seconds
        }

        /// <summary>
        /// Interval in seconds between gesture stay events (0 = every frame).
        /// </summary>
        public float gestureStayInterval
        {
            get => m_GestureStayInterval;
            set => m_GestureStayInterval = Mathf.Max(0f, value);
        }

        /// <summary>
        /// Number of recent samples to use for stay velocity calculation (1 = instantaneous, higher = smoother).
        /// </summary>
        public int stayVelocitySamples
        {
            get => m_StayVelocitySamples;
            set => m_StayVelocitySamples = Mathf.Max(1, value);
        }

        void Awake()
        {
            m_StaticHandGesture = GetComponent<StaticHandGesture>();
            m_PositionSamples = new List<Vector3>();
            m_TimeSamples = new List<float>();
        }

        void OnEnable()
        {
            if (m_StaticHandGesture != null)
            {
                m_StaticHandGesture.gesturePerformed.AddListener(OnGesturePerformed);
                m_StaticHandGesture.gestureEnded.AddListener(OnGestureEnded);
                
                // Subscribe to hand tracking events to track position
                if (m_StaticHandGesture.handTrackingEvents != null)
                {
                    m_StaticHandGesture.handTrackingEvents.jointsUpdated.AddListener(OnJointsUpdated);
                }
            }
        }

        void OnDisable()
        {
            if (m_StaticHandGesture != null)
            {
                m_StaticHandGesture.gesturePerformed.RemoveListener(OnGesturePerformed);
                m_StaticHandGesture.gestureEnded.RemoveListener(OnGestureEnded);
                
                if (m_StaticHandGesture.handTrackingEvents != null)
                {
                    m_StaticHandGesture.handTrackingEvents.jointsUpdated.RemoveListener(OnJointsUpdated);
                }
            }
        }

        void Update()
        {
            if (m_IsGestureActive)
            {
                float currentTime = Time.time;
                
                // Check for force exit timeout
                if (m_ForceExitEnabled && currentTime - m_GestureStartTime >= m_ForceExitTimeout)
                {
                    OnGestureEnded();
                    return;
                }
                
                // Check for gesture stay events
                if (m_GestureStayInterval == 0f || currentTime - m_LastGestureStayTime >= m_GestureStayInterval)
                {
                    m_LastGestureStayTime = currentTime;
                    
                    // Fire gesture stay events
                    m_GestureStay?.Invoke();
                    
                    Vector3 stayVelocity = CalculateRecentVelocity();
                    m_GestureStayWithVelocity?.Invoke(stayVelocity);
                }
            }
        }

        void OnGesturePerformed()
        {
            if (!m_IsGestureActive)
            {
                m_IsGestureActive = true;
                m_GestureStartTime = Time.time;
                m_LastGestureStayTime = Time.time;
                m_PositionSamples.Clear();
                m_TimeSamples.Clear();
                m_HasValidLastPosition = false;
                
                m_GestureEnter?.Invoke();
            }
        }

        void OnGestureEnded()
        {
            if (m_IsGestureActive)
            {
                m_IsGestureActive = false;
                
                // Calculate final velocity
                Vector3 velocity = CalculateAverageVelocity();
                
                // Fire events
                m_GestureExit?.Invoke();
                m_GestureExitWithVelocity?.Invoke(velocity);
                
                // Clear samples
                m_PositionSamples.Clear();
                m_TimeSamples.Clear();
                m_HasValidLastPosition = false;
            }
        }

        void OnJointsUpdated(XRHandJointsUpdatedEventArgs eventArgs)
        {
            if (!m_IsGestureActive || !isActiveAndEnabled)
                return;

            // Get the position of the tracked joint
            if (eventArgs.hand.GetJoint(m_TrackedJoint).TryGetPose(out var pose))
            {
                Vector3 currentPosition = pose.position;
                float currentTime = Time.time;

                // Add sample
                m_PositionSamples.Add(currentPosition);
                m_TimeSamples.Add(currentTime);

                // Limit the number of samples to prevent memory issues
                const int maxSamples = 100;
                if (m_PositionSamples.Count > maxSamples)
                {
                    m_PositionSamples.RemoveAt(0);
                    m_TimeSamples.RemoveAt(0);
                }

                m_LastPosition = currentPosition;
                m_HasValidLastPosition = true;
            }
        }

        Vector3 CalculateAverageVelocity()
        {
            if (m_PositionSamples.Count < m_MinimumSamples)
                return Vector3.zero;

            Vector3 totalVelocity = Vector3.zero;
            int velocitySamples = 0;

            for (int i = 1; i < m_PositionSamples.Count; i++)
            {
                float deltaTime = m_TimeSamples[i] - m_TimeSamples[i - 1];
                if (deltaTime > 0)
                {
                    Vector3 deltaPosition = m_PositionSamples[i] - m_PositionSamples[i - 1];
                    Vector3 velocity = deltaPosition / deltaTime;
                    totalVelocity += velocity;
                    velocitySamples++;
                }
            }

            return velocitySamples > 0 ? totalVelocity / velocitySamples : Vector3.zero;
        }

        Vector3 CalculateRecentVelocity()
        {
            if (m_PositionSamples.Count < 2)
                return Vector3.zero;

            // Determine how many samples to use (limited by available samples and user setting)
            int samplesToUse = Mathf.Min(m_StayVelocitySamples, m_PositionSamples.Count);
            
            if (samplesToUse == 1)
            {
                // Instantaneous velocity - use just the last two samples
                if (m_PositionSamples.Count < 2)
                    return Vector3.zero;
                    
                int lastIndex = m_PositionSamples.Count - 1;
                float deltaTime = m_TimeSamples[lastIndex] - m_TimeSamples[lastIndex - 1];
                
                if (deltaTime > 0)
                {
                    Vector3 deltaPosition = m_PositionSamples[lastIndex] - m_PositionSamples[lastIndex - 1];
                    return deltaPosition / deltaTime;
                }
                return Vector3.zero;
            }
            else
            {
                // Smoothed velocity over recent samples
                Vector3 totalVelocity = Vector3.zero;
                int velocitySamples = 0;
                
                // Start from the most recent samples and work backwards
                int startIndex = Mathf.Max(1, m_PositionSamples.Count - samplesToUse);
                
                for (int i = startIndex; i < m_PositionSamples.Count; i++)
                {
                    float deltaTime = m_TimeSamples[i] - m_TimeSamples[i - 1];
                    if (deltaTime > 0)
                    {
                        Vector3 deltaPosition = m_PositionSamples[i] - m_PositionSamples[i - 1];
                        Vector3 velocity = deltaPosition / deltaTime;
                        totalVelocity += velocity;
                        velocitySamples++;
                    }
                }
                
                return velocitySamples > 0 ? totalVelocity / velocitySamples : Vector3.zero;
            }
        }

        /// <summary>
        /// Manually reset the gesture state. Useful for debugging or forced state changes.
        /// </summary>
        public void ResetGestureState()
        {
            if (m_IsGestureActive)
            {
                OnGestureEnded();
            }
        }

        /// <summary>
        /// Get the current number of position samples collected during this gesture.
        /// </summary>
        public int GetSampleCount()
        {
            return m_PositionSamples.Count;
        }

        /// <summary>
        /// Get the duration of the current gesture in seconds.
        /// </summary>
        public float GetGestureDuration()
        {
            if (!m_IsGestureActive || m_TimeSamples.Count < 2)
                return 0f;

            return m_TimeSamples[m_TimeSamples.Count - 1] - m_TimeSamples[0];
        }
    }
} 