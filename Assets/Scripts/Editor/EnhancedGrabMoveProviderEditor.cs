using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Interaction.Toolkit.Locomotion.Movement;
using SFUBreathing.Locomotion;

namespace SFUBreathing.Editor.Locomotion
{
    /// <summary>
    /// Custom editor for EnhancedGrabMoveProvider that extends the original GrabMoveProvider editor
    /// to include local axis movement controls and per-axis movement flip controls in the inspector.
    /// </summary>
    [CustomEditor(typeof(EnhancedGrabMoveProvider), true)]
    [CanEditMultipleObjects]
    public class EnhancedGrabMoveProviderEditor : GrabMoveProviderEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.movementSpace"/>.</summary>
        protected SerializedProperty m_MovementSpace;
        
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.headTransform"/>.</summary>
        protected SerializedProperty m_HeadTransform;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.enableLocalXMovement"/>.</summary>
        protected SerializedProperty m_EnableLocalXMovement;
        
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.enableLocalYMovement"/>.</summary>
        protected SerializedProperty m_EnableLocalYMovement;
        
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.enableLocalZMovement"/>.</summary>
        protected SerializedProperty m_EnableLocalZMovement;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.flipXMovement"/>.</summary>
        protected SerializedProperty m_FlipXMovement;
        
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.flipYMovement"/>.</summary>
        protected SerializedProperty m_FlipYMovement;
        
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.flipZMovement"/>.</summary>
        protected SerializedProperty m_FlipZMovement;
        
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.useHandTracking"/>.</summary>
        protected SerializedProperty m_UseHandTracking;
        
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="EnhancedGrabMoveProvider.handAdapter"/>.</summary>
        protected SerializedProperty m_HandAdapter;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static new partial class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.movementSpace"/>.</summary>
            public static readonly GUIContent movementSpace = EditorGUIUtility.TrTextContent("Movement Space", "Coordinate space used for movement calculations. Global uses world constraints, Local uses XR Origin orientation, Head uses camera orientation.");
            
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.headTransform"/>.</summary>
            public static readonly GUIContent headTransform = EditorGUIUtility.TrTextContent("Head Transform", "Transform to use as head reference when using Head movement space (typically main camera). If null, will auto-detect main camera in XR Origin.");

            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.enableLocalXMovement"/>.</summary>
            public static readonly GUIContent enableLocalXMovement = EditorGUIUtility.TrTextContent("Enable Local X Movement", "Controls whether to allow local movement on the X-axis. When disabled, left/right controller movement is ignored.");
            
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.enableLocalYMovement"/>.</summary>
            public static readonly GUIContent enableLocalYMovement = EditorGUIUtility.TrTextContent("Enable Local Y Movement", "Controls whether to allow local movement on the Y-axis. When disabled, up/down controller movement is ignored.");
            
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.enableLocalZMovement"/>.</summary>
            public static readonly GUIContent enableLocalZMovement = EditorGUIUtility.TrTextContent("Enable Local Z Movement", "Controls whether to allow local movement on the Z-axis. When disabled, forward/backward controller movement is ignored.");

            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.flipXMovement"/>.</summary>
            public static readonly GUIContent flipXMovement = EditorGUIUtility.TrTextContent("Flip X Movement", "Flip movement direction on the X-axis. When enabled, moving the controller left will move the world right.");
            
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.flipYMovement"/>.</summary>
            public static readonly GUIContent flipYMovement = EditorGUIUtility.TrTextContent("Flip Y Movement", "Flip movement direction on the Y-axis. When enabled, moving the controller up will move the world down.");
            
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.flipZMovement"/>.</summary>
            public static readonly GUIContent flipZMovement = EditorGUIUtility.TrTextContent("Flip Z Movement", "Flip movement direction on the Z-axis. When enabled, moving the controller forward will move the world backward.");
            
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.useHandTracking"/>.</summary>
            public static readonly GUIContent useHandTracking = EditorGUIUtility.TrTextContent("Use Hand Tracking", "Enable hand tracking mode for grab movement. When enabled, hand gestures will trigger grab movement instead of controller buttons.");
            
            /// <summary><see cref="GUIContent"/> for <see cref="EnhancedGrabMoveProvider.handAdapter"/>.</summary>
            public static readonly GUIContent handAdapter = EditorGUIUtility.TrTextContent("Hand Adapter", "Simple Hand Grab Adapter that provides hand transforms and gesture state. Required when Use Hand Tracking is enabled.");
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected new void OnEnable()
        {
            base.OnEnable();
            
            // Find the serialized properties for the movement space and head reference
            m_MovementSpace = serializedObject.FindProperty("m_MovementSpace");
            m_HeadTransform = serializedObject.FindProperty("m_HeadTransform");
            
            // Find the serialized properties for the local movement controls
            m_EnableLocalXMovement = serializedObject.FindProperty("m_EnableLocalXMovement");
            m_EnableLocalYMovement = serializedObject.FindProperty("m_EnableLocalYMovement");
            m_EnableLocalZMovement = serializedObject.FindProperty("m_EnableLocalZMovement");
            
            // Find the serialized properties for the flip parameters
            m_FlipXMovement = serializedObject.FindProperty("m_FlipXMovement");
            m_FlipYMovement = serializedObject.FindProperty("m_FlipYMovement");
            m_FlipZMovement = serializedObject.FindProperty("m_FlipZMovement");
            
            // Find the serialized properties for hand tracking support
            m_UseHandTracking = serializedObject.FindProperty("m_UseHandTracking");
            m_HandAdapter = serializedObject.FindProperty("m_HandAdapter");
        }

        /// <summary>
        /// This method is automatically called by <see cref="DrawInspector"/> to
        /// draw the property fields. Override this method to add the local movement controls
        /// and flip properties while maintaining the existing inspector layout.
        /// </summary>
        protected override void DrawProperties()
        {
            // Draw all the base properties first
            base.DrawProperties();
            
            // Add a separator for the movement space section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement Space", EditorStyles.boldLabel);
            
            // Draw the movement space dropdown
            EditorGUILayout.PropertyField(m_MovementSpace, Contents.movementSpace);
            
            var currentMovementSpace = (MovementSpace)m_MovementSpace.enumValueIndex;
            
            // Show head transform field only in Head mode
            if (currentMovementSpace == MovementSpace.Head)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_HeadTransform, Contents.headTransform);
                EditorGUI.indentLevel--;
            }
            
            // Show local axis controls only in Local and Head modes
            if (currentMovementSpace == MovementSpace.Local || currentMovementSpace == MovementSpace.Head)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Axis Controls", EditorStyles.boldLabel);
                
                EditorGUILayout.PropertyField(m_EnableLocalXMovement, Contents.enableLocalXMovement);
                EditorGUILayout.PropertyField(m_EnableLocalYMovement, Contents.enableLocalYMovement);
                EditorGUILayout.PropertyField(m_EnableLocalZMovement, Contents.enableLocalZMovement);
            }
            
            // Show helpful info for Global mode
            if (currentMovementSpace == MovementSpace.Global)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Global mode uses the world space constraints from the base GrabMoveProvider (Enable Free X/Y/Z Movement above).", MessageType.Info);
            }
            
            // Movement flip controls are always visible
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Movement Direction", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(m_FlipXMovement, Contents.flipXMovement);
            EditorGUILayout.PropertyField(m_FlipYMovement, Contents.flipYMovement);
            EditorGUILayout.PropertyField(m_FlipZMovement, Contents.flipZMovement);
            
            // Hand tracking support section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Hand Tracking Support", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(m_UseHandTracking, Contents.useHandTracking);
            
            if (m_UseHandTracking.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_HandAdapter, Contents.handAdapter);
                
                if (m_HandAdapter.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("Hand Adapter is required when Use Hand Tracking is enabled. Please assign a SimpleHandGrabAdapter component.", MessageType.Warning);
                }
                
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox("Setup Instructions:\n1. Add SimpleHandGrabAdapter component\n2. Assign left/right hand transforms from XR Hands\n3. Connect StaticHandGesture events:\n   • gesturePerformed → OnLeftGrabStarted/OnRightGrabStarted\n   • gestureEnded → OnLeftGrabEnded/OnRightGrabEnded", MessageType.Info);
                EditorGUI.indentLevel--;
            }
        }
    }
}