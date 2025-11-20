using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TransformMotionDetector))]
public class TransformMotionDetectorEditor : Editor
{
    private SerializedProperty targetTransform;
    private SerializedProperty useWorldSpace;
    private SerializedProperty referenceTransform;
    
    private SerializedProperty motionThreshold;
    private SerializedProperty directionThreshold;
    private SerializedProperty smoothingFrames;
    
    private SerializedProperty enableDebugLogs;
    private SerializedProperty drawDebugRays;
    
    private SerializedProperty motionEvents;

    private void OnEnable()
    {
        targetTransform = serializedObject.FindProperty("targetTransform");
        useWorldSpace = serializedObject.FindProperty("useWorldSpace");
        referenceTransform = serializedObject.FindProperty("referenceTransform");
        
        motionThreshold = serializedObject.FindProperty("motionThreshold");
        directionThreshold = serializedObject.FindProperty("directionThreshold");
        smoothingFrames = serializedObject.FindProperty("smoothingFrames");
        
        enableDebugLogs = serializedObject.FindProperty("enableDebugLogs");
        drawDebugRays = serializedObject.FindProperty("drawDebugRays");
        
        motionEvents = serializedObject.FindProperty("motionEvents");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Tracking Settings
        EditorGUILayout.LabelField("Tracking Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(targetTransform, new GUIContent("Target Transform", "The transform to track for motion detection"));
        EditorGUILayout.PropertyField(useWorldSpace, new GUIContent("Use World Space", "Track in world space vs local space relative to reference transform"));
        
        if (!useWorldSpace.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(referenceTransform, new GUIContent("Reference Transform", "Transform to use as local space reference (usually camera or parent)"));
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Motion Detection Settings
        EditorGUILayout.LabelField("Motion Detection", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(motionThreshold, new GUIContent("Motion Threshold", "Minimum movement magnitude to detect any motion"));
        EditorGUILayout.PropertyField(directionThreshold, new GUIContent("Direction Threshold", "Dot product threshold for direction validation (0-1). Higher values require more precise alignment."));
        EditorGUILayout.PropertyField(smoothingFrames, new GUIContent("Smoothing Frames", "Number of frames to smooth velocity over"));
        
        EditorGUILayout.Space();
        
        // Direction Info
        EditorGUILayout.LabelField("Direction Detection", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Directions are detected using dot products with reference vectors:\n• World Space: Uses Unity's standard directions (Up, Down, Left, Right, Forward, Back)\n• Local Space: Uses reference transform's local directions", MessageType.Info);
        
        EditorGUILayout.Space();
        
        // Debug Settings
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableDebugLogs, new GUIContent("Enable Debug Logs", "Log direction enter/leave events to console"));
        EditorGUILayout.PropertyField(drawDebugRays, new GUIContent("Draw Debug Rays", "Visualize motion vectors and directions in scene view"));
        
        EditorGUILayout.Space();
        
        // Events
        EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(motionEvents, true);
        
        // Runtime Information
        if (Application.isPlaying)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
            
            TransformMotionDetector detector = (TransformMotionDetector)target;
            
            EditorGUILayout.BeginVertical("box");
            
            // Target information
            EditorGUILayout.LabelField("Target Transform:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  World Position: {detector.GetTargetWorldPosition()}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Tracked Position: {detector.GetCurrentPosition()}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Velocity: {detector.GetCurrentVelocity()}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Is Moving: {detector.IsMoving()}", EditorStyles.miniLabel);
            
            // Reference transform information (if using local space)
            if (!useWorldSpace.boolValue && referenceTransform.objectReferenceValue != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Reference Transform:", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"  World Position: {detector.GetReferenceWorldPosition()}", EditorStyles.miniLabel);
            }
            
            var activeDirections = detector.GetActiveDirections();
            if (activeDirections.Count > 0)
            {
                EditorGUILayout.LabelField($"Active Directions: {string.Join(", ", activeDirections)}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Active Directions: None", EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
            
            // Repaint constantly during play mode to show live updates
            if (detector.IsMoving())
                Repaint();
        }

        serializedObject.ApplyModifiedProperties();
    }
} 