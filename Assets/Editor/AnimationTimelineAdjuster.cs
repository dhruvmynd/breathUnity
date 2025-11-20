using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AnimationTimelineAdjuster : EditorWindow
{
    private AnimationClip sourceClip;
    private string outputFolder = "Assets/AdjustedAnimations";
    
    [MenuItem("Tools/Animation Timeline Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<AnimationTimelineAdjuster>("Animation Adjuster");
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Animation Timeline Adjuster", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        sourceClip = EditorGUILayout.ObjectField("Source Animation Clip", sourceClip, typeof(AnimationClip), false) as AnimationClip;
        outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
        
        EditorGUILayout.Space();
        
        GUI.enabled = sourceClip != null;
        if (GUILayout.Button("Process Animation"))
        {
            ProcessAnimation();
        }
        GUI.enabled = true;
    }
    
    private void ProcessAnimation()
    {
        if (sourceClip == null) return;
        
        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder(outputFolder))
        {
            string parentFolder = System.IO.Path.GetDirectoryName(outputFolder);
            string folderName = System.IO.Path.GetFileName(outputFolder);
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
        
        // Get all curves from the animation
        var bindings = AnimationUtility.GetCurveBindings(sourceClip);
        Dictionary<string, List<EditorCurveBinding>> pathBindings = new Dictionary<string, List<EditorCurveBinding>>();
        
        // Group bindings by path (object)
        foreach (var binding in bindings)
        {
            string path = binding.path;
            if (!pathBindings.ContainsKey(path))
            {
                pathBindings[path] = new List<EditorCurveBinding>();
            }
            pathBindings[path].Add(binding);
        }
        
        // Combined clip with all animations starting at frame 0
        AnimationClip combinedClip = new AnimationClip();
        combinedClip.name = sourceClip.name + "_AllAtFrame0";
        combinedClip.frameRate = sourceClip.frameRate;
        
        // Process each object's animation
        foreach (var pathBinding in pathBindings)
        {
            string path = pathBinding.Key;
            List<EditorCurveBinding> objectBindings = pathBinding.Value;
            
            // Find start frame for this object
            float firstKeyTime = float.MaxValue;
            float lastKeyTime = float.MinValue;
            
            foreach (var binding in objectBindings)
            {
                AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                if (curve.keys.Length > 0)
                {
                    firstKeyTime = Mathf.Min(firstKeyTime, curve.keys[0].time);
                    lastKeyTime = Mathf.Max(lastKeyTime, curve.keys[curve.keys.Length - 1].time);
                }
            }
            
            if (firstKeyTime == float.MaxValue) continue; // No keyframes
            
            Debug.Log($"Object: {path} - Starts at: {firstKeyTime} - Ends at: {lastKeyTime}");
            
            // Add to combined clip with offset
            foreach (var binding in objectBindings)
            {
                AnimationCurve sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
                AnimationCurve newCurve = new AnimationCurve();
                
                // Shift all keyframes to start at time 0
                foreach (var key in sourceCurve.keys)
                {
                    Keyframe newKey = new Keyframe(key.time - firstKeyTime, key.value, key.inTangent, key.outTangent);
                    newKey.inWeight = key.inWeight;
                    newKey.outWeight = key.outWeight;
                    newKey.weightedMode = key.weightedMode;
                    newCurve.AddKey(newKey);
                }
                
                AnimationUtility.SetEditorCurve(combinedClip, binding, newCurve);
            }
        }
        
        // Save combined clip
        string assetPath = $"{outputFolder}/{combinedClip.name}.anim";
        AssetDatabase.CreateAsset(combinedClip, assetPath);
        Debug.Log($"Created combined clip: {assetPath}");
        
        AssetDatabase.Refresh();
    }
}