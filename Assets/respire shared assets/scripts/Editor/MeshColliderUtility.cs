using UnityEngine;
using UnityEditor;

/// <summary>
/// Unity Editor utility for managing mesh colliders in child objects.
/// Checks all mesh collider components in children and either removes them or assigns meshes from mesh renderers.
/// </summary>
public class MeshColliderUtility
{
    [MenuItem("Tools/Mesh Collider Utility/Process Mesh Colliders in Children")]
    private static void ProcessMeshCollidersInChildren()
    {
        GameObject selectedObject = Selection.activeGameObject;
        
        if (selectedObject == null)
        {
            EditorUtility.DisplayDialog("No Object Selected", "Please select a GameObject in the hierarchy before running this tool.", "OK");
            return;
        }

        ProcessMeshColliders(selectedObject);
    }

    [MenuItem("Tools/Mesh Collider Utility/Process Mesh Colliders in Children", true)]
    private static bool ValidateProcessMeshCollidersInChildren()
    {
        return Selection.activeGameObject != null;
    }

    /// <summary>
    /// Processes all mesh colliders in the children of the specified GameObject.
    /// </summary>
    /// <param name="parentObject">The parent GameObject to process</param>
    private static void ProcessMeshColliders(GameObject parentObject)
    {
        // Get all MeshCollider components in children (including the parent itself)
        MeshCollider[] meshColliders = parentObject.GetComponentsInChildren<MeshCollider>();
        
        int processedCount = 0;
        int skippedCount = 0;
        int assignedCount = 0;

        // Record the operation for undo
        Undo.SetCurrentGroupName("Process Mesh Colliders");
        int undoGroup = Undo.GetCurrentGroup();

        foreach (MeshCollider meshCollider in meshColliders)
        {
            if (meshCollider == null) continue;

            GameObject childObject = meshCollider.gameObject;
            MeshRenderer meshRenderer = childObject.GetComponent<MeshRenderer>();
            SkinnedMeshRenderer skinnedMeshRenderer = childObject.GetComponent<SkinnedMeshRenderer>();
            MeshFilter meshFilter = childObject.GetComponent<MeshFilter>();

            processedCount++;

            // Check if we have either a MeshRenderer with MeshFilter or a SkinnedMeshRenderer
            bool hasMeshRenderer = meshRenderer != null && meshFilter != null;
            bool hasSkinnedMeshRenderer = skinnedMeshRenderer != null;

            if (!hasMeshRenderer && !hasSkinnedMeshRenderer)
            {
                // No mesh renderer or skinned mesh renderer found, skip this mesh collider
                skippedCount++;
                Debug.Log($"Skipped MeshCollider on '{childObject.name}' (no MeshRenderer/MeshFilter or SkinnedMeshRenderer found)");
            }
            else
            {
                // Get the appropriate mesh
                Mesh mesh = null;
                string meshSource = "";

                if (hasMeshRenderer)
                {
                    mesh = meshFilter.sharedMesh;
                    meshSource = "MeshRenderer";
                }
                else if (hasSkinnedMeshRenderer)
                {
                    mesh = skinnedMeshRenderer.sharedMesh;
                    meshSource = "SkinnedMeshRenderer";
                }

                if (mesh != null)
                {
                    Undo.RecordObject(meshCollider, "Assign Mesh to Collider");
                    meshCollider.sharedMesh = mesh;
                    assignedCount++;
                    Debug.Log($"Assigned mesh '{mesh.name}' from {meshSource} to MeshCollider on '{childObject.name}'");
                }
                else
                {
                    Debug.LogWarning($"{meshSource} on '{childObject.name}' has no mesh assigned");
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);

        // Display summary
        string summary = $"Mesh Collider Processing Complete:\n" +
                        $"• Total processed: {processedCount}\n" +
                        $"• Mesh colliders skipped: {skippedCount}\n" +
                        $"• Meshes assigned: {assignedCount}";
        
        EditorUtility.DisplayDialog("Processing Complete", summary, "OK");
        
        Debug.Log($"MeshColliderUtility: Processed {processedCount} mesh colliders. Skipped: {skippedCount}, Assigned: {assignedCount}");
    }
} 