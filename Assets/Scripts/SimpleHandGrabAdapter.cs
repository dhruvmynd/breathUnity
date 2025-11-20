using UnityEngine;
using UnityEngine.XR.Hands;

namespace SFUBreathing.Locomotion
{
    /// <summary>
    /// Simple adapter that allows hand transforms and gestures to drive grab movement.
    /// Replaces controller transform with hand transform and grab button with gesture events.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Simple Hand Grab Adapter", 12)]
    public class SimpleHandGrabAdapter : MonoBehaviour
    {
        [Header("Hand Transforms")]
        [SerializeField]
        [Tooltip("Transform that represents the left hand position (typically from XR Hand prefab).")]
        Transform m_LeftHandTransform;
        
        [SerializeField]
        [Tooltip("Transform that represents the right hand position (typically from XR Hand prefab).")]
        Transform m_RightHandTransform;
        
        [Header("Settings")]
        [SerializeField]
        [Tooltip("Which hand to use when both are performing grab gestures. If None, uses whichever started first.")]
        Handedness m_PreferredHand = Handedness.Right;
        
        // Gesture state tracking
        bool m_LeftGrabActive;
        bool m_RightGrabActive;
        Handedness m_ActiveHand = Handedness.Invalid;
        
        /// <summary>
        /// Whether any hand is currently grabbing.
        /// </summary>
        public bool IsGrabbing => m_LeftGrabActive || m_RightGrabActive;
        
        /// <summary>
        /// The transform of the currently active hand, or null if no hand is grabbing.
        /// </summary>
        public Transform ActiveHandTransform
        {
            get
            {
                if (m_ActiveHand == Handedness.Left && m_LeftHandTransform != null)
                    return m_LeftHandTransform;
                else if (m_ActiveHand == Handedness.Right && m_RightHandTransform != null)
                    return m_RightHandTransform;
                else
                    return null;
            }
        }
        
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
        
        /// <summary>
        /// Call this when the left hand grab gesture is performed.
        /// Connect this to StaticHandGesture.gesturePerformed event.
        /// </summary>
        public void OnLeftGrabStarted()
        {
            m_LeftGrabActive = true;
            UpdateActiveHand();
        }
        
        /// <summary>
        /// Call this when the left hand grab gesture ends.
        /// Connect this to StaticHandGesture.gestureEnded event.
        /// </summary>
        public void OnLeftGrabEnded()
        {
            m_LeftGrabActive = false;
            UpdateActiveHand();
        }
        
        /// <summary>
        /// Call this when the right hand grab gesture is performed.
        /// Connect this to StaticHandGesture.gesturePerformed event.
        /// </summary>
        public void OnRightGrabStarted()
        {
            m_RightGrabActive = true;
            UpdateActiveHand();
        }
        
        /// <summary>
        /// Call this when the right hand grab gesture ends.
        /// Connect this to StaticHandGesture.gestureEnded event.
        /// </summary>
        public void OnRightGrabEnded()
        {
            m_RightGrabActive = false;
            UpdateActiveHand();
        }
        
        void UpdateActiveHand()
        {
            // If preferred hand is set and active, use it
            if (m_PreferredHand != Handedness.Invalid)
            {
                if (m_PreferredHand == Handedness.Left && m_LeftGrabActive)
                {
                    m_ActiveHand = Handedness.Left;
                    return;
                }
                else if (m_PreferredHand == Handedness.Right && m_RightGrabActive)
                {
                    m_ActiveHand = Handedness.Right;
                    return;
                }
            }
            
            // Otherwise, use whichever hand is active
            if (m_LeftGrabActive && !m_RightGrabActive)
                m_ActiveHand = Handedness.Left;
            else if (m_RightGrabActive && !m_LeftGrabActive)
                m_ActiveHand = Handedness.Right;
            // If both active, keep current active hand
            // If neither active, clear
            else if (!m_LeftGrabActive && !m_RightGrabActive)
                m_ActiveHand = Handedness.Invalid;
        }
    }
}