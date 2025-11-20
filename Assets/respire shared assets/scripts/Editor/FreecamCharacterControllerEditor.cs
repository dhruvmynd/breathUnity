using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FreecamCharacterController))]
[CanEditMultipleObjects]
public class FreecamCharacterControllerEditor : Editor
{
    // Base Character Controller Properties
    private SerializedProperty rb;
    private SerializedProperty enableGravity;
    private SerializedProperty moveForce;
    private SerializedProperty maxSpeed;
    
    // Bounce Settings
    private SerializedProperty bounceForce;
    private SerializedProperty bounceType;
    private SerializedProperty bounceOffCameraLook;
    private SerializedProperty bounceHorizontalMultiplier;
    private SerializedProperty exitBounceMinSpeed;
    private SerializedProperty minimumBounceTime;
    private SerializedProperty allowContinuousBounce;
    
    // Collision Settings
    private SerializedProperty enableCollisionBounce;
    
    // Ground Check Settings
    private SerializedProperty groundLayer;
    private SerializedProperty groundCheckDistance;
    
    // Freecam Specific Properties
    private SerializedProperty moveSpeed;
    private SerializedProperty sprintMultiplier;
    private SerializedProperty slowMultiplier;
    private SerializedProperty mouseSensitivityX;
    private SerializedProperty mouseSensitivityY;
    private SerializedProperty invertY;
    private SerializedProperty sprintKey;
    private SerializedProperty slowKey;
    private SerializedProperty upKey;
    private SerializedProperty downKey;
    private SerializedProperty enableMouseMovement;
    private SerializedProperty toggleMouseKey;
    private SerializedProperty useCameraForward;
    
    // Debug
    private SerializedProperty showDebug;

    void OnEnable()
    {
        // Base Character Controller Properties
        rb = serializedObject.FindProperty("rb");
        enableGravity = serializedObject.FindProperty("enableGravity");
        moveForce = serializedObject.FindProperty("moveForce");
        maxSpeed = serializedObject.FindProperty("maxSpeed");
        
        // Bounce Settings
        bounceForce = serializedObject.FindProperty("bounceForce");
        bounceType = serializedObject.FindProperty("bounceType");
        bounceOffCameraLook = serializedObject.FindProperty("bounceOffCameraLook");
        bounceHorizontalMultiplier = serializedObject.FindProperty("bounceHorizontalMultiplier");
        exitBounceMinSpeed = serializedObject.FindProperty("exitBounceMinSpeed");
        minimumBounceTime = serializedObject.FindProperty("minimumBounceTime");
        allowContinuousBounce = serializedObject.FindProperty("allowContinuousBounce");
        
        // Collision Settings
        enableCollisionBounce = serializedObject.FindProperty("enableCollisionBounce");
        
        // Ground Check Settings
        groundLayer = serializedObject.FindProperty("groundLayer");
        groundCheckDistance = serializedObject.FindProperty("groundCheckDistance");
        
        // Freecam Specific Properties
        moveSpeed = serializedObject.FindProperty("moveSpeed");
        sprintMultiplier = serializedObject.FindProperty("sprintMultiplier");
        slowMultiplier = serializedObject.FindProperty("slowMultiplier");
        mouseSensitivityX = serializedObject.FindProperty("mouseSensitivityX");
        mouseSensitivityY = serializedObject.FindProperty("mouseSensitivityY");
        invertY = serializedObject.FindProperty("invertY");
        sprintKey = serializedObject.FindProperty("sprintKey");
        slowKey = serializedObject.FindProperty("slowKey");
        upKey = serializedObject.FindProperty("upKey");
        downKey = serializedObject.FindProperty("downKey");
        enableMouseMovement = serializedObject.FindProperty("enableMouseMovement");
        toggleMouseKey = serializedObject.FindProperty("toggleMouseKey");
        useCameraForward = serializedObject.FindProperty("useCameraForward");
        
        // Debug
        showDebug = serializedObject.FindProperty("showDebug");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        FreecamCharacterController freecam = (FreecamCharacterController)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Freecam Character Controller for smooth flying/floating movement with WASD + mouse controls.", MessageType.Info);
        EditorGUILayout.Space();
        
        // Components
        EditorGUILayout.LabelField("Primary Components", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(rb);
        
        if (rb.objectReferenceValue == null)
        {
            if (GUILayout.Button("Auto-Assign Rigidbody"))
            {
                freecam.gameObject.TryGetComponent(out Rigidbody foundRb);
                if (foundRb != null) rb.objectReferenceValue = foundRb;
                else EditorUtility.DisplayDialog("Missing Component", "No Rigidbody component found. Please add one.", "OK");
            }
        }
        
        EditorGUILayout.Space();
        
        // Physics
        EditorGUILayout.LabelField("Physics", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableGravity);
        EditorGUILayout.PropertyField(moveForce);
        EditorGUILayout.PropertyField(maxSpeed);
        
        EditorGUILayout.Space();
        
        // Freecam Movement
        EditorGUILayout.LabelField("Freecam Movement", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(moveSpeed);
        EditorGUILayout.PropertyField(sprintMultiplier);
        EditorGUILayout.PropertyField(slowMultiplier);
        EditorGUILayout.PropertyField(useCameraForward);
        
        EditorGUILayout.Space();
        
        // Mouse Controls
        EditorGUILayout.LabelField("Mouse Controls", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableMouseMovement);
        EditorGUILayout.PropertyField(mouseSensitivityX);
        EditorGUILayout.PropertyField(mouseSensitivityY);
        EditorGUILayout.PropertyField(invertY);
        EditorGUILayout.PropertyField(toggleMouseKey);
        
        EditorGUILayout.Space();
        
        // Input Keys
        EditorGUILayout.LabelField("Input Keys", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(sprintKey);
        EditorGUILayout.PropertyField(slowKey);
        EditorGUILayout.PropertyField(upKey);
        EditorGUILayout.PropertyField(downKey);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space();
        
        // Bounce Settings
        EditorGUILayout.LabelField("Bounce Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bounceForce);
        EditorGUILayout.PropertyField(bounceType);
        EditorGUILayout.PropertyField(bounceOffCameraLook);
        EditorGUILayout.PropertyField(bounceHorizontalMultiplier);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bounce State Control", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(exitBounceMinSpeed);
        EditorGUILayout.PropertyField(minimumBounceTime);
        EditorGUILayout.PropertyField(allowContinuousBounce);
        
        EditorGUILayout.Space();
        
        // Collision
        EditorGUILayout.LabelField("Collision", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(enableCollisionBounce);
        
        EditorGUILayout.Space();
        
        // Ground Check
        EditorGUILayout.LabelField("Ground Check", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(groundLayer);
        EditorGUILayout.PropertyField(groundCheckDistance);
        
        EditorGUILayout.Space();
        
        // Debug
        EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(showDebug);
        
        EditorGUILayout.Space();
        
        // Runtime Information
        if (Application.isPlaying)
        {
            EditorGUILayout.LabelField("Runtime Information", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
            
            if (freecam.Rigidbody != null)
            {
                EditorGUILayout.LabelField($"Movement State: {freecam.CurrentMovementState}");
                EditorGUILayout.LabelField($"Can Accept Input: {freecam.CanAcceptInput}");
                EditorGUILayout.LabelField($"Velocity: {freecam.GetSpeed():F2}");
                EditorGUILayout.LabelField($"Is Grounded: {freecam.IsGrounded()}");
                EditorGUILayout.LabelField($"Mouse Movement: {(freecam.EnableMouseMovement ? "Enabled" : "Disabled")}");
            }
            
            EditorGUILayout.EndVertical();
            if (showDebug.boolValue) Repaint();
        }
        
        serializedObject.ApplyModifiedProperties();
    }
    
    [MenuItem("GameObject/Respire/Freecam Character Controller", false, 1)]
    static void CreateFreecamCharacterController()
    {
        GameObject go = new GameObject("Freecam Character Controller");
        go.AddComponent<CapsuleCollider>();
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.useGravity = false; // Freecam typically doesn't use gravity
        go.AddComponent<FreecamCharacterController>();
        
        Selection.activeGameObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Create Freecam Character Controller");
    }
} 