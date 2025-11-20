using UnityEngine;
using TMPro;

/// <summary>
/// Quest Score Display - Simple standalone score display optimized for Quest 3 via Quest Link
/// Shows breathing score in large, clear text that's easily visible in VR
/// </summary>
public class QuestScoreDisplay : MonoBehaviour
{
    [Header("Quest Score Display Settings")]
    [Tooltip("Text component for displaying the score - Drag from any GameObject in the scene")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("Text component for displaying feedback (optional) - Drag from any GameObject in the scene")]
    public TextMeshProUGUI feedbackText;
    
    [Tooltip("Text component for displaying session info (optional) - Drag from any GameObject in the scene")]
    public TextMeshProUGUI sessionText;
    
    [Header("Display Settings")]
    [Tooltip("How often to update the display (in seconds)")]
    public float updateInterval = 0.5f;
    
    [Tooltip("Font size for the main score")]
    public float scoreFontSize = 72f;
    
    [Tooltip("Font size for feedback text")]
    public float feedbackFontSize = 24f;
    
    [Tooltip("Font size for session info")]
    public float sessionFontSize = 18f;
    
    [Header("Score Colors")]
    [Tooltip("Color for excellent scores (85-100)")]
    public Color excellentColor = Color.green;
    
    [Tooltip("Color for good scores (70-84)")]
    public Color goodColor = Color.yellow;
    
    [Tooltip("Color for poor scores (0-69)")]
    public Color poorColor = Color.red;
    
    [Tooltip("Color when no data is available")]
    public Color noDataColor = Color.gray;
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool showDebugInfo = true;
    
    // Internal state
    private BreathingScoreCalculator scoreCalculator;
    private UDPHeartRateReceiver udpReceiver;
    private float lastUpdateTime = 0f;
    
    // Current display values
    private float currentScore = 0f;
    private string currentFeedback = "";
    private float sessionTime = 0f;
    private int cycleCount = 0;
    
    void Start()
    {
        // Find the breathing score calculator
        scoreCalculator = FindObjectOfType<BreathingScoreCalculator>();
        if (scoreCalculator == null)
        {
            Debug.LogWarning("QuestScoreDisplay: BreathingScoreCalculator not found! Score will show 0.");
        }
        
        // Find UDP receiver for session info
        udpReceiver = FindObjectOfType<UDPHeartRateReceiver>();
        
        // Setup text components if not assigned
        SetupTextComponents();
        
        // Initialize display
        UpdateDisplay();
        
        Debug.Log("🎯 Quest Score Display initialized");
    }
    
    void SetupTextComponents()
    {
        // First, try to find existing TextMeshPro UI elements in the scene
        // Only do this if no text components are manually assigned
        if (scoreText == null && feedbackText == null && sessionText == null)
        {
            FindExistingTextComponents();
        }
        
        // If no score text is assigned, try to find one on this GameObject
        if (scoreText == null)
        {
            scoreText = GetComponent<TextMeshProUGUI>();
        }
        
        // Create score text if none exists
        if (scoreText == null)
        {
            GameObject scoreObj = new GameObject("ScoreText");
            scoreObj.transform.SetParent(transform);
            scoreObj.transform.localPosition = Vector3.zero;
            scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            Debug.Log("⚠️ Created new ScoreText GameObject - you can drag your existing TextMeshProUGUI into the scoreText field instead");
        }
        
        // Configure score text
        if (scoreText != null)
        {
            scoreText.fontSize = scoreFontSize;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.text = "Score: --";
            scoreText.color = noDataColor;
        }
        
        // Create feedback text if assigned but not found
        if (feedbackText == null && transform.childCount > 0)
        {
            GameObject feedbackObj = new GameObject("FeedbackText");
            feedbackObj.transform.SetParent(transform);
            feedbackObj.transform.localPosition = new Vector3(0, -1.5f, 0);
            feedbackText = feedbackObj.AddComponent<TextMeshProUGUI>();
            Debug.Log("⚠️ Created new FeedbackText GameObject - you can drag your existing TextMeshProUGUI into the feedbackText field instead");
            
            feedbackText.fontSize = feedbackFontSize;
            feedbackText.fontStyle = FontStyles.Normal;
            feedbackText.alignment = TextAlignmentOptions.Center;
            feedbackText.text = "";
            feedbackText.color = noDataColor;
        }
        
        // Create session text if assigned but not found
        if (sessionText == null && transform.childCount > 1)
        {
            GameObject sessionObj = new GameObject("SessionText");
            sessionObj.transform.SetParent(transform);
            sessionObj.transform.localPosition = new Vector3(0, -2.5f, 0);
            sessionText = sessionObj.AddComponent<TextMeshProUGUI>();
            Debug.Log("⚠️ Created new SessionText GameObject - you can drag your existing TextMeshProUGUI into the sessionText field instead");
            
            sessionText.fontSize = sessionFontSize;
            sessionText.fontStyle = FontStyles.Normal;
            sessionText.alignment = TextAlignmentOptions.Center;
            sessionText.text = "";
            sessionText.color = noDataColor;
        }
    }
    
    void FindExistingTextComponents()
    {
        // Find all TextMeshProUGUI components in the scene (UI version)
        TextMeshProUGUI[] allTextComponents = FindObjectsOfType<TextMeshProUGUI>();
        
        Debug.Log($"🔍 Found {allTextComponents.Length} TextMeshProUGUI components in scene");
        
        // Also check for regular TextMeshPro components
        TextMeshPro[] allTextMeshProComponents = FindObjectsOfType<TextMeshPro>();
        Debug.Log($"🔍 Found {allTextMeshProComponents.Length} TextMeshPro (3D) components in scene");
        
        // Try to identify and assign text components based on common naming patterns
        foreach (TextMeshProUGUI textComp in allTextComponents)
        {
            string objName = textComp.gameObject.name.ToLower();
            string textContent = textComp.text.ToLower();
            
            Debug.Log($"📝 Found TextMeshProUGUI: '{textComp.gameObject.name}' with text: '{textComp.text}'");
            
            // Try to identify score text
            if (scoreText == null && (objName.Contains("score") || textContent.Contains("score")))
            {
                scoreText = textComp;
                Debug.Log($"✅ Assigned score text: {objName}");
            }
            // Try to identify feedback text
            else if (feedbackText == null && (objName.Contains("feedback") || objName.Contains("message") || textContent.Contains("good") || textContent.Contains("excellent")))
            {
                feedbackText = textComp;
                Debug.Log($"✅ Assigned feedback text: {objName}");
            }
            // Try to identify session text
            else if (sessionText == null && (objName.Contains("session") || objName.Contains("time") || objName.Contains("cycle") || textContent.Contains("session")))
            {
                sessionText = textComp;
                Debug.Log($"✅ Assigned session text: {objName}");
            }
        }
        
        // If we still don't have assignments, try to use the first few text components we found
        if (allTextComponents.Length > 0)
        {
            if (scoreText == null)
            {
                scoreText = allTextComponents[0];
                Debug.Log($"🔄 Using first TextMeshProUGUI as score text: {allTextComponents[0].gameObject.name}");
            }
            
            if (feedbackText == null && allTextComponents.Length > 1)
            {
                feedbackText = allTextComponents[1];
                Debug.Log($"🔄 Using second TextMeshProUGUI as feedback text: {allTextComponents[1].gameObject.name}");
            }
            
            if (sessionText == null && allTextComponents.Length > 2)
            {
                sessionText = allTextComponents[2];
                Debug.Log($"🔄 Using third TextMeshProUGUI as session text: {allTextComponents[2].gameObject.name}");
            }
        }
        
        // Log final assignments
        Debug.Log($"📊 Final assignments:");
        Debug.Log($"   Score Text: {(scoreText != null ? scoreText.gameObject.name : "None")}");
        Debug.Log($"   Feedback Text: {(feedbackText != null ? feedbackText.gameObject.name : "None")}");
        Debug.Log($"   Session Text: {(sessionText != null ? sessionText.gameObject.name : "None")}");
    }
    
    void Update()
    {
        // Update at specified intervals to avoid performance issues
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateDisplay()
    {
        // Get current score from calculator
        if (scoreCalculator != null)
        {
            // Use reflection to get the smoothed score (private field)
            var smoothedScoreField = scoreCalculator.GetType().GetField("smoothedScore", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (smoothedScoreField != null)
            {
                currentScore = (float)smoothedScoreField.GetValue(scoreCalculator);
            }
            else
            {
                // Fallback: try to get public score if available
                currentScore = 0f;
            }
        }
        
        // Get session information
        UpdateSessionInfo();
        
        // Update score text
        UpdateScoreText();
        
        // Update feedback text
        UpdateFeedbackText();
        
        // Update session text
        UpdateSessionText();
        
        // Debug output
        if (showDebugInfo && Time.time % 5f < Time.deltaTime)
        {
            Debug.Log($"🎯 Quest Score: {currentScore:F1} | Session: {sessionTime:F1}s | Cycles: {cycleCount}");
        }
    }
    
    void UpdateSessionInfo()
    {
        if (udpReceiver != null && udpReceiver.gotPacket)
        {
            // Calculate session time (simplified)
            sessionTime = Time.time;
            
            // Try to get cycle count from score calculator
            if (scoreCalculator != null)
            {
                var cyclesField = scoreCalculator.GetType().GetField("breathingCycles", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (cyclesField != null)
                {
                    var cycles = cyclesField.GetValue(scoreCalculator);
                    if (cycles != null)
                    {
                        var countProperty = cycles.GetType().GetProperty("Count");
                        if (countProperty != null)
                        {
                            cycleCount = (int)countProperty.GetValue(cycles);
                        }
                    }
                }
            }
        }
        else
        {
            sessionTime = 0f;
            cycleCount = 0;
        }
    }
    
    void UpdateScoreText()
    {
        if (scoreText == null) return;
        
        // Format score text
        string scoreDisplay = $"Score: {currentScore:F0}";
        
        // Determine color based on score
        Color scoreColor;
        if (currentScore >= 85f)
        {
            scoreColor = excellentColor;
        }
        else if (currentScore >= 70f)
        {
            scoreColor = goodColor;
        }
        else if (currentScore > 0f)
        {
            scoreColor = poorColor;
        }
        else
        {
            scoreColor = noDataColor;
            scoreDisplay = "Score: --";
        }
        
        // Update text
        scoreText.text = scoreDisplay;
        scoreText.color = scoreColor;
    }
    
    void UpdateFeedbackText()
    {
        if (feedbackText == null) return;
        
        // Generate feedback based on score
        string feedback = "";
        Color feedbackColor = noDataColor;
        
        if (currentScore >= 85f)
        {
            feedback = "Excellent! 🎉";
            feedbackColor = excellentColor;
        }
        else if (currentScore >= 70f)
        {
            feedback = "Good breathing! 👍";
            feedbackColor = goodColor;
        }
        else if (currentScore > 0f)
        {
            feedback = "Keep practicing! 💪";
            feedbackColor = poorColor;
        }
        else
        {
            feedback = "Follow the breathing guide";
            feedbackColor = noDataColor;
        }
        
        feedbackText.text = feedback;
        feedbackText.color = feedbackColor;
    }
    
    void UpdateSessionText()
    {
        if (sessionText == null) return;
        
        if (sessionTime > 0f)
        {
            int minutes = Mathf.FloorToInt(sessionTime / 60f);
            int seconds = Mathf.FloorToInt(sessionTime % 60f);
            string sessionDisplay = $"Session: {minutes:00}:{seconds:00} | Cycles: {cycleCount}";
            
            sessionText.text = sessionDisplay;
            sessionText.color = Color.white;
        }
        else
        {
            sessionText.text = "No active session";
            sessionText.color = noDataColor;
        }
    }
    
    // Public methods for external access
    public float GetCurrentScore() => currentScore;
    public string GetCurrentFeedback() => currentFeedback;
    public float GetSessionTime() => sessionTime;
    public int GetCycleCount() => cycleCount;
    
    // Manual assignment methods for troubleshooting
    [ContextMenu("Re-scan Text Components")]
    public void RescanTextComponents()
    {
        scoreText = null;
        feedbackText = null;
        sessionText = null;
        FindExistingTextComponents();
        Debug.Log("🔄 Re-scanned text components");
    }
    
    [ContextMenu("List All TextMeshPro Components")]
    public void ListAllTextComponents()
    {
        TextMeshProUGUI[] allTextComponents = FindObjectsOfType<TextMeshProUGUI>();
        Debug.Log($"🔍 Found {allTextComponents.Length} TextMeshProUGUI components:");
        
        for (int i = 0; i < allTextComponents.Length; i++)
        {
            Debug.Log($"   {i + 1}. '{allTextComponents[i].gameObject.name}' - Text: '{allTextComponents[i].text}'");
        }
        
        TextMeshPro[] allTextMeshProComponents = FindObjectsOfType<TextMeshPro>();
        Debug.Log($"🔍 Found {allTextMeshProComponents.Length} TextMeshPro (3D) components:");
        
        for (int i = 0; i < allTextMeshProComponents.Length; i++)
        {
            Debug.Log($"   {i + 1}. '{allTextMeshProComponents[i].gameObject.name}' - Text: '{allTextMeshProComponents[i].text}'");
        }
    }
    
    // Manual assignment methods
    public void SetScoreText(TextMeshProUGUI textComponent)
    {
        scoreText = textComponent;
        Debug.Log($"✅ Manually assigned score text: {textComponent.gameObject.name}");
    }
    
    public void SetFeedbackText(TextMeshProUGUI textComponent)
    {
        feedbackText = textComponent;
        Debug.Log($"✅ Manually assigned feedback text: {textComponent.gameObject.name}");
    }
    
    public void SetSessionText(TextMeshProUGUI textComponent)
    {
        sessionText = textComponent;
        Debug.Log($"✅ Manually assigned session text: {textComponent.gameObject.name}");
    }
    
    // Easy assignment method - call this from another script or inspector
    [ContextMenu("Auto-Assign First 3 TextMeshProUGUI Components")]
    public void AutoAssignFirstThreeTextComponents()
    {
        TextMeshProUGUI[] allTextComponents = FindObjectsOfType<TextMeshProUGUI>();
        
        if (allTextComponents.Length >= 1)
        {
            scoreText = allTextComponents[0];
            Debug.Log($"✅ Auto-assigned score text: {allTextComponents[0].gameObject.name}");
        }
        
        if (allTextComponents.Length >= 2)
        {
            feedbackText = allTextComponents[1];
            Debug.Log($"✅ Auto-assigned feedback text: {allTextComponents[1].gameObject.name}");
        }
        
        if (allTextComponents.Length >= 3)
        {
            sessionText = allTextComponents[2];
            Debug.Log($"✅ Auto-assigned session text: {allTextComponents[2].gameObject.name}");
        }
        
        Debug.Log($"🎯 Auto-assignment complete! Found {allTextComponents.Length} TextMeshProUGUI components.");
    }
    
    // Manual control methods for testing
    [ContextMenu("Test Excellent Score")]
    public void TestExcellentScore()
    {
        currentScore = 95f;
        UpdateDisplay();
        Debug.Log("🧪 Testing excellent score display");
    }
    
    [ContextMenu("Test Good Score")]
    public void TestGoodScore()
    {
        currentScore = 75f;
        UpdateDisplay();
        Debug.Log("🧪 Testing good score display");
    }
    
    [ContextMenu("Test Poor Score")]
    public void TestPoorScore()
    {
        currentScore = 45f;
        UpdateDisplay();
        Debug.Log("🧪 Testing poor score display");
    }
    
    [ContextMenu("Test No Data")]
    public void TestNoData()
    {
        currentScore = 0f;
        sessionTime = 0f;
        cycleCount = 0;
        UpdateDisplay();
        Debug.Log("🧪 Testing no data display");
    }
}
