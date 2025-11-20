using UnityEngine;
using UnityEditor;
using SFUBreathing.VR;

namespace SFUBreathing.Editor.VR
{
    /// <summary>
    /// Custom editor for ControllerPushDetector with organized layout and real-time debugging features.
    /// </summary>
    [CustomEditor(typeof(ControllerPushDetector), true)]
    [CanEditMultipleObjects]
    public class ControllerPushDetectorEditor : UnityEditor.Editor
    {
        // Serialized properties
        SerializedProperty m_ControllerTransform;
        SerializedProperty m_HeadTransform;
        SerializedProperty m_DetectionMode;
        SerializedProperty m_DetectionSpace;
        SerializedProperty m_QuickPushThreshold;
        SerializedProperty m_QuickPushWindow;
        SerializedProperty m_EnableAccelerationTrigger;
        SerializedProperty m_AccelerationThreshold;
        SerializedProperty m_SustainedPushThreshold;
        SerializedProperty m_SustainedPushDuration;
        SerializedProperty m_MinimumDistance;
        SerializedProperty m_MaxDriftSpeed;
        SerializedProperty m_DirectionTolerance;
        SerializedProperty m_SmoothingFrames;
        SerializedProperty m_CooldownPeriod;
        SerializedProperty m_EnableXAxis;
        SerializedProperty m_EnableYAxis;
        SerializedProperty m_EnableZAxis;
        SerializedProperty m_RequireNotGrabbing;
        SerializedProperty m_EnableDebug;
        SerializedProperty m_DrawDebugRays;
        SerializedProperty m_Events;

        // Foldout states
        bool m_ShowQuickPushSettings = true;
        bool m_ShowSustainedPushSettings = true;
        bool m_ShowFilteringSettings = true;
        bool m_ShowAdvancedSettings = false;
        bool m_ShowDebugSettings = false;
        bool m_ShowEventsSettings = false;
        bool m_ShowRuntimeInfo = false;

        // GUI content
        static class Contents
        {
            public static readonly GUIContent controllerTransform = new GUIContent("Controller Transform", 
                "Controller transform to track. If null, uses this transform.");
            public static readonly GUIContent headTransform = new GUIContent("Head Transform", 
                "Head/camera transform for head space calculations. If null, auto-detects main camera.");
            public static readonly GUIContent detectionMode = new GUIContent("Detection Mode", 
                "Type of push detection to perform");
            public static readonly GUIContent detectionSpace = new GUIContent("Detection Space", 
                "Coordinate space for direction calculations");
            
            public static readonly GUIContent quickPushThreshold = new GUIContent("Velocity Threshold", 
                "Minimum velocity (m/s) for quick push detection");
            public static readonly GUIContent quickPushWindow = new GUIContent("Detection Window", 
                "Time window (seconds) for quick push detection");
                
            public static readonly GUIContent sustainedPushThreshold = new GUIContent("Velocity Threshold", 
                "Minimum velocity (m/s) for sustained push detection");
            public static readonly GUIContent sustainedPushDuration = new GUIContent("Minimum Duration", 
                "Minimum duration (seconds) for sustained push");
                
            public static readonly GUIContent minimumDistance = new GUIContent("Minimum Distance", 
                "Minimum distance (meters) to register as movement");
            public static readonly GUIContent maxDriftSpeed = new GUIContent("Max Drift Speed", 
                "Maximum drift velocity (m/s) - below this is ignored");
            public static readonly GUIContent directionTolerance = new GUIContent("Direction Tolerance", 
                "Direction tolerance (degrees) - deviation from forward allowed");
                
            public static readonly GUIContent smoothingFrames = new GUIContent("Smoothing Frames", 
                "Number of frames to average velocity over");
            public static readonly GUIContent cooldownPeriod = new GUIContent("Cooldown Period", 
                "Cooldown period (seconds) after detection to prevent rapid re-triggers");
                
            public static readonly GUIContent enableXAxis = new GUIContent("Enable X Movement", 
                "Allow movement on X-axis (left/right). Disable to prevent grab move interference.");
            public static readonly GUIContent enableYAxis = new GUIContent("Enable Y Movement", 
                "Allow movement on Y-axis (up/down). Disable to prevent grab move interference.");
            public static readonly GUIContent enableZAxis = new GUIContent("Enable Z Movement", 
                "Allow movement on Z-axis (forward/backward). This is the primary push axis.");
                
            public static readonly GUIContent requireNotGrabbing = new GUIContent("Require Not Grabbing", 
                "Only detect pushes when not grabbing (requires grab interactor on same GameObject)");
            public static readonly GUIContent enableDebug = new GUIContent("Enable Debug Logging", 
                "Enable debug logging and visualization");
            public static readonly GUIContent drawDebugRays = new GUIContent("Draw Debug Rays", 
                "Draw debug rays showing movement direction");
        }

        void OnEnable()
        {
            // Find all serialized properties
            m_ControllerTransform = serializedObject.FindProperty("m_ControllerTransform");
            m_HeadTransform = serializedObject.FindProperty("m_HeadTransform");
            m_DetectionMode = serializedObject.FindProperty("m_DetectionMode");
            m_DetectionSpace = serializedObject.FindProperty("m_DetectionSpace");
            m_QuickPushThreshold = serializedObject.FindProperty("m_QuickPushThreshold");
            m_QuickPushWindow = serializedObject.FindProperty("m_QuickPushWindow");
            m_EnableAccelerationTrigger = serializedObject.FindProperty("m_EnableAccelerationTrigger");
            m_AccelerationThreshold = serializedObject.FindProperty("m_AccelerationThreshold");
            m_SustainedPushThreshold = serializedObject.FindProperty("m_SustainedPushThreshold");
            m_SustainedPushDuration = serializedObject.FindProperty("m_SustainedPushDuration");
            m_MinimumDistance = serializedObject.FindProperty("m_MinimumDistance");
            m_MaxDriftSpeed = serializedObject.FindProperty("m_MaxDriftSpeed");
            m_DirectionTolerance = serializedObject.FindProperty("m_DirectionTolerance");
            m_SmoothingFrames = serializedObject.FindProperty("m_SmoothingFrames");
            m_CooldownPeriod = serializedObject.FindProperty("m_CooldownPeriod");
            m_EnableXAxis = serializedObject.FindProperty("m_EnableXAxis");
            m_EnableYAxis = serializedObject.FindProperty("m_EnableYAxis");
            m_EnableZAxis = serializedObject.FindProperty("m_EnableZAxis");
            m_RequireNotGrabbing = serializedObject.FindProperty("m_RequireNotGrabbing");
            m_EnableDebug = serializedObject.FindProperty("m_EnableDebug");
            m_DrawDebugRays = serializedObject.FindProperty("m_DrawDebugRays");
            m_Events = serializedObject.FindProperty("Events");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var detector = target as ControllerPushDetector;
            var currentMode = (PushDetectionMode)m_DetectionMode.enumValueIndex;

            DrawHeader();
            EditorGUILayout.Space();

            // Controller Setup
            DrawControllerSetup();
            EditorGUILayout.Space();

            // Detection Mode
            DrawDetectionMode(currentMode);
            EditorGUILayout.Space();

            // Mode-specific settings
            DrawModeSpecificSettings(currentMode);

            // Filtering settings
            DrawFilteringSettings();

            // Axis controls
            DrawAxisControls();

            // Advanced settings
            DrawAdvancedSettings();

            // Debug settings
            DrawDebugSettings();

            // Events
            DrawEvents();

            // Runtime information (play mode only)
            if (Application.isPlaying)
                DrawRuntimeInfo(detector);

            serializedObject.ApplyModifiedProperties();
        }

        void DrawHeader()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            var headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter
            };
            EditorGUILayout.LabelField("Controller Push Detector", headerStyle);
            
            var descStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            EditorGUILayout.LabelField("Sophisticated push detection for VR controllers with Unity Events integration", descStyle);
            EditorGUILayout.EndVertical();
        }

        void DrawControllerSetup()
        {
            EditorGUILayout.LabelField("Controller Setup", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(m_ControllerTransform, Contents.controllerTransform);
            
            var detectionSpace = (PushDetectionSpace)m_DetectionSpace.enumValueIndex;
            if (detectionSpace == PushDetectionSpace.Head)
            {
                EditorGUILayout.PropertyField(m_HeadTransform, Contents.headTransform);
                if (m_HeadTransform.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Head transform will be auto-detected from main camera if not set.", MessageType.Info);
                }
            }
            
            EditorGUI.indentLevel--;
        }

        void DrawDetectionMode(PushDetectionMode currentMode)
        {
            EditorGUILayout.LabelField("Detection Configuration", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(m_DetectionMode, Contents.detectionMode);
            EditorGUILayout.PropertyField(m_DetectionSpace, Contents.detectionSpace);
            
            // Show space-specific help
            var detectionSpace = (PushDetectionSpace)m_DetectionSpace.enumValueIndex;
            switch (detectionSpace)
            {
                case PushDetectionSpace.Head:
                    EditorGUILayout.HelpBox("Head Space: Forward direction based on where the user is looking. Best for natural push gestures.", MessageType.Info);
                    break;
                case PushDetectionSpace.Controller:
                    EditorGUILayout.HelpBox("Controller Space: Forward direction based on controller orientation. Good for precise directional control.", MessageType.Info);
                    break;
                case PushDetectionSpace.World:
                    EditorGUILayout.HelpBox("World Space: Forward direction is global Z-axis. Consistent regardless of user orientation.", MessageType.Info);
                    break;
            }
            
            EditorGUI.indentLevel--;
        }

        void DrawModeSpecificSettings(PushDetectionMode currentMode)
        {
            // Quick Push Settings
            if (currentMode == PushDetectionMode.QuickPush || currentMode == PushDetectionMode.Both)
            {
                m_ShowQuickPushSettings = EditorGUILayout.Foldout(m_ShowQuickPushSettings, "Quick Push Detection", true);
                if (m_ShowQuickPushSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_QuickPushThreshold, Contents.quickPushThreshold);
                    EditorGUILayout.PropertyField(m_QuickPushWindow, Contents.quickPushWindow);
                    
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(m_EnableAccelerationTrigger, new GUIContent("Enable Acceleration Trigger", "Enable immediate trigger on high acceleration"));
                    if (m_EnableAccelerationTrigger.boolValue)
                    {
                        EditorGUI.indentLevel++;
                        EditorGUILayout.PropertyField(m_AccelerationThreshold, new GUIContent("Acceleration Threshold", "Minimum acceleration (m/s²) for immediate trigger"));
                        EditorGUI.indentLevel--;
                    }
                    
                    // Show quick push help
                    EditorGUILayout.HelpBox("Quick Push: Detects fast forward movements for instant bounce triggers. Acceleration trigger provides immediate response without waiting for sustained movement.", MessageType.None);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();
            }

            // Sustained Push Settings
            if (currentMode == PushDetectionMode.SustainedPush || currentMode == PushDetectionMode.Both)
            {
                m_ShowSustainedPushSettings = EditorGUILayout.Foldout(m_ShowSustainedPushSettings, "Sustained Push Detection", true);
                if (m_ShowSustainedPushSettings)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(m_SustainedPushThreshold, Contents.sustainedPushThreshold);
                    EditorGUILayout.PropertyField(m_SustainedPushDuration, Contents.sustainedPushDuration);
                    
                    // Show sustained push help
                    EditorGUILayout.HelpBox("Sustained Push: Detects continuous forward movement over time. Good for gradual, controlled movement interactions.", MessageType.None);
                    EditorGUI.indentLevel--;
                }
                EditorGUILayout.Space();
            }
        }

        void DrawFilteringSettings()
        {
            m_ShowFilteringSettings = EditorGUILayout.Foldout(m_ShowFilteringSettings, "Movement Filtering", true);
            if (m_ShowFilteringSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_MinimumDistance, Contents.minimumDistance);
                EditorGUILayout.PropertyField(m_MaxDriftSpeed, Contents.maxDriftSpeed);
                EditorGUILayout.PropertyField(m_DirectionTolerance, Contents.directionTolerance);
                
                // Validation warnings
                if (m_MaxDriftSpeed.floatValue >= m_SustainedPushThreshold.floatValue)
                {
                    EditorGUILayout.HelpBox("Warning: Max Drift Speed is >= Sustained Push Threshold. This may prevent sustained push detection.", MessageType.Warning);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        void DrawAxisControls()
        {
            EditorGUILayout.LabelField("Axis Controls", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.PropertyField(m_EnableXAxis, Contents.enableXAxis);
            EditorGUILayout.PropertyField(m_EnableYAxis, Contents.enableYAxis);
            EditorGUILayout.PropertyField(m_EnableZAxis, Contents.enableZAxis);
            
            // Validation
            if (!m_EnableXAxis.boolValue && !m_EnableYAxis.boolValue && !m_EnableZAxis.boolValue)
            {
                EditorGUILayout.HelpBox("Warning: All axes are disabled. At least one axis should be enabled for detection.", MessageType.Warning);
            }
            else if (!m_EnableZAxis.boolValue)
            {
                EditorGUILayout.HelpBox("Note: Z-axis (forward/backward) is disabled. This is the primary push direction.", MessageType.Info);
            }
            
            EditorGUI.indentLevel--;
            EditorGUILayout.Space();
        }

        void DrawAdvancedSettings()
        {
            m_ShowAdvancedSettings = EditorGUILayout.Foldout(m_ShowAdvancedSettings, "Advanced Settings", true);
            if (m_ShowAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_SmoothingFrames, Contents.smoothingFrames);
                EditorGUILayout.PropertyField(m_CooldownPeriod, Contents.cooldownPeriod);
                EditorGUILayout.PropertyField(m_RequireNotGrabbing, Contents.requireNotGrabbing);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        void DrawDebugSettings()
        {
            m_ShowDebugSettings = EditorGUILayout.Foldout(m_ShowDebugSettings, "Debug Settings", true);
            if (m_ShowDebugSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_EnableDebug, Contents.enableDebug);
                EditorGUILayout.PropertyField(m_DrawDebugRays, Contents.drawDebugRays);
                
                if (m_DrawDebugRays.boolValue)
                {
                    EditorGUILayout.HelpBox("Debug Rays: White = Current velocity, Green = Detection space forward, Cyan = Transformed velocity, Colored ray above = Current state", MessageType.Info);
                }
                
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        void DrawEvents()
        {
            m_ShowEventsSettings = EditorGUILayout.Foldout(m_ShowEventsSettings, "Unity Events", true);
            if (m_ShowEventsSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Events, true);
                
                EditorGUILayout.HelpBox("Tip: Wire OnQuickPush to CharacterController.KickOffBounceBackward for instant bounce on push gestures.", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }

        void DrawRuntimeInfo(ControllerPushDetector detector)
        {
            m_ShowRuntimeInfo = EditorGUILayout.Foldout(m_ShowRuntimeInfo, "Runtime Information", true);
            if (m_ShowRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                
                EditorGUILayout.EnumPopup("Current State", detector.currentState);
                EditorGUILayout.Vector3Field("Current Velocity", detector.currentVelocity);
                EditorGUILayout.FloatField("Speed (m/s)", detector.currentVelocity.magnitude);
                EditorGUILayout.Vector3Field("Current Acceleration", detector.currentAcceleration);
                EditorGUILayout.FloatField("Acceleration (m/s²)", detector.currentAcceleration.magnitude);
                EditorGUILayout.Toggle("Is Detecting", detector.isDetecting);
                
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
                
                // Manual trigger button
                EditorGUILayout.Space();
                if (GUILayout.Button("Trigger Manual Push"))
                {
                    detector.TriggerManualPush(Vector3.forward * 2.0f);
                }
            }
        }

        // Scene view visualization
        void OnSceneGUI()
        {
            var detector = target as ControllerPushDetector;
            if (!detector.enabled || !Application.isPlaying)
                return;

            var transform = detector.controllerTransform;
            if (transform == null)
                return;

            Handles.color = Color.green;
            var forwardDir = Vector3.forward;
            switch (detector.detectionSpace)
            {
                case PushDetectionSpace.Head:
                    if (detector.headTransform != null)
                        forwardDir = detector.headTransform.forward;
                    break;
                case PushDetectionSpace.Controller:
                    forwardDir = transform.forward;
                    break;
            }

            // Draw detection space forward direction
            Handles.ArrowHandleCap(0, transform.position, Quaternion.LookRotation(forwardDir), 0.3f, EventType.Repaint);
            
            // Draw current velocity if significant
            var velocity = detector.currentVelocity;
            if (velocity.magnitude > 0.1f)
            {
                Handles.color = Color.white;
                Handles.DrawLine(transform.position, transform.position + velocity * 0.5f);
            }
            
            // Draw state indicator
            Handles.color = GetStateColor(detector.currentState);
            Handles.SphereHandleCap(0, transform.position + Vector3.up * 0.2f, Quaternion.identity, 0.05f, EventType.Repaint);
        }

        Color GetStateColor(PushDetectionState state)
        {
            switch (state)
            {
                case PushDetectionState.Idle: return Color.gray;
                case PushDetectionState.Detecting: return Color.yellow;
                case PushDetectionState.QuickPushDetected: return Color.red;
                case PushDetectionState.SustainedPushActive: return Color.blue;
                case PushDetectionState.Cooldown: return Color.magenta;
                default: return Color.white;
            }
        }
    }
}