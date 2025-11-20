using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

namespace SFUBreathing.Locomotion
{
    /// <summary>
    /// Defines the coordinate space used for grab movement calculations.
    /// </summary>
    public enum MovementSpace
    {
        /// <summary>Use world space constraints (standard behavior from parent class).</summary>
        Global,
        /// <summary>Use local controller space with XR Origin orientation.</summary>
        Local,
        /// <summary>Use local controller space with head/camera orientation.</summary>
        Head
    }

    /// <summary>
    /// Enhanced version of GrabMoveProvider that provides unified movement space control
    /// and per-axis movement flip functionality. Allows choosing between Global, Local, 
    /// and Head-based coordinate spaces with independent axis controls for each.
    /// </summary>
    /// <seealso cref="GrabMoveProvider"/>
    /// <seealso cref="ConstrainedMoveProvider"/>
    [AddComponentMenu("XR/Locomotion/Enhanced Grab Move Provider", 11)]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@2.4/manual/locomotion.html")]
    public class EnhancedGrabMoveProvider : GrabMoveProvider
    {
        [SerializeField]
        [Tooltip("Coordinate space used for movement calculations. Global uses world constraints, Local uses XR Origin orientation, Head uses camera orientation.")]
        MovementSpace m_MovementSpace = MovementSpace.Global;

        /// <summary>
        /// Controls the coordinate space used for grab movement calculations.
        /// Global: Uses world space constraints (standard parent class behavior).
        /// Local: Uses local controller space with XR Origin orientation.
        /// Head: Uses local controller space with head/camera orientation.
        /// </summary>
        public MovementSpace movementSpace
        {
            get => m_MovementSpace;
            set => m_MovementSpace = value;
        }

        [SerializeField]
        [Tooltip("Transform to use as head reference when using Head movement space (typically main camera). If null, will auto-detect main camera in XR Origin.")]
        Transform m_HeadTransform;

        /// <summary>
        /// Transform to use as head reference when movement space is set to Head.
        /// If null, will auto-detect main camera in XR Origin.
        /// Only used when movementSpace is MovementSpace.Head.
        /// </summary>
        public Transform headTransform
        {
            get => m_HeadTransform;
            set => m_HeadTransform = value;
        }

        [SerializeField]
        [Tooltip("Controls whether to allow local movement on the X-axis. When disabled, left/right controller movement is ignored.")]
        bool m_EnableLocalXMovement = true;

        /// <summary>
        /// Controls whether to allow local movement on the X-axis.
        /// When disabled, left/right controller movement is ignored.
        /// </summary>
        public bool enableLocalXMovement
        {
            get => m_EnableLocalXMovement;
            set => m_EnableLocalXMovement = value;
        }

        [SerializeField]
        [Tooltip("Controls whether to allow local movement on the Y-axis. When disabled, up/down controller movement is ignored.")]
        bool m_EnableLocalYMovement = true;

        /// <summary>
        /// Controls whether to allow local movement on the Y-axis.
        /// When disabled, up/down controller movement is ignored.
        /// </summary>
        public bool enableLocalYMovement
        {
            get => m_EnableLocalYMovement;
            set => m_EnableLocalYMovement = value;
        }

        [SerializeField]
        [Tooltip("Controls whether to allow local movement on the Z-axis. When disabled, forward/backward controller movement is ignored.")]
        bool m_EnableLocalZMovement = true;

        /// <summary>
        /// Controls whether to allow local movement on the Z-axis.
        /// When disabled, forward/backward controller movement is ignored.
        /// </summary>
        public bool enableLocalZMovement
        {
            get => m_EnableLocalZMovement;
            set => m_EnableLocalZMovement = value;
        }

        [SerializeField]
        [Tooltip("Flip movement direction on the X-axis. When enabled, moving the controller left will move the world right.")]
        bool m_FlipXMovement = false;

        /// <summary>
        /// Controls whether to flip movement direction on the X-axis.
        /// When enabled, moving the controller left will move the world right.
        /// </summary>
        public bool flipXMovement
        {
            get => m_FlipXMovement;
            set => m_FlipXMovement = value;
        }

        [SerializeField]
        [Tooltip("Flip movement direction on the Y-axis. When enabled, moving the controller up will move the world down.")]
        bool m_FlipYMovement = false;

        /// <summary>
        /// Controls whether to flip movement direction on the Y-axis.
        /// When enabled, moving the controller up will move the world down.
        /// </summary>
        public bool flipYMovement
        {
            get => m_FlipYMovement;
            set => m_FlipYMovement = value;
        }

        [SerializeField]
        [Tooltip("Flip movement direction on the Z-axis. When enabled, moving the controller forward will move the world backward.")]
        bool m_FlipZMovement = false;

        /// <summary>
        /// Controls whether to flip movement direction on the Z-axis.
        /// When enabled, moving the controller forward will move the world backward.
        /// </summary>
        public bool flipZMovement
        {
            get => m_FlipZMovement;
            set => m_FlipZMovement = value;
        }

        [Header("Hand Tracking Support")]
        [SerializeField]
        [Tooltip("Enable hand tracking mode for grab movement. When enabled, hand gestures will trigger grab movement instead of controller buttons.")]
        bool m_UseHandTracking = false;
        
        [SerializeField]
        [Tooltip("Simple Hand Grab Adapter that provides hand transforms and gesture state. Required when Use Hand Tracking is enabled.")]
        SimpleHandGrabAdapter m_HandAdapter;
        
        /// <summary>
        /// Whether to use hand tracking for grab movement instead of controller input.
        /// </summary>
        public bool useHandTracking
        {
            get => m_UseHandTracking;
            set => m_UseHandTracking = value;
        }
        
        /// <summary>
        /// The hand adapter component that provides hand tracking data.
        /// </summary>
        public SimpleHandGrabAdapter handAdapter
        {
            get => m_HandAdapter;
            set => m_HandAdapter = value;
        }

        bool m_EnhancedIsMoving;
        Vector3 m_EnhancedPreviousControllerLocalPosition;
        Vector3 m_EnhancedPreviousControllerHeadLocalPosition;
        
        [Header("Debug")]
        [SerializeField]
        [Tooltip("Enable debug logging for hand tracking grab movement.")]
        bool m_EnableHandTrackingDebug = false;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            AutoDetectHeadTransform();
        }
        
        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected new void OnEnable()
        {
            // Call base to ensure input actions are enabled
            base.OnEnable();
            
            // Reset movement state
            m_EnhancedIsMoving = false;
            
            // Initialize previous positions to current to prevent jumps
            var activeTransform = GetActiveTransform();
            if (activeTransform != null)
            {
                m_EnhancedPreviousControllerLocalPosition = activeTransform.localPosition;
                
                // For head space mode, initialize relative position
                var headTransform = GetLocalSpaceTransform();
                if (headTransform != null && m_MovementSpace == MovementSpace.Head)
                {
                    m_EnhancedPreviousControllerHeadLocalPosition = headTransform.InverseTransformPoint(activeTransform.position);
                }
            }
            
            if (m_EnableHandTrackingDebug)
            {
                Debug.Log($"[EnhancedGrabMove] OnEnable - Reset previous positions to prevent movement jump");
            }
        }
        
        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected new void OnDisable()
        {
            // Reset movement state when disabled
            m_EnhancedIsMoving = false;
            
            if (m_EnableHandTrackingDebug)
            {
                Debug.Log($"[EnhancedGrabMove] OnDisable - Reset movement state");
            }
            
            // Call base to ensure input actions are disabled
            base.OnDisable();
        }

        /// <summary>
        /// Automatically detects and assigns the head transform if not manually set.
        /// Looks for the main camera in the XR Origin hierarchy.
        /// </summary>
        void AutoDetectHeadTransform()
        {
            if (m_HeadTransform != null)
                return; // Already manually assigned

            var xrOrigin = mediator?.xrOrigin?.Origin;
            if (xrOrigin == null)
                return;

            // Try to find main camera as a child of XR Origin
            var mainCamera = Camera.main;
            if (mainCamera != null && IsChildOf(mainCamera.transform, xrOrigin.transform))
            {
                m_HeadTransform = mainCamera.transform;
                return;
            }

            // Fallback: Look for any camera in XR Origin hierarchy
            var camera = xrOrigin.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                m_HeadTransform = camera.transform;
            }
        }

        /// <summary>
        /// Checks if a transform is a child of another transform.
        /// </summary>
        /// <param name="child">The potential child transform.</param>
        /// <param name="parent">The potential parent transform.</param>
        /// <returns>True if child is a descendant of parent.</returns>
        bool IsChildOf(Transform child, Transform parent)
        {
            var current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = current.parent;
            }
            return false;
        }

        /// <summary>
        /// Gets the effective transform to use for local space calculations based on movement space setting.
        /// Returns head transform for Head mode, XR Origin transform for Local mode.
        /// Returns null for Global mode as it uses parent class behavior.
        /// </summary>
        Transform GetLocalSpaceTransform()
        {
            var xrOrigin = mediator?.xrOrigin?.Origin;
            if (xrOrigin == null)
                return null;

            switch (m_MovementSpace)
            {
                case MovementSpace.Head:
                    return m_HeadTransform != null ? m_HeadTransform : xrOrigin.transform;
                case MovementSpace.Local:
                    return xrOrigin.transform;
                case MovementSpace.Global:
                default:
                    return null; // Global mode uses parent class behavior
            }
        }
        
        /// <summary>
        /// Gets the active transform for movement tracking (either controller or hand).
        /// </summary>
        Transform GetActiveTransform()
        {
            if (m_UseHandTracking && m_HandAdapter != null && m_HandAdapter.ActiveHandTransform != null)
            {
                if (m_EnableHandTrackingDebug && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[EnhancedGrabMove] Using hand transform: {m_HandAdapter.ActiveHandTransform.position}");
                }
                return m_HandAdapter.ActiveHandTransform;
            }
            
            return controllerTransform;
        }
        
        /// <summary>
        /// Override to support hand tracking gestures as grab input.
        /// </summary>
        public new bool IsGrabbing()
        {
            // Check hand tracking first
            if (m_UseHandTracking && m_HandAdapter != null)
            {
                // For hand tracking, we don't need to check enableMoveWhileSelecting
                // since hands don't have "selection" like controllers do
                bool isGrabbing = m_HandAdapter.IsGrabbing;
                
                if (m_EnableHandTrackingDebug && Time.frameCount % 30 == 0) // Log every half second
                {
                    Debug.Log($"[EnhancedGrabMove] IsGrabbing check - UseHandTracking: {m_UseHandTracking}, " +
                              $"HandAdapter: {m_HandAdapter != null}, HandAdapter.IsGrabbing: {(m_HandAdapter != null ? m_HandAdapter.IsGrabbing : false)}, " +
                              $"Result: {isGrabbing}");
                }
                
                return isGrabbing;
            }
            
            // Fall back to base implementation for controller input
            return base.IsGrabbing();
        }

        /// <inheritdoc/>
        protected override Vector3 ComputeDesiredMove(out bool attemptingMove)
        {
            if (m_EnableHandTrackingDebug && Time.frameCount % 60 == 0 && m_UseHandTracking)
            {
                Debug.Log($"[EnhancedGrabMove] ComputeDesiredMove - MovementSpace: {m_MovementSpace}, " +
                          $"canMove: {canMove}, IsGrabbing: {IsGrabbing()}");
            }
            
            switch (m_MovementSpace)
            {
                case MovementSpace.Global:
                    return ComputeGlobalMove(out attemptingMove);
                    
                case MovementSpace.Local:
                case MovementSpace.Head:
                    return ComputeLocalMove(out attemptingMove);
                    
                default:
                    attemptingMove = false;
                    return Vector3.zero;
            }
        }

        /// <summary>
        /// Computes movement using global world space constraints (standard parent class behavior).
        /// </summary>
        Vector3 ComputeGlobalMove(out bool attemptingMove)
        {
            // Use parent class implementation and apply flip multipliers
            var baseMove = base.ComputeDesiredMove(out attemptingMove);
            
            if (!attemptingMove)
                return baseMove;

            // Apply flip multipliers to the base movement
            var flipMultiplier = new Vector3(
                m_FlipXMovement ? -1f : 1f,
                m_FlipYMovement ? -1f : 1f,
                m_FlipZMovement ? -1f : 1f
            );

            return Vector3.Scale(baseMove, flipMultiplier);
        }

        /// <summary>
        /// Computes movement using local controller space with axis constraints.
        /// Uses either XR Origin or head transform based on movement space setting.
        /// </summary>
        Vector3 ComputeLocalMove(out bool attemptingMove)
        {
            attemptingMove = false;
            var xrOrigin = mediator.xrOrigin?.Origin;
            var wasMoving = m_EnhancedIsMoving;
            m_EnhancedIsMoving = canMove && IsGrabbing() && xrOrigin != null;
            
            if (m_EnableHandTrackingDebug && m_UseHandTracking && (wasMoving != m_EnhancedIsMoving))
            {
                Debug.Log($"[EnhancedGrabMove] Movement state changed - WasMoving: {wasMoving}, IsMoving: {m_EnhancedIsMoving}, " +
                          $"canMove: {canMove}, IsGrabbing: {IsGrabbing()}, xrOrigin: {xrOrigin != null}");
            }
            
            if (!m_EnhancedIsMoving)
                return Vector3.zero;

            if (m_MovementSpace == MovementSpace.Head)
            {
                return ComputeHeadLocalMove(out attemptingMove, wasMoving);
            }
            else
            {
                return ComputeXROriginLocalMove(out attemptingMove, wasMoving);
            }
        }

        /// <summary>
        /// Computes movement using XR Origin local space (Local movement mode).
        /// </summary>
        Vector3 ComputeXROriginLocalMove(out bool attemptingMove, bool wasMoving)
        {
            var xrOrigin = mediator.xrOrigin?.Origin;
            var activeTransform = GetActiveTransform();
            var controllerLocalPosition = activeTransform.localPosition;
            
            if (!wasMoving && m_EnhancedIsMoving)
            {
                // Do not move the first frame of grab
                m_EnhancedPreviousControllerLocalPosition = controllerLocalPosition;
                attemptingMove = false;
                return Vector3.zero;
            }

            attemptingMove = true;
            
            // Calculate local movement delta in XR Origin space
            var localDelta = m_EnhancedPreviousControllerLocalPosition - controllerLocalPosition;
            
            // Apply local axis constraints
            if (!m_EnableLocalXMovement)
                localDelta.x = 0f;
            if (!m_EnableLocalYMovement)
                localDelta.y = 0f;
            if (!m_EnableLocalZMovement)
                localDelta.z = 0f;
            
            // Apply movement factor
            localDelta *= moveFactor;
            
            // Transform to world space using XR Origin orientation
            var worldMove = xrOrigin.transform.TransformVector(localDelta);
            
            // Update previous position for next frame
            m_EnhancedPreviousControllerLocalPosition = controllerLocalPosition;

            // Apply flip multipliers to the world space movement
            var flipMultiplier = new Vector3(
                m_FlipXMovement ? -1f : 1f,
                m_FlipYMovement ? -1f : 1f,
                m_FlipZMovement ? -1f : 1f
            );

            return Vector3.Scale(worldMove, flipMultiplier);
        }

        /// <summary>
        /// Computes movement using head local space (Head movement mode).
        /// Uses the same approach as original GrabMoveProvider but with head transform instead of XR Origin.
        /// </summary>
        Vector3 ComputeHeadLocalMove(out bool attemptingMove, bool wasMoving)
        {
            var headTransform = GetLocalSpaceTransform();
            if (headTransform == null)
            {
                attemptingMove = false;
                return Vector3.zero;
            }

            // Calculate controller/hand position relative to head transform
            // This mimics controllerTransform.localPosition but relative to head instead of XR Origin
            var activeTransform = GetActiveTransform();
            var controllerRelativeToHead = headTransform.InverseTransformPoint(activeTransform.position);
            
            if (!wasMoving && m_EnhancedIsMoving)
            {
                // Do not move the first frame of grab
                m_EnhancedPreviousControllerHeadLocalPosition = controllerRelativeToHead;
                attemptingMove = false;
                return Vector3.zero;
            }

            attemptingMove = true;
            
            // Calculate movement delta in head's local space (same pattern as original)
            var localDelta = m_EnhancedPreviousControllerHeadLocalPosition - controllerRelativeToHead;
            
            // Debug logging to understand what's happening
            Debug.Log($"Head Mode Debug - Local Delta: {localDelta}, Head Forward: {headTransform.forward}, Head Right: {headTransform.right}");
            
            // Apply local axis constraints in head space
            if (!m_EnableLocalXMovement)
                localDelta.x = 0f;
            if (!m_EnableLocalYMovement)
                localDelta.y = 0f;
            if (!m_EnableLocalZMovement)
                localDelta.z = 0f;
            
            Debug.Log($"Head Mode Debug - After Constraints: {localDelta}");
            
            // Apply movement factor
            localDelta *= moveFactor;
            
            // Transform the local delta to world space using head orientation (same as original pattern)
            var worldMove = headTransform.TransformVector(localDelta);
            
            Debug.Log($"Head Mode Debug - Final World Move: {worldMove}");
            
            // Update previous position for next frame
            m_EnhancedPreviousControllerHeadLocalPosition = controllerRelativeToHead;

            // Apply flip multipliers to the world space movement
            var flipMultiplier = new Vector3(
                m_FlipXMovement ? -1f : 1f,
                m_FlipYMovement ? -1f : 1f,
                m_FlipZMovement ? -1f : 1f
            );

            return Vector3.Scale(worldMove, flipMultiplier);
        }
    }
}