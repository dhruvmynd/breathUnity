using UnityEngine;
using UnityEditor;

public class PrefabScatterer : EditorWindow
{
    private Transform centerTransform;
    private GameObject prefab;
    private int count = 10;
    private float radius = 5f;
    private float innerRadius = 0f;
    private float height = 10f;
    private bool useCylinder = false;
    private bool surfaceOnly = true;
    private bool useInnerRadius = false;
    private Vector2 horizontalArcRange = new Vector2(0f, 360f);
    private Vector2 verticalArcRange = new Vector2(0f, 180f);
    private Vector2 heightRange = new Vector2(-1f, 1f);
    private bool randomizeRotation = true;
    private bool randomizeScale = false;
    private bool lockScaleAxes = true;
    private Vector3 minScale = Vector3.one * 0.8f;
    private Vector3 maxScale = Vector3.one * 1.2f;
    private bool showPreview = true;
    private Color previewColor = new Color(0.2f, 0.7f, 1f, 0.3f);
    private Color innerPreviewColor = new Color(1f, 0.5f, 0.2f, 0.3f);
    private int previewDensity = 32;

    [MenuItem("Tools/Prefab Scatterer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabScatterer>("Prefab Scatterer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Scattering Settings", EditorStyles.boldLabel);

        centerTransform = EditorGUILayout.ObjectField("Center Transform", centerTransform, typeof(Transform), true) as Transform;
        prefab = EditorGUILayout.ObjectField("Prefab to Scatter", prefab, typeof(GameObject), false) as GameObject;
        count = EditorGUILayout.IntField("Number to Spawn", count);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Shape Settings", EditorStyles.boldLabel);
        useCylinder = EditorGUILayout.Toggle("Use Cylinder Shape", useCylinder);
        radius = EditorGUILayout.FloatField("Outer Radius", radius);
        
        useInnerRadius = EditorGUILayout.Toggle("Use Inner Radius", useInnerRadius);
        if (useInnerRadius)
        {
            EditorGUI.indentLevel++;
            innerRadius = EditorGUILayout.Slider("Inner Radius", innerRadius, 0f, radius - 0.1f);
            EditorGUI.indentLevel--;
        }
        
        surfaceOnly = EditorGUILayout.Toggle("Outer Surface Only", surfaceOnly);
        
        if (useCylinder)
        {
            height = EditorGUILayout.FloatField("Cylinder Height", height);
            EditorGUILayout.LabelField("Height Range (-1 to 1, normalized)");
            heightRange = EditorGUILayout.Vector2Field("", heightRange);
            heightRange.x = Mathf.Clamp(heightRange.x, -1f, 1f);
            heightRange.y = Mathf.Clamp(heightRange.y, -1f, 1f);
        }
        else
        {
            EditorGUILayout.LabelField("Vertical Arc Range (0-180°)");
            verticalArcRange = EditorGUILayout.Vector2Field("", verticalArcRange);
            verticalArcRange.x = Mathf.Clamp(verticalArcRange.x, 0f, 180f);
            verticalArcRange.y = Mathf.Clamp(verticalArcRange.y, 0f, 180f);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Horizontal Arc Range (0-360°)");
        horizontalArcRange = EditorGUILayout.Vector2Field("", horizontalArcRange);
        horizontalArcRange.x = Mathf.Clamp(horizontalArcRange.x, 0f, 360f);
        horizontalArcRange.y = Mathf.Clamp(horizontalArcRange.y, 0f, 360f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Instance Settings", EditorStyles.boldLabel);
        randomizeRotation = EditorGUILayout.Toggle("Randomize Rotation", randomizeRotation);
        
        randomizeScale = EditorGUILayout.Toggle("Randomize Scale", randomizeScale);
        if (randomizeScale)
        {
            EditorGUI.indentLevel++;
            lockScaleAxes = EditorGUILayout.Toggle("Lock Scale Axes (Uniform)", lockScaleAxes);
            
            if (lockScaleAxes)
            {
                float minUniform = minScale.x;
                float maxUniform = maxScale.x;
                
                EditorGUI.BeginChangeCheck();
                minUniform = EditorGUILayout.FloatField("Min Scale", minUniform);
                maxUniform = EditorGUILayout.FloatField("Max Scale", maxUniform);
                
                if (EditorGUI.EndChangeCheck())
                {
                    minScale = Vector3.one * minUniform;
                    maxScale = Vector3.one * maxUniform;
                }
            }
            else
            {
                minScale = EditorGUILayout.Vector3Field("Min Scale", minScale);
                maxScale = EditorGUILayout.Vector3Field("Max Scale", maxScale);
            }
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
        showPreview = EditorGUILayout.Toggle("Show Preview", showPreview);
        if (showPreview)
        {
            EditorGUI.indentLevel++;
            previewColor = EditorGUILayout.ColorField("Outer Preview Color", previewColor);
            if (useInnerRadius)
            {
                innerPreviewColor = EditorGUILayout.ColorField("Inner Preview Color", innerPreviewColor);
            }
            previewDensity = EditorGUILayout.IntSlider("Preview Density", previewDensity, 8, 64);
            EditorGUI.indentLevel--;
            
            if (centerTransform != null)
            {
                SceneView.RepaintAll();
            }
        }

        EditorGUILayout.Space();
        GUI.enabled = centerTransform != null && prefab != null && count > 0 && radius > 0;
        if (GUILayout.Button("Scatter Prefabs"))
        {
            ScatterPrefabs();
        }
        GUI.enabled = true;
    }
    
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!showPreview || centerTransform == null)
            return;
            
        Handles.matrix = Matrix4x4.TRS(
            centerTransform.position, 
            centerTransform.rotation, 
            Vector3.one
        );
        
        if (useCylinder)
        {
            // Draw outer cylinder
            Handles.color = previewColor;
            DrawCylinderPreview(radius);
            
            // Draw inner cylinder if needed
            if (useInnerRadius && innerRadius > 0)
            {
                Handles.color = innerPreviewColor;
                DrawCylinderPreview(innerRadius);
            }
        }
        else
        {
            // Draw outer sphere
            Handles.color = previewColor;
            DrawSpherePreview(radius);
            
            // Draw inner sphere if needed
            if (useInnerRadius && innerRadius > 0)
            {
                Handles.color = innerPreviewColor;
                DrawSpherePreview(innerRadius);
            }
        }
    }
    
    private void DrawSpherePreview(float sphereRadius)
    {
        float hStart = horizontalArcRange.x * Mathf.Deg2Rad;
        float hEnd = horizontalArcRange.y * Mathf.Deg2Rad;
        float vStart = verticalArcRange.x * Mathf.Deg2Rad;
        float vEnd = verticalArcRange.y * Mathf.Deg2Rad;
        
        // Horizontal circles at different vertical angles
        int vSteps = previewDensity / 4;
        for (int i = 0; i <= vSteps; i++)
        {
            float vAngle = Mathf.Lerp(vStart, vEnd, i / (float)vSteps);
            float localY = sphereRadius * Mathf.Cos(vAngle);
            float localRadius = sphereRadius * Mathf.Sin(vAngle);
            
            Vector3 center = new Vector3(0, localY, 0);
            Vector3 startDir = new Vector3(Mathf.Cos(hStart), 0, Mathf.Sin(hStart));
            Vector3 endDir = new Vector3(Mathf.Cos(hEnd), 0, Mathf.Sin(hEnd));
            
            Handles.DrawWireArc(center, Vector3.up, startDir, (hEnd - hStart) * Mathf.Rad2Deg, localRadius);
        }
        
        // Vertical arcs at different horizontal angles
        int hSteps = previewDensity / 4;
        for (int i = 0; i <= hSteps; i++)
        {
            float hAngle = Mathf.Lerp(hStart, hEnd, i / (float)hSteps);
            float dirX = Mathf.Cos(hAngle);
            float dirZ = Mathf.Sin(hAngle);
            
            Vector3 start = new Vector3(
                sphereRadius * Mathf.Sin(vStart) * dirX,
                sphereRadius * Mathf.Cos(vStart),
                sphereRadius * Mathf.Sin(vStart) * dirZ
            );
            
            Vector3 end = new Vector3(
                sphereRadius * Mathf.Sin(vEnd) * dirX,
                sphereRadius * Mathf.Cos(vEnd),
                sphereRadius * Mathf.Sin(vEnd) * dirZ
            );
            
            Handles.DrawLine(start, end);
        }
        
        // Draw some internal points if showing volume points
        if (!surfaceOnly && showPreview && sphereRadius == radius)
        {
            Color pointColor = new Color(previewColor.r, previewColor.g, previewColor.b, previewColor.a * 0.5f);
            Handles.color = pointColor;
            int pointCount = previewDensity / 2;
            
            for (int i = 0; i < pointCount; i++)
            {
                // Get a random point inside the sphere section
                Vector3 point = GetRandomSpherePoint(true);
                
                // Draw a small marker
                float pointSize = radius * 0.02f;
                Handles.DrawWireCube(point, Vector3.one * pointSize);
            }
        }
    }
    
    private void DrawCylinderPreview(float cylinderRadius)
    {
        float hStart = horizontalArcRange.x * Mathf.Deg2Rad;
        float hEnd = horizontalArcRange.y * Mathf.Deg2Rad;
        float halfHeight = height / 2f;
        float minY = heightRange.x * halfHeight;
        float maxY = heightRange.y * halfHeight;
        
        // Draw arcs at top and bottom
        Vector3 bottomCenter = new Vector3(0, minY, 0);
        Vector3 topCenter = new Vector3(0, maxY, 0);
        Vector3 startDir = new Vector3(Mathf.Cos(hStart), 0, Mathf.Sin(hStart));
        
        Handles.DrawWireArc(bottomCenter, Vector3.up, startDir, (hEnd - hStart) * Mathf.Rad2Deg, cylinderRadius);
        Handles.DrawWireArc(topCenter, Vector3.up, startDir, (hEnd - hStart) * Mathf.Rad2Deg, cylinderRadius);
        
        // Draw vertical lines
        int steps = previewDensity / 4;
        for (int i = 0; i <= steps; i++)
        {
            float angle = Mathf.Lerp(hStart, hEnd, i / (float)steps);
            float x = cylinderRadius * Mathf.Cos(angle);
            float z = cylinderRadius * Mathf.Sin(angle);
            
            Vector3 bottom = new Vector3(x, minY, z);
            Vector3 top = new Vector3(x, maxY, z);
            
            Handles.DrawLine(bottom, top);
        }
        
        // Draw some internal points if showing volume points
        if (!surfaceOnly && showPreview && cylinderRadius == radius)
        {
            Color pointColor = new Color(previewColor.r, previewColor.g, previewColor.b, previewColor.a * 0.5f);
            Handles.color = pointColor;
            int pointCount = previewDensity / 2;
            
            for (int i = 0; i < pointCount; i++)
            {
                // Get a random point inside the cylinder section
                Vector3 point = GetRandomCylinderPoint(true);
                
                // Draw a small marker
                float pointSize = radius * 0.02f;
                Handles.DrawWireCube(point, Vector3.one * pointSize);
            }
        }
    }

    private void ScatterPrefabs()
    {
        Undo.SetCurrentGroupName("Scatter Prefabs");
        int undoGroupIndex = Undo.GetCurrentGroup();

        GameObject parent = new GameObject(prefab.name + " Group");
        Undo.RegisterCreatedObjectUndo(parent, "Create Parent Object");
        parent.transform.position = centerTransform.position;
        parent.transform.rotation = centerTransform.rotation;

        for (int i = 0; i < count; i++)
        {
            Vector3 position;
            
            if (useCylinder)
            {
                position = GetCylinderPoint();
            }
            else
            {
                position = GetSpherePoint();
            }
            
            // Create the prefab instance
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Undo.RegisterCreatedObjectUndo(instance, "Create Prefab Instance");
            
            // Set parent and position
            instance.transform.SetParent(parent.transform);
            instance.transform.localPosition = position;
            
            // Randomize rotation if enabled
            if (randomizeRotation)
            {
                instance.transform.rotation = Random.rotation;
            }
            
            // Randomize scale if enabled
            if (randomizeScale)
            {
                Vector3 scale;
                
                if (lockScaleAxes)
                {
                    float uniformScale = Random.Range(minScale.x, maxScale.x);
                    scale = Vector3.one * uniformScale;
                }
                else
                {
                    scale = new Vector3(
                        Random.Range(minScale.x, maxScale.x),
                        Random.Range(minScale.y, maxScale.y),
                        Random.Range(minScale.z, maxScale.z)
                    );
                }
                
                instance.transform.localScale = scale;
            }
        }

        Undo.CollapseUndoOperations(undoGroupIndex);
        Selection.activeGameObject = parent;
    }
    
    private Vector3 GetSpherePoint()
    {
        return GetRandomSpherePoint(false);
    }
    
    private Vector3 GetRandomSpherePoint(bool forPreview)
    {
        float minR = useInnerRadius ? innerRadius : 0f;
        float maxR = radius;
        
        float currentRadius;
        
        if (surfaceOnly)
        {
            // Place on outer surface only
            currentRadius = maxR;
        }
        else if (useInnerRadius)
        {
            // Place between inner and outer radius
            // For uniform distribution in a sphere shell, we need this formula
            float minCubed = Mathf.Pow(minR, 3);
            float maxCubed = Mathf.Pow(maxR, 3);
            float randomCubed = Mathf.Lerp(minCubed, maxCubed, Random.value);
            currentRadius = Mathf.Pow(randomCubed, 1f/3f);
        }
        else
        {
            // Place throughout the volume of the sphere
            currentRadius = maxR * Mathf.Pow(Random.value, 1f/3f);
        }
        
        // For preview, use a few fixed divisions between inner and outer
        if (forPreview && useInnerRadius)
        {
            float t = Random.Range(0, 3) / 2f; // 0, 0.5, or 1
            currentRadius = Mathf.Lerp(minR, maxR, t);
        }
        
        // Calculate random point on sphere with arc constraints
        float horizontalAngle = Random.Range(horizontalArcRange.x, horizontalArcRange.y) * Mathf.Deg2Rad;
        float verticalAngle = Random.Range(verticalArcRange.x, verticalArcRange.y) * Mathf.Deg2Rad;
        
        // Convert from spherical to cartesian coordinates
        float y = currentRadius * Mathf.Cos(verticalAngle);
        float r = currentRadius * Mathf.Sin(verticalAngle);
        float x = r * Mathf.Cos(horizontalAngle);
        float z = r * Mathf.Sin(horizontalAngle);
        
        return new Vector3(x, y, z);
    }
    
    private Vector3 GetCylinderPoint()
    {
        return GetRandomCylinderPoint(false);
    }
    
    private Vector3 GetRandomCylinderPoint(bool forPreview)
    {
        float minR = useInnerRadius ? innerRadius : 0f;
        float maxR = radius;
        
        float currentRadius;
        
        if (surfaceOnly)
        {
            // Place on outer surface only
            currentRadius = maxR;
        }
        else if (useInnerRadius)
        {
            // Place between inner and outer radius
            // For uniform distribution in a cylinder, we need square root
            float minSquared = minR * minR;
            float maxSquared = maxR * maxR;
            float randomSquared = Mathf.Lerp(minSquared, maxSquared, Random.value);
            currentRadius = Mathf.Sqrt(randomSquared);
        }
        else
        {
            // Place throughout the volume of the cylinder
            currentRadius = maxR * Mathf.Sqrt(Random.value);
        }
        
        // For preview, use a few fixed divisions between inner and outer
        if (forPreview && useInnerRadius)
        {
            float t = Random.Range(0, 3) / 2f; // 0, 0.5, or 1
            currentRadius = Mathf.Lerp(minR, maxR, t);
        }
        
        // Calculate random angle within horizontal arc range
        float horizontalAngle = Random.Range(horizontalArcRange.x, horizontalArcRange.y) * Mathf.Deg2Rad;
        
        // Calculate random height within height range
        float normalizedHeight = Random.Range(heightRange.x, heightRange.y);
        float y = normalizedHeight * (height / 2f);
        
        // Calculate x and z based on the angle
        float x = currentRadius * Mathf.Cos(horizontalAngle);
        float z = currentRadius * Mathf.Sin(horizontalAngle);
        
        return new Vector3(x, y, z);
    }
} 