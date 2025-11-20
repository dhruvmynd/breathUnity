using UnityEngine;
using TMPro;

/// <summary>
/// Quest Score Setup - Helper script to quickly set up score display for Quest 3
/// Creates a standalone score display that works well with Quest Link
/// </summary>
public class QuestScoreSetup : MonoBehaviour
{
    [Header("Setup Options")]
    [Tooltip("Position for the score display in world space")]
    public Vector3 displayPosition = new Vector3(0, 2, 3);
    
    [Tooltip("Scale for the score display")]
    public Vector3 displayScale = new Vector3(1, 1, 1);
    
    [Tooltip("Automatically setup when this component starts")]
    public bool autoSetup = true;
    
    [Header("Display Configuration")]
    [Tooltip("Font size for the main score")]
    public float scoreFontSize = 72f;
    
    [Tooltip("Font size for feedback text")]
    public float feedbackFontSize = 24f;
    
    [Tooltip("Font size for session info")]
    public float sessionFontSize = 18f;
    
    [Tooltip("How often to update the display (in seconds)")]
    public float updateInterval = 0.5f;
    
    [Header("Colors")]
    [Tooltip("Color for excellent scores (85-100)")]
    public Color excellentColor = Color.green;
    
    [Tooltip("Color for good scores (70-84)")]
    public Color goodColor = Color.yellow;
    
    [Tooltip("Color for poor scores (0-69)")]
    public Color poorColor = Color.red;
    
    [Tooltip("Color when no data is available")]
    public Color noDataColor = Color.gray;
    
    private QuestScoreDisplay scoreDisplay;
    
    void Start()
    {
        if (autoSetup)
        {
            SetupScoreDisplay();
        }
    }
    
    [ContextMenu("Setup Score Display")]
    public void SetupScoreDisplay()
    {
        // Create the main score display object
        GameObject scoreDisplayObj = new GameObject("QuestScoreDisplay");
        scoreDisplayObj.transform.position = displayPosition;
        scoreDisplayObj.transform.localScale = displayScale;
        
        // Add the QuestScoreDisplay component
        scoreDisplay = scoreDisplayObj.AddComponent<QuestScoreDisplay>();
        
        // Configure the display settings
        scoreDisplay.scoreFontSize = scoreFontSize;
        scoreDisplay.feedbackFontSize = feedbackFontSize;
        scoreDisplay.sessionFontSize = sessionFontSize;
        scoreDisplay.updateInterval = updateInterval;
        scoreDisplay.excellentColor = excellentColor;
        scoreDisplay.goodColor = goodColor;
        scoreDisplay.poorColor = poorColor;
        scoreDisplay.noDataColor = noDataColor;
        
        // Create a simple background for better visibility
        CreateBackground(scoreDisplayObj);
        
        // Create a frame for better visual separation
        CreateFrame(scoreDisplayObj);
        
        Debug.Log("🎯 Quest Score Display setup complete!");
        Debug.Log($"📍 Position: {displayPosition}");
        Debug.Log($"📏 Scale: {displayScale}");
        Debug.Log($"🎨 Font Sizes - Score: {scoreFontSize}, Feedback: {feedbackFontSize}, Session: {sessionFontSize}");
    }
    
    void CreateBackground(GameObject parent)
    {
        // Create a simple background panel
        GameObject background = GameObject.CreatePrimitive(PrimitiveType.Cube);
        background.name = "ScoreBackground";
        background.transform.SetParent(parent.transform);
        background.transform.localPosition = new Vector3(0, -0.5f, -0.1f);
        background.transform.localScale = new Vector3(4, 3, 0.1f);
        
        // Make it semi-transparent
        Renderer renderer = background.GetComponent<Renderer>();
        Material bgMaterial = new Material(Shader.Find("Standard"));
        bgMaterial.color = new Color(0, 0, 0, 0.7f); // Semi-transparent black
        bgMaterial.SetFloat("_Mode", 3); // Transparent mode
        bgMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bgMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bgMaterial.SetInt("_ZWrite", 0);
        bgMaterial.DisableKeyword("_ALPHATEST_ON");
        bgMaterial.EnableKeyword("_ALPHABLEND_ON");
        bgMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        bgMaterial.renderQueue = 3000;
        renderer.material = bgMaterial;
        
        // Remove collider
        DestroyImmediate(background.GetComponent<Collider>());
    }
    
    void CreateFrame(GameObject parent)
    {
        // Create a simple frame around the score
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "ScoreFrame";
        frame.transform.SetParent(parent.transform);
        frame.transform.localPosition = new Vector3(0, -0.5f, -0.05f);
        frame.transform.localScale = new Vector3(4.2f, 3.2f, 0.05f);
        
        // Make it a thin frame
        Renderer renderer = frame.GetComponent<Renderer>();
        Material frameMaterial = new Material(Shader.Find("Standard"));
        frameMaterial.color = Color.white;
        renderer.material = frameMaterial;
        
        // Remove collider
        DestroyImmediate(frame.GetComponent<Collider>());
    }
    
    [ContextMenu("Update Display Settings")]
    public void UpdateDisplaySettings()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.scoreFontSize = scoreFontSize;
            scoreDisplay.feedbackFontSize = feedbackFontSize;
            scoreDisplay.sessionFontSize = sessionFontSize;
            scoreDisplay.updateInterval = updateInterval;
            scoreDisplay.excellentColor = excellentColor;
            scoreDisplay.goodColor = goodColor;
            scoreDisplay.poorColor = poorColor;
            scoreDisplay.noDataColor = noDataColor;
            
            Debug.Log("🎯 Display settings updated!");
        }
        else
        {
            Debug.LogWarning("🎯 No score display found. Run Setup Score Display first.");
        }
    }
    
    [ContextMenu("Test Score Display")]
    public void TestScoreDisplay()
    {
        if (scoreDisplay != null)
        {
            scoreDisplay.TestExcellentScore();
            Debug.Log("🧪 Testing score display...");
        }
        else
        {
            Debug.LogWarning("🎯 No score display found. Run Setup Score Display first.");
        }
    }
    
    [ContextMenu("Remove Score Display")]
    public void RemoveScoreDisplay()
    {
        if (scoreDisplay != null)
        {
            DestroyImmediate(scoreDisplay.gameObject);
            scoreDisplay = null;
            Debug.Log("🗑️ Score display removed.");
        }
        else
        {
            Debug.Log("🎯 No score display to remove.");
        }
    }
}
