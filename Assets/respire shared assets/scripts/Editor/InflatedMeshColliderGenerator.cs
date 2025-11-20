using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using AutoLOD.MeshDecimator;

namespace RespireSharedAssets.Editor
{
    public enum ColliderGenerationMethod
    {
        CageMesh,
        Voxelization
    }
    
    [System.Serializable]
    public class InflatedMeshColliderGenerator : EditorWindow
    {
        [Header("Collider Generation Method")]
        [SerializeField] private ColliderGenerationMethod generationMethod = ColliderGenerationMethod.Voxelization;
        
        [Header("Cage Settings")]
        [SerializeField] private float cageThickness = 0.05f;
        [SerializeField] private bool createSolidCage = true;
        
        [Header("Voxelization Settings")]
        [SerializeField] private float voxelSize = 0.1f;
        [SerializeField] private float voxelPadding = 0.05f;
        [SerializeField] private bool optimizeVoxelMesh = true;
        
        [Header("Optimization")]
        [SerializeField] private bool useMeshDecimation = true;
        [SerializeField] private int targetFaceCount = 1000;
        [SerializeField] private bool createMeshCollider = true;
        [SerializeField] private bool saveMeshAsAsset = false;
        [SerializeField] private string meshAssetPath = "Assets/Generated Meshes/";
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        private GameObject selectedGameObject;
        private Mesh generatedMesh;
        private CFastMeshDecimator meshDecimator;
        
        [MenuItem("Tools/Respire/Inflated Mesh Collider Generator")]
        public static void ShowWindow()
        {
            GetWindow<InflatedMeshColliderGenerator>("Inflated Mesh Collider Generator");
        }
        
        private void OnEnable()
        {
            // Initialize the mesh decimator
            if (meshDecimator == null)
            {
                meshDecimator = new CFastMeshDecimator();
                meshDecimator.Initialize();
            }
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Inflated Mesh Collider Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            // Target GameObject selection
            EditorGUILayout.BeginHorizontal();
            selectedGameObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", selectedGameObject, typeof(GameObject), true);
            if (GUILayout.Button("Use Selected", GUILayout.Width(100)))
            {
                if (Selection.activeGameObject != null)
                {
                    selectedGameObject = Selection.activeGameObject;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Settings
            generationMethod = (ColliderGenerationMethod)EditorGUILayout.EnumPopup("Generation Method", generationMethod);
            
            GUILayout.Space(5);
            
            if (generationMethod == ColliderGenerationMethod.CageMesh)
            {
                cageThickness = EditorGUILayout.FloatField("Cage Thickness", cageThickness);
                createSolidCage = EditorGUILayout.Toggle("Create Solid Cage", createSolidCage);
            }
            else if (generationMethod == ColliderGenerationMethod.Voxelization)
            {
                voxelSize = EditorGUILayout.FloatField("Voxel Size", voxelSize);
                voxelPadding = EditorGUILayout.FloatField("Voxel Padding", voxelPadding);
                optimizeVoxelMesh = EditorGUILayout.Toggle("Optimize Voxel Mesh", optimizeVoxelMesh);
            }
            
            GUILayout.Space(5);
            useMeshDecimation = EditorGUILayout.Toggle("Use Mesh Decimation", useMeshDecimation);
            
            GUI.enabled = useMeshDecimation;
            targetFaceCount = EditorGUILayout.IntField("Target Face Count", targetFaceCount);
            GUI.enabled = true;
            
            createMeshCollider = EditorGUILayout.Toggle("Create Mesh Collider", createMeshCollider);
            saveMeshAsAsset = EditorGUILayout.Toggle("Save Mesh as Asset", saveMeshAsAsset);
            
            if (saveMeshAsAsset)
            {
                meshAssetPath = EditorGUILayout.TextField("Mesh Asset Path", meshAssetPath);
            }
            
            showDebugInfo = EditorGUILayout.Toggle("Show Debug Info", showDebugInfo);
            
            GUILayout.Space(20);
            
            // Generate button
            GUI.enabled = selectedGameObject != null;
            if (GUILayout.Button("Generate Inflated Collider Mesh", GUILayout.Height(30)))
            {
                GenerateInflatedColliderMesh();
            }
            GUI.enabled = true;
            
            // Debug info
            if (showDebugInfo && selectedGameObject != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Debug Information", EditorStyles.boldLabel);
                
                var meshRenderers = selectedGameObject.GetComponentsInChildren<MeshRenderer>();
                EditorGUILayout.LabelField("Mesh Renderers Found", meshRenderers.Length.ToString());
                
                int totalVertices = 0;
                int totalTriangles = 0;
                int validMeshCount = 0;
                foreach (var renderer in meshRenderers)
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    var meshCollider = renderer.GetComponent<MeshCollider>();
                    
                    // Only count objects that have both MeshFilter and MeshCollider components
                    if (meshFilter != null && meshFilter.sharedMesh != null && meshCollider != null)
                    {
                        totalVertices += meshFilter.sharedMesh.vertexCount;
                        totalTriangles += meshFilter.sharedMesh.triangles.Length / 3;
                        validMeshCount++;
                    }
                }
                
                EditorGUILayout.LabelField("Valid Meshes (with MeshCollider)", validMeshCount.ToString());
                
                EditorGUILayout.LabelField("Total Vertices", totalVertices.ToString());
                EditorGUILayout.LabelField("Total Triangles", totalTriangles.ToString());
                
                if (generatedMesh != null)
                {
                    EditorGUILayout.LabelField("Generated Mesh Vertices", generatedMesh.vertexCount.ToString());
                    EditorGUILayout.LabelField("Generated Mesh Triangles", (generatedMesh.triangles.Length / 3).ToString());
                }
            }
        }
        
        private void GenerateInflatedColliderMesh()
        {
            if (selectedGameObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a GameObject first.", "OK");
                return;
            }
            
            try
            {
                // Find all mesh renderers in children
                var meshRenderers = selectedGameObject.GetComponentsInChildren<MeshRenderer>();
                
                if (meshRenderers.Length == 0)
                {
                    EditorUtility.DisplayDialog("Error", "No mesh renderers found in the selected GameObject or its children.", "OK");
                    return;
                }
                
                Mesh combinedMesh;
                
                if (generationMethod == ColliderGenerationMethod.Voxelization)
                {
                    // Use voxelization method
                    combinedMesh = CreateVoxelizedMesh(meshRenderers);
                }
                else
                {
                    // Use cage mesh method
                    List<CombineInstance> combineInstances = new List<CombineInstance>();
                    
                    foreach (var renderer in meshRenderers)
                    {
                        var meshFilter = renderer.GetComponent<MeshFilter>();
                        var meshCollider = renderer.GetComponent<MeshCollider>();
                        
                        // Only process objects that have both MeshFilter and MeshCollider components
                        if (meshFilter == null || meshFilter.sharedMesh == null || meshCollider == null) continue;
                        
                        // Create cage mesh
                        Mesh cageMesh = CreateCageMesh(meshFilter.sharedMesh, cageThickness, createSolidCage);
                        
                        // Create combine instance
                        CombineInstance combine = new CombineInstance();
                        combine.mesh = cageMesh;
                        combine.transform = selectedGameObject.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                        combineInstances.Add(combine);
                    }
                    
                    if (combineInstances.Count == 0)
                    {
                        EditorUtility.DisplayDialog("Error", "No valid meshes found to combine.", "OK");
                        return;
                    }
                    
                    // Combine all cage meshes
                    combinedMesh = new Mesh();
                    combinedMesh.name = $"{selectedGameObject.name}_CageCombined";
                    combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support more than 65535 vertices
                    combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
                    
                    // Clean up temporary meshes
                    foreach (var combine in combineInstances)
                    {
                        if (combine.mesh != null)
                        {
                            DestroyImmediate(combine.mesh);
                        }
                    }
                }
                
                // Recalculate bounds and normals
                combinedMesh.RecalculateBounds();
                combinedMesh.RecalculateNormals();
                
                Mesh finalMesh;
                
                // Optionally decimate the mesh using AutoLOD
                if (useMeshDecimation)
                {
                    Mesh decimatedMesh = meshDecimator.DecimateMesh(combinedMesh, targetFaceCount, false);
                    
                    if (decimatedMesh == null)
                    {
                        Debug.LogWarning("Mesh decimation failed, using original combined mesh.");
                        finalMesh = combinedMesh;
                    }
                    else
                    {
                        finalMesh = decimatedMesh;
                        // Clean up the original combined mesh if decimation succeeded
                        if (decimatedMesh != combinedMesh)
                        {
                            DestroyImmediate(combinedMesh);
                        }
                    }
                }
                else
                {
                    finalMesh = combinedMesh;
                }
                
                generatedMesh = finalMesh;
                generatedMesh.name = $"{selectedGameObject.name}_InflatedCollider";
                
                // Create mesh collider if requested
                if (createMeshCollider)
                {
                    CreateMeshCollider(finalMesh);
                }
                
                // Save mesh as asset if requested
                if (saveMeshAsAsset)
                {
                    SaveMeshAsAsset(finalMesh);
                }
                
                string methodName = generationMethod == ColliderGenerationMethod.Voxelization ? "Voxelized" : "Cage";
                EditorUtility.DisplayDialog("Success", 
                    $"Generated {methodName.ToLower()} collider mesh with {finalMesh.vertexCount} vertices and {finalMesh.triangles.Length / 3} triangles." +
                    (useMeshDecimation ? " (Decimated)" : " (Original)"), 
                    "OK");
                
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"An error occurred: {e.Message}", "OK");
                Debug.LogError($"InflatedMeshColliderGenerator Error: {e}");
            }
        }
        
        private Mesh CreateCageMesh(Mesh originalMesh, float thickness, bool createSolid)
        {
            if (!createSolid)
            {
                // Simple inflation for backward compatibility
                return InflateMesh(originalMesh, thickness);
            }
            
            // Create a solid cage mesh with thickness
            Vector3[] originalVertices = originalMesh.vertices;
            Vector3[] originalNormals = originalMesh.normals;
            int[] originalTriangles = originalMesh.triangles;
            
            // If normals don't exist, recalculate them
            if (originalNormals == null || originalNormals.Length != originalVertices.Length)
            {
                Mesh tempMesh = Object.Instantiate(originalMesh);
                tempMesh.RecalculateNormals();
                originalNormals = tempMesh.normals;
                DestroyImmediate(tempMesh);
            }
            
            // Create vertices for both inner and outer surfaces
            Vector3[] cageVertices = new Vector3[originalVertices.Length * 2];
            Vector3[] cageNormals = new Vector3[originalNormals.Length * 2];
            
            // Outer surface (inflated outward)
            for (int i = 0; i < originalVertices.Length; i++)
            {
                cageVertices[i] = originalVertices[i] + originalNormals[i] * (thickness * 0.5f);
                cageNormals[i] = originalNormals[i];
            }
            
            // Inner surface (inflated inward)
            for (int i = 0; i < originalVertices.Length; i++)
            {
                cageVertices[i + originalVertices.Length] = originalVertices[i] - originalNormals[i] * (thickness * 0.5f);
                cageNormals[i + originalVertices.Length] = -originalNormals[i]; // Flip normals for inner surface
            }
            
            // Create triangles for both surfaces
            List<int> cageTriangles = new List<int>();
            
            // Outer surface triangles (same as original)
            for (int i = 0; i < originalTriangles.Length; i++)
            {
                cageTriangles.Add(originalTriangles[i]);
            }
            
            // Inner surface triangles (flipped winding order)
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                cageTriangles.Add(originalTriangles[i + 2] + originalVertices.Length);
                cageTriangles.Add(originalTriangles[i + 1] + originalVertices.Length);
                cageTriangles.Add(originalTriangles[i] + originalVertices.Length);
            }
            
            // Create side faces to connect inner and outer surfaces
            CreateSideFaces(originalMesh, cageTriangles, originalVertices.Length);
            
            // Create the final cage mesh
            Mesh cageMesh = new Mesh();
            cageMesh.name = originalMesh.name + "_Cage";
            cageMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            cageMesh.vertices = cageVertices;
            cageMesh.triangles = cageTriangles.ToArray();
            cageMesh.normals = cageNormals;
            cageMesh.RecalculateBounds();
            
            return cageMesh;
        }
        
        private Mesh InflateMesh(Mesh originalMesh, float distance)
        {
            // Create a copy of the original mesh
            Mesh inflatedMesh = Object.Instantiate(originalMesh);
            
            Vector3[] vertices = inflatedMesh.vertices;
            Vector3[] normals = inflatedMesh.normals;
            
            // If normals don't exist, recalculate them
            if (normals == null || normals.Length != vertices.Length)
            {
                inflatedMesh.RecalculateNormals();
                normals = inflatedMesh.normals;
            }
            
            // Inflate vertices along their normals
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += normals[i] * distance;
            }
            
            inflatedMesh.vertices = vertices;
            inflatedMesh.RecalculateBounds();
            
            return inflatedMesh;
        }
        
        private void CreateSideFaces(Mesh originalMesh, List<int> triangles, int vertexCount)
        {
            // Get boundary edges of the original mesh
            HashSet<(int, int)> edges = new HashSet<(int, int)>();
            Dictionary<(int, int), int> edgeCount = new Dictionary<(int, int), int>();
            
            int[] originalTriangles = originalMesh.triangles;
            
            // Count edge occurrences to find boundary edges
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int v1 = originalTriangles[i + j];
                    int v2 = originalTriangles[i + (j + 1) % 3];
                    
                    // Ensure consistent edge ordering
                    var edge = v1 < v2 ? (v1, v2) : (v2, v1);
                    
                    if (edgeCount.ContainsKey(edge))
                        edgeCount[edge]++;
                    else
                        edgeCount[edge] = 1;
                }
            }
            
            // Boundary edges appear only once
            foreach (var kvp in edgeCount)
            {
                if (kvp.Value == 1)
                {
                    edges.Add(kvp.Key);
                }
            }
            
            // Create side faces for boundary edges
            foreach (var edge in edges)
            {
                int v1 = edge.Item1;
                int v2 = edge.Item2;
                
                // Outer edge vertices
                int outerV1 = v1;
                int outerV2 = v2;
                
                // Inner edge vertices
                int innerV1 = v1 + vertexCount;
                int innerV2 = v2 + vertexCount;
                
                // Create two triangles to form a quad
                // Triangle 1: outer1 -> inner1 -> outer2
                triangles.Add(outerV1);
                triangles.Add(innerV1);
                triangles.Add(outerV2);
                
                // Triangle 2: outer2 -> inner1 -> inner2
                triangles.Add(outerV2);
                triangles.Add(innerV1);
                triangles.Add(innerV2);
            }
        }
        
        private Mesh CreateVoxelizedMesh(MeshRenderer[] meshRenderers)
        {
            // Calculate bounding box of all valid meshes
            Bounds totalBounds = new Bounds();
            bool boundsInitialized = false;
            List<(Mesh mesh, Matrix4x4 transform)> validMeshes = new List<(Mesh, Matrix4x4)>();
            
            foreach (var renderer in meshRenderers)
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                var meshCollider = renderer.GetComponent<MeshCollider>();
                
                // Only process objects that have both MeshFilter and MeshCollider components
                if (meshFilter == null || meshFilter.sharedMesh == null || meshCollider == null) continue;
                
                Matrix4x4 transformMatrix = selectedGameObject.transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                validMeshes.Add((meshFilter.sharedMesh, transformMatrix));
                
                // Calculate transformed bounds
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3[] corners = new Vector3[8];
                corners[0] = transformMatrix.MultiplyPoint3x4(meshBounds.min);
                corners[1] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z));
                corners[2] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z));
                corners[3] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z));
                corners[4] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z));
                corners[5] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z));
                corners[6] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z));
                corners[7] = transformMatrix.MultiplyPoint3x4(meshBounds.max);
                
                if (!boundsInitialized)
                {
                    totalBounds = new Bounds(corners[0], Vector3.zero);
                    boundsInitialized = true;
                }
                
                foreach (var corner in corners)
                {
                    totalBounds.Encapsulate(corner);
                }
            }
            
            if (validMeshes.Count == 0)
            {
                Debug.LogError("No valid meshes found for voxelization.");
                return null;
            }
            
            // Expand bounds by padding
            totalBounds.Expand(voxelPadding * 2);
            
            // Calculate voxel grid dimensions
            Vector3 gridSize = totalBounds.size;
            int voxelsX = Mathf.CeilToInt(gridSize.x / voxelSize);
            int voxelsY = Mathf.CeilToInt(gridSize.y / voxelSize);
            int voxelsZ = Mathf.CeilToInt(gridSize.z / voxelSize);
            
            // Create voxel grid
            bool[,,] voxelGrid = new bool[voxelsX, voxelsY, voxelsZ];
            Vector3 minBounds = totalBounds.min;
            
            // Fill voxel grid
            for (int x = 0; x < voxelsX; x++)
            {
                for (int y = 0; y < voxelsY; y++)
                {
                    for (int z = 0; z < voxelsZ; z++)
                    {
                        Vector3 voxelCenter = minBounds + new Vector3(
                            (x + 0.5f) * voxelSize,
                            (y + 0.5f) * voxelSize,
                            (z + 0.5f) * voxelSize
                        );
                        
                        // Check if voxel center is inside any mesh
                        foreach (var meshData in validMeshes)
                        {
                            if (IsPointInsideMesh(voxelCenter, meshData.mesh, meshData.transform))
                            {
                                voxelGrid[x, y, z] = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            // Generate mesh from voxel grid
            return GenerateMeshFromVoxels(voxelGrid, voxelsX, voxelsY, voxelsZ, minBounds, voxelSize);
        }
        
        private bool IsPointInsideMesh(Vector3 point, Mesh mesh, Matrix4x4 transformMatrix)
        {
            // Transform point to mesh local space
            Vector3 localPoint = transformMatrix.inverse.MultiplyPoint3x4(point);
            
            // Simple bounds check first
            if (!mesh.bounds.Contains(localPoint))
                return false;
            
            // Ray casting method - cast ray from point and count intersections
            Vector3 rayDirection = Vector3.right; // Use consistent direction
            int intersectionCount = 0;
            
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];
                
                if (RayTriangleIntersect(localPoint, rayDirection, v1, v2, v3))
                {
                    intersectionCount++;
                }
            }
            
            // Odd number of intersections means point is inside
            return (intersectionCount % 2) == 1;
        }
        
        private bool RayTriangleIntersect(Vector3 rayOrigin, Vector3 rayDirection, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            const float EPSILON = 0.0000001f;
            
            Vector3 edge1 = v2 - v1;
            Vector3 edge2 = v3 - v1;
            Vector3 h = Vector3.Cross(rayDirection, edge2);
            float a = Vector3.Dot(edge1, h);
            
            if (a > -EPSILON && a < EPSILON)
                return false;
            
            float f = 1.0f / a;
            Vector3 s = rayOrigin - v1;
            float u = f * Vector3.Dot(s, h);
            
            if (u < 0.0f || u > 1.0f)
                return false;
            
            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(rayDirection, q);
            
            if (v < 0.0f || u + v > 1.0f)
                return false;
            
            float t = f * Vector3.Dot(edge2, q);
            return t > EPSILON; // Ray intersection
        }
        
        private Mesh GenerateMeshFromVoxels(bool[,,] voxelGrid, int sizeX, int sizeY, int sizeZ, Vector3 origin, float voxelSize)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            
            // Generate box for each occupied voxel
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (!voxelGrid[x, y, z]) continue;
                        
                        // Skip interior voxels if optimization is enabled
                        if (optimizeVoxelMesh && IsInteriorVoxel(voxelGrid, x, y, z, sizeX, sizeY, sizeZ))
                            continue;
                        
                        Vector3 voxelPos = origin + new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
                        AddVoxelFaces(vertices, triangles, voxelGrid, x, y, z, sizeX, sizeY, sizeZ, voxelPos, voxelSize);
                    }
                }
            }
            
            Mesh voxelMesh = new Mesh();
            voxelMesh.name = "VoxelizedMesh";
            voxelMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            voxelMesh.vertices = vertices.ToArray();
            voxelMesh.triangles = triangles.ToArray();
            voxelMesh.RecalculateNormals();
            voxelMesh.RecalculateBounds();
            
            return voxelMesh;
        }
        
        private bool IsInteriorVoxel(bool[,,] grid, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
        {
            // Check if all 6 neighboring voxels are occupied
            if (x > 0 && x < sizeX - 1 && y > 0 && y < sizeY - 1 && z > 0 && z < sizeZ - 1)
            {
                return grid[x - 1, y, z] && grid[x + 1, y, z] &&
                       grid[x, y - 1, z] && grid[x, y + 1, z] &&
                       grid[x, y, z - 1] && grid[x, y, z + 1];
            }
            return false;
        }
        
        private void AddVoxelFaces(List<Vector3> vertices, List<int> triangles, bool[,,] grid, 
            int x, int y, int z, int sizeX, int sizeY, int sizeZ, Vector3 pos, float size)
        {
            // Only add faces that are exposed (not facing another voxel)
            Vector3[] faceVertices = new Vector3[8];
            
            // Calculate voxel corners
            faceVertices[0] = pos;                                    // 0,0,0
            faceVertices[1] = pos + new Vector3(size, 0, 0);         // 1,0,0
            faceVertices[2] = pos + new Vector3(size, size, 0);      // 1,1,0
            faceVertices[3] = pos + new Vector3(0, size, 0);         // 0,1,0
            faceVertices[4] = pos + new Vector3(0, 0, size);         // 0,0,1
            faceVertices[5] = pos + new Vector3(size, 0, size);      // 1,0,1
            faceVertices[6] = pos + new Vector3(size, size, size);   // 1,1,1
            faceVertices[7] = pos + new Vector3(0, size, size);      // 0,1,1
            
            // Check each face and add if exposed
            // Front face (-Z)
            if (z == 0 || !grid[x, y, z - 1])
                AddQuad(vertices, triangles, faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3]);
            
            // Back face (+Z)
            if (z == sizeZ - 1 || !grid[x, y, z + 1])
                AddQuad(vertices, triangles, faceVertices[5], faceVertices[4], faceVertices[7], faceVertices[6]);
            
            // Left face (-X)
            if (x == 0 || !grid[x - 1, y, z])
                AddQuad(vertices, triangles, faceVertices[4], faceVertices[0], faceVertices[3], faceVertices[7]);
            
            // Right face (+X)
            if (x == sizeX - 1 || !grid[x + 1, y, z])
                AddQuad(vertices, triangles, faceVertices[1], faceVertices[5], faceVertices[6], faceVertices[2]);
            
            // Bottom face (-Y)
            if (y == 0 || !grid[x, y - 1, z])
                AddQuad(vertices, triangles, faceVertices[4], faceVertices[5], faceVertices[1], faceVertices[0]);
            
            // Top face (+Y)
            if (y == sizeY - 1 || !grid[x, y + 1, z])
                AddQuad(vertices, triangles, faceVertices[3], faceVertices[2], faceVertices[6], faceVertices[7]);
        }
        
        private void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            int startIndex = vertices.Count;
            vertices.AddRange(new Vector3[] { v1, v2, v3, v4 });
            
            // Add two triangles to form a quad
            triangles.AddRange(new int[] {
                startIndex, startIndex + 1, startIndex + 2,
                startIndex, startIndex + 2, startIndex + 3
            });
        }
        
        private void CreateMeshCollider(Mesh mesh)
        {
            // Check if mesh collider already exists
            MeshCollider existingCollider = selectedGameObject.GetComponent<MeshCollider>();
            
            if (existingCollider != null)
            {
                // Update existing collider
                Undo.RecordObject(existingCollider, "Update Inflated Mesh Collider");
                existingCollider.sharedMesh = mesh;
                existingCollider.convex = false; // Set to false for complex collision detection
            }
            else
            {
                // Create new mesh collider
                MeshCollider newCollider = Undo.AddComponent<MeshCollider>(selectedGameObject);
                newCollider.sharedMesh = mesh;
                newCollider.convex = false;
            }
            
            EditorUtility.SetDirty(selectedGameObject);
        }
        
        private void SaveMeshAsAsset(Mesh mesh)
        {
            // Ensure directory exists
            if (!System.IO.Directory.Exists(meshAssetPath))
            {
                System.IO.Directory.CreateDirectory(meshAssetPath);
            }
            
            string assetPath = $"{meshAssetPath}{mesh.name}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
            
            // Create mesh asset
            AssetDatabase.CreateAsset(mesh, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            Debug.Log($"Mesh saved as asset: {assetPath}");
        }
        
        private void OnDestroy()
        {
            // Clean up
            if (generatedMesh != null)
            {
                DestroyImmediate(generatedMesh);
            }
        }
    }
    
    // Component version for runtime use
    [System.Serializable]
    public class InflatedMeshColliderComponent : MonoBehaviour
    {
        [Header("Collider Generation Method")]
        [SerializeField] private ColliderGenerationMethod generationMethod = ColliderGenerationMethod.Voxelization;
        
        [Header("Cage Settings")]
        [SerializeField] private float cageThickness = 0.05f;
        [SerializeField] private bool createSolidCage = true;
        
        [Header("Voxelization Settings")]
        [SerializeField] private float voxelSize = 0.1f;
        [SerializeField] private float voxelPadding = 0.05f;
        [SerializeField] private bool optimizeVoxelMesh = true;
        
        [Header("Optimization")]
        [SerializeField] private bool useMeshDecimation = true;
        [SerializeField] private int targetFaceCount = 1000;
        [SerializeField] private bool generateOnStart = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = false;
        
        private Mesh generatedColliderMesh;
        private CFastMeshDecimator meshDecimator;
        
        private void Start()
        {
            if (generateOnStart)
            {
                GenerateColliderMesh();
            }
        }
        
        [ContextMenu("Generate Collider Mesh")]
        public void GenerateColliderMesh()
        {
            // Initialize decimator
            if (meshDecimator == null)
            {
                meshDecimator = new CFastMeshDecimator();
                meshDecimator.Initialize();
            }
            
            // Find all mesh renderers in children
            var meshRenderers = GetComponentsInChildren<MeshRenderer>();
            
            if (meshRenderers.Length == 0)
            {
                Debug.LogWarning("No mesh renderers found in children.");
                return;
            }
            
            Mesh combinedMesh;
            
            if (generationMethod == ColliderGenerationMethod.Voxelization)
            {
                // Use voxelization method
                combinedMesh = CreateVoxelizedMesh(meshRenderers);
                if (combinedMesh == null)
                {
                    Debug.LogError("Failed to create voxelized mesh.");
                    return;
                }
            }
            else
            {
                // Use cage mesh method
                List<CombineInstance> combineInstances = new List<CombineInstance>();
                
                foreach (var renderer in meshRenderers)
                {
                    var meshFilter = renderer.GetComponent<MeshFilter>();
                    var childMeshCollider = renderer.GetComponent<MeshCollider>();
                    
                    // Only process objects that have both MeshFilter and MeshCollider components
                    if (meshFilter == null || meshFilter.sharedMesh == null || childMeshCollider == null) continue;
                    
                    // Create cage mesh
                    Mesh cageMesh = CreateCageMesh(meshFilter.sharedMesh, cageThickness, createSolidCage);
                    
                    // Create combine instance
                    CombineInstance combine = new CombineInstance();
                    combine.mesh = cageMesh;
                    combine.transform = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                    combineInstances.Add(combine);
                }
                
                if (combineInstances.Count == 0)
                {
                    Debug.LogWarning("No valid meshes found to combine.");
                    return;
                }
                
                // Combine all cage meshes
                combinedMesh = new Mesh();
                combinedMesh.name = $"{gameObject.name}_CageCombined";
                combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Support more than 65535 vertices
                combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);
                
                // Clean up temporary meshes
                foreach (var combine in combineInstances)
                {
                    if (combine.mesh != null)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(combine.mesh);
                        }
                        else
                        {
                            DestroyImmediate(combine.mesh);
                        }
                    }
                }
            }
            
            // Recalculate bounds and normals
            combinedMesh.RecalculateBounds();
            combinedMesh.RecalculateNormals();
            
            Mesh finalMesh;
            
            // Optionally decimate the mesh using AutoLOD
            if (useMeshDecimation)
            {
                Mesh decimatedMesh = meshDecimator.DecimateMesh(combinedMesh, targetFaceCount, false);
                
                if (decimatedMesh == null)
                {
                    Debug.LogWarning("Mesh decimation failed, using original combined mesh.");
                    finalMesh = combinedMesh;
                }
                else
                {
                    finalMesh = decimatedMesh;
                    // Clean up the original combined mesh if decimation succeeded
                    if (decimatedMesh != combinedMesh)
                    {
                        if (Application.isPlaying)
                        {
                            Destroy(combinedMesh);
                        }
                        else
                        {
                            DestroyImmediate(combinedMesh);
                        }
                    }
                }
            }
            else
            {
                finalMesh = combinedMesh;
            }
            
            // Clean up old mesh
            if (generatedColliderMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(generatedColliderMesh);
                }
                else
                {
                    DestroyImmediate(generatedColliderMesh);
                }
            }
            
            generatedColliderMesh = finalMesh;
            generatedColliderMesh.name = $"{gameObject.name}_InflatedCollider";
            
            // Create or update mesh collider
            MeshCollider meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }
            
            meshCollider.sharedMesh = generatedColliderMesh;
            meshCollider.convex = false;
            
            string methodName = generationMethod == ColliderGenerationMethod.Voxelization ? "voxelized" : "cage";
            Debug.Log($"Generated {methodName} collider mesh with {finalMesh.vertexCount} vertices and {finalMesh.triangles.Length / 3} triangles." +
                     (useMeshDecimation ? " (Decimated)" : " (Original)"));
        }
        
        private Mesh CreateCageMesh(Mesh originalMesh, float thickness, bool createSolid)
        {
            if (!createSolid)
            {
                // Simple inflation for backward compatibility
                return InflateMesh(originalMesh, thickness);
            }
            
            // Create a solid cage mesh with thickness
            Vector3[] originalVertices = originalMesh.vertices;
            Vector3[] originalNormals = originalMesh.normals;
            int[] originalTriangles = originalMesh.triangles;
            
            // If normals don't exist, recalculate them
            if (originalNormals == null || originalNormals.Length != originalVertices.Length)
            {
                Mesh tempMesh = Instantiate(originalMesh);
                tempMesh.RecalculateNormals();
                originalNormals = tempMesh.normals;
                if (Application.isPlaying)
                {
                    Destroy(tempMesh);
                }
                else
                {
                    DestroyImmediate(tempMesh);
                }
            }
            
            // Create vertices for both inner and outer surfaces
            Vector3[] cageVertices = new Vector3[originalVertices.Length * 2];
            Vector3[] cageNormals = new Vector3[originalNormals.Length * 2];
            
            // Outer surface (inflated outward)
            for (int i = 0; i < originalVertices.Length; i++)
            {
                cageVertices[i] = originalVertices[i] + originalNormals[i] * (thickness * 0.5f);
                cageNormals[i] = originalNormals[i];
            }
            
            // Inner surface (inflated inward)
            for (int i = 0; i < originalVertices.Length; i++)
            {
                cageVertices[i + originalVertices.Length] = originalVertices[i] - originalNormals[i] * (thickness * 0.5f);
                cageNormals[i + originalVertices.Length] = -originalNormals[i]; // Flip normals for inner surface
            }
            
            // Create triangles for both surfaces
            List<int> cageTriangles = new List<int>();
            
            // Outer surface triangles (same as original)
            for (int i = 0; i < originalTriangles.Length; i++)
            {
                cageTriangles.Add(originalTriangles[i]);
            }
            
            // Inner surface triangles (flipped winding order)
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                cageTriangles.Add(originalTriangles[i + 2] + originalVertices.Length);
                cageTriangles.Add(originalTriangles[i + 1] + originalVertices.Length);
                cageTriangles.Add(originalTriangles[i] + originalVertices.Length);
            }
            
            // Create side faces to connect inner and outer surfaces
            CreateSideFaces(originalMesh, cageTriangles, originalVertices.Length);
            
            // Create the final cage mesh
            Mesh cageMesh = new Mesh();
            cageMesh.name = originalMesh.name + "_Cage";
            cageMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            cageMesh.vertices = cageVertices;
            cageMesh.triangles = cageTriangles.ToArray();
            cageMesh.normals = cageNormals;
            cageMesh.RecalculateBounds();
            
            return cageMesh;
        }
        
        private Mesh InflateMesh(Mesh originalMesh, float distance)
        {
            // Create a copy of the original mesh
            Mesh inflatedMesh = Instantiate(originalMesh);
            
            Vector3[] vertices = inflatedMesh.vertices;
            Vector3[] normals = inflatedMesh.normals;
            
            // If normals don't exist, recalculate them
            if (normals == null || normals.Length != vertices.Length)
            {
                inflatedMesh.RecalculateNormals();
                normals = inflatedMesh.normals;
            }
            
            // Inflate vertices along their normals
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] += normals[i] * distance;
            }
            
            inflatedMesh.vertices = vertices;
            inflatedMesh.RecalculateBounds();
            
            return inflatedMesh;
        }
        
        private void CreateSideFaces(Mesh originalMesh, List<int> triangles, int vertexCount)
        {
            // Get boundary edges of the original mesh
            HashSet<(int, int)> edges = new HashSet<(int, int)>();
            Dictionary<(int, int), int> edgeCount = new Dictionary<(int, int), int>();
            
            int[] originalTriangles = originalMesh.triangles;
            
            // Count edge occurrences to find boundary edges
            for (int i = 0; i < originalTriangles.Length; i += 3)
            {
                for (int j = 0; j < 3; j++)
                {
                    int v1 = originalTriangles[i + j];
                    int v2 = originalTriangles[i + (j + 1) % 3];
                    
                    // Ensure consistent edge ordering
                    var edge = v1 < v2 ? (v1, v2) : (v2, v1);
                    
                    if (edgeCount.ContainsKey(edge))
                        edgeCount[edge]++;
                    else
                        edgeCount[edge] = 1;
                }
            }
            
            // Boundary edges appear only once
            foreach (var kvp in edgeCount)
            {
                if (kvp.Value == 1)
                {
                    edges.Add(kvp.Key);
                }
            }
            
            // Create side faces for boundary edges
            foreach (var edge in edges)
            {
                int v1 = edge.Item1;
                int v2 = edge.Item2;
                
                // Outer edge vertices
                int outerV1 = v1;
                int outerV2 = v2;
                
                // Inner edge vertices
                int innerV1 = v1 + vertexCount;
                int innerV2 = v2 + vertexCount;
                
                // Create two triangles to form a quad
                // Triangle 1: outer1 -> inner1 -> outer2
                triangles.Add(outerV1);
                triangles.Add(innerV1);
                triangles.Add(outerV2);
                
                // Triangle 2: outer2 -> inner1 -> inner2
                triangles.Add(outerV2);
                triangles.Add(innerV1);
                triangles.Add(innerV2);
            }
        }
        
        private Mesh CreateVoxelizedMesh(MeshRenderer[] meshRenderers)
        {
            // Calculate bounding box of all valid meshes
            Bounds totalBounds = new Bounds();
            bool boundsInitialized = false;
            List<(Mesh mesh, Matrix4x4 transform)> validMeshes = new List<(Mesh, Matrix4x4)>();
            
            foreach (var renderer in meshRenderers)
            {
                var meshFilter = renderer.GetComponent<MeshFilter>();
                var meshCollider = renderer.GetComponent<MeshCollider>();
                
                // Only process objects that have both MeshFilter and MeshCollider components
                if (meshFilter == null || meshFilter.sharedMesh == null || meshCollider == null) continue;
                
                Matrix4x4 transformMatrix = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                validMeshes.Add((meshFilter.sharedMesh, transformMatrix));
                
                // Calculate transformed bounds
                Bounds meshBounds = meshFilter.sharedMesh.bounds;
                Vector3[] corners = new Vector3[8];
                corners[0] = transformMatrix.MultiplyPoint3x4(meshBounds.min);
                corners[1] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.min.x, meshBounds.min.y, meshBounds.max.z));
                corners[2] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.min.z));
                corners[3] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.min.z));
                corners[4] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.min.x, meshBounds.max.y, meshBounds.max.z));
                corners[5] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.max.x, meshBounds.min.y, meshBounds.max.z));
                corners[6] = transformMatrix.MultiplyPoint3x4(new Vector3(meshBounds.max.x, meshBounds.max.y, meshBounds.min.z));
                corners[7] = transformMatrix.MultiplyPoint3x4(meshBounds.max);
                
                if (!boundsInitialized)
                {
                    totalBounds = new Bounds(corners[0], Vector3.zero);
                    boundsInitialized = true;
                }
                
                foreach (var corner in corners)
                {
                    totalBounds.Encapsulate(corner);
                }
            }
            
            if (validMeshes.Count == 0)
            {
                Debug.LogError("No valid meshes found for voxelization.");
                return null;
            }
            
            // Expand bounds by padding
            totalBounds.Expand(voxelPadding * 2);
            
            // Calculate voxel grid dimensions
            Vector3 gridSize = totalBounds.size;
            int voxelsX = Mathf.CeilToInt(gridSize.x / voxelSize);
            int voxelsY = Mathf.CeilToInt(gridSize.y / voxelSize);
            int voxelsZ = Mathf.CeilToInt(gridSize.z / voxelSize);
            
            // Create voxel grid
            bool[,,] voxelGrid = new bool[voxelsX, voxelsY, voxelsZ];
            Vector3 minBounds = totalBounds.min;
            
            // Fill voxel grid
            for (int x = 0; x < voxelsX; x++)
            {
                for (int y = 0; y < voxelsY; y++)
                {
                    for (int z = 0; z < voxelsZ; z++)
                    {
                        Vector3 voxelCenter = minBounds + new Vector3(
                            (x + 0.5f) * voxelSize,
                            (y + 0.5f) * voxelSize,
                            (z + 0.5f) * voxelSize
                        );
                        
                        // Check if voxel center is inside any mesh
                        foreach (var meshData in validMeshes)
                        {
                            if (IsPointInsideMesh(voxelCenter, meshData.mesh, meshData.transform))
                            {
                                voxelGrid[x, y, z] = true;
                                break;
                            }
                        }
                    }
                }
            }
            
            // Generate mesh from voxel grid
            return GenerateMeshFromVoxels(voxelGrid, voxelsX, voxelsY, voxelsZ, minBounds, voxelSize);
        }
        
        private bool IsPointInsideMesh(Vector3 point, Mesh mesh, Matrix4x4 transformMatrix)
        {
            // Transform point to mesh local space
            Vector3 localPoint = transformMatrix.inverse.MultiplyPoint3x4(point);
            
            // Simple bounds check first
            if (!mesh.bounds.Contains(localPoint))
                return false;
            
            // Ray casting method - cast ray from point and count intersections
            Vector3 rayDirection = Vector3.right; // Use consistent direction
            int intersectionCount = 0;
            
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];
                
                if (RayTriangleIntersect(localPoint, rayDirection, v1, v2, v3))
                {
                    intersectionCount++;
                }
            }
            
            // Odd number of intersections means point is inside
            return (intersectionCount % 2) == 1;
        }
        
        private bool RayTriangleIntersect(Vector3 rayOrigin, Vector3 rayDirection, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            const float EPSILON = 0.0000001f;
            
            Vector3 edge1 = v2 - v1;
            Vector3 edge2 = v3 - v1;
            Vector3 h = Vector3.Cross(rayDirection, edge2);
            float a = Vector3.Dot(edge1, h);
            
            if (a > -EPSILON && a < EPSILON)
                return false;
            
            float f = 1.0f / a;
            Vector3 s = rayOrigin - v1;
            float u = f * Vector3.Dot(s, h);
            
            if (u < 0.0f || u > 1.0f)
                return false;
            
            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(rayDirection, q);
            
            if (v < 0.0f || u + v > 1.0f)
                return false;
            
            float t = f * Vector3.Dot(edge2, q);
            return t > EPSILON; // Ray intersection
        }
        
        private Mesh GenerateMeshFromVoxels(bool[,,] voxelGrid, int sizeX, int sizeY, int sizeZ, Vector3 origin, float voxelSize)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            
            // Generate box for each occupied voxel
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        if (!voxelGrid[x, y, z]) continue;
                        
                        // Skip interior voxels if optimization is enabled
                        if (optimizeVoxelMesh && IsInteriorVoxel(voxelGrid, x, y, z, sizeX, sizeY, sizeZ))
                            continue;
                        
                        Vector3 voxelPos = origin + new Vector3(x * voxelSize, y * voxelSize, z * voxelSize);
                        AddVoxelFaces(vertices, triangles, voxelGrid, x, y, z, sizeX, sizeY, sizeZ, voxelPos, voxelSize);
                    }
                }
            }
            
            Mesh voxelMesh = new Mesh();
            voxelMesh.name = "VoxelizedMesh";
            voxelMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            voxelMesh.vertices = vertices.ToArray();
            voxelMesh.triangles = triangles.ToArray();
            voxelMesh.RecalculateNormals();
            voxelMesh.RecalculateBounds();
            
            return voxelMesh;
        }
        
        private bool IsInteriorVoxel(bool[,,] grid, int x, int y, int z, int sizeX, int sizeY, int sizeZ)
        {
            // Check if all 6 neighboring voxels are occupied
            if (x > 0 && x < sizeX - 1 && y > 0 && y < sizeY - 1 && z > 0 && z < sizeZ - 1)
            {
                return grid[x - 1, y, z] && grid[x + 1, y, z] &&
                       grid[x, y - 1, z] && grid[x, y + 1, z] &&
                       grid[x, y, z - 1] && grid[x, y, z + 1];
            }
            return false;
        }
        
        private void AddVoxelFaces(List<Vector3> vertices, List<int> triangles, bool[,,] grid, 
            int x, int y, int z, int sizeX, int sizeY, int sizeZ, Vector3 pos, float size)
        {
            // Only add faces that are exposed (not facing another voxel)
            Vector3[] faceVertices = new Vector3[8];
            
            // Calculate voxel corners
            faceVertices[0] = pos;                                    // 0,0,0
            faceVertices[1] = pos + new Vector3(size, 0, 0);         // 1,0,0
            faceVertices[2] = pos + new Vector3(size, size, 0);      // 1,1,0
            faceVertices[3] = pos + new Vector3(0, size, 0);         // 0,1,0
            faceVertices[4] = pos + new Vector3(0, 0, size);         // 0,0,1
            faceVertices[5] = pos + new Vector3(size, 0, size);      // 1,0,1
            faceVertices[6] = pos + new Vector3(size, size, size);   // 1,1,1
            faceVertices[7] = pos + new Vector3(0, size, size);      // 0,1,1
            
            // Check each face and add if exposed
            // Front face (-Z)
            if (z == 0 || !grid[x, y, z - 1])
                AddQuad(vertices, triangles, faceVertices[0], faceVertices[1], faceVertices[2], faceVertices[3]);
            
            // Back face (+Z)
            if (z == sizeZ - 1 || !grid[x, y, z + 1])
                AddQuad(vertices, triangles, faceVertices[5], faceVertices[4], faceVertices[7], faceVertices[6]);
            
            // Left face (-X)
            if (x == 0 || !grid[x - 1, y, z])
                AddQuad(vertices, triangles, faceVertices[4], faceVertices[0], faceVertices[3], faceVertices[7]);
            
            // Right face (+X)
            if (x == sizeX - 1 || !grid[x + 1, y, z])
                AddQuad(vertices, triangles, faceVertices[1], faceVertices[5], faceVertices[6], faceVertices[2]);
            
            // Bottom face (-Y)
            if (y == 0 || !grid[x, y - 1, z])
                AddQuad(vertices, triangles, faceVertices[4], faceVertices[5], faceVertices[1], faceVertices[0]);
            
            // Top face (+Y)
            if (y == sizeY - 1 || !grid[x, y + 1, z])
                AddQuad(vertices, triangles, faceVertices[3], faceVertices[2], faceVertices[6], faceVertices[7]);
        }
        
        private void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            int startIndex = vertices.Count;
            vertices.AddRange(new Vector3[] { v1, v2, v3, v4 });
            
            // Add two triangles to form a quad
            triangles.AddRange(new int[] {
                startIndex, startIndex + 1, startIndex + 2,
                startIndex, startIndex + 2, startIndex + 3
            });
        }
        
        private void OnDrawGizmosSelected()
        {
            if (showDebugGizmos && generatedColliderMesh != null)
            {
                Gizmos.color = Color.green;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireMesh(generatedColliderMesh);
            }
        }
        
        private void OnDestroy()
        {
            // Clean up
            if (generatedColliderMesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(generatedColliderMesh);
                }
                else
                {
                    DestroyImmediate(generatedColliderMesh);
                }
            }
        }
    }
} 