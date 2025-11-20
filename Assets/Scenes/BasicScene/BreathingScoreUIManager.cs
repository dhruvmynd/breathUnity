using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Breathing Score UI Manager - Handles the visual display of breathing scores
/// Creates and manages UI elements for real-time score feedback
/// </summary>
public class BreathingScoreUIManager : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Canvas to attach UI elements to")]
    public Canvas targetCanvas;
    
    [Tooltip("Reference to BreathingScoreCalculator")]
    public BreathingScoreCalculator scoreCalculator;
    
    [Header("UI Prefab Settings")]
    [Tooltip("Font size for score display")]
    public int scoreFontSize = 48;
    
    [Tooltip("Font size for feedback text")]
    public int feedbackFontSize = 24;
    
    [Tooltip("Font size for summary text")]
    public int summaryFontSize = 18;
    
    [Tooltip("UI element spacing")]
    public float elementSpacing = 20f;
    
    [Tooltip("UI element padding")]
    public float elementPadding = 10f;
    
    [Header("Visual Effects")]
    [Tooltip("Enable score pulse animation")]
    public bool enableScorePulse = true;
    
    [Tooltip("Pulse speed for score animation")]
    public float pulseSpeed = 2f;
    
    [Tooltip("Pulse intensity")]
    public float pulseIntensity = 0.1f;
    
    [Tooltip("Enable color transitions")]
    public bool enableColorTransitions = true;
    
    [Tooltip("Transition speed")]
    public float colorTransitionSpeed = 3f;
    
    // UI Elements
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI feedbackText;
    private TextMeshProUGUI summaryText;
    private Image scoreBackground;
    private Image feedbackBackground;
    
    // Animation state
    private float pulseTime = 0f;
    private Color targetScoreColor;
    private Color targetFeedbackColor;
    private Color currentScoreColor;
    private Color currentFeedbackColor;
    
    void Start()
    {
        // Find canvas if not assigned
        if (targetCanvas == null)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogError("BreathingScoreUIManager: No Canvas found! Please assign a Canvas.");
                return;
            }
        }
        
        // Find score calculator if not assigned
        if (scoreCalculator == null)
        {
            scoreCalculator = FindObjectOfType<BreathingScoreCalculator>();
            if (scoreCalculator == null)
            {
                Debug.LogError("BreathingScoreUIManager: BreathingScoreCalculator not found!");
                return;
            }
        }
        
        // Create UI elements
        CreateUIElements();
        
        // Initialize colors
        InitializeColors();
        
        Debug.Log("📊 Breathing Score UI Manager initialized");
    }
    
    void CreateUIElements()
    {
        // Create main container
        GameObject container = new GameObject("BreathingScoreContainer");
        container.transform.SetParent(targetCanvas.transform, false);
        
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.8f);
        containerRect.anchorMax = new Vector2(0.5f, 0.8f);
        containerRect.anchoredPosition = Vector2.zero;
        containerRect.sizeDelta = new Vector2(400f, 200f);
        
        // Create score display
        CreateScoreDisplay(container);
        
        // Create feedback display
        CreateFeedbackDisplay(container);
        
        // Create summary display
        CreateSummaryDisplay(container);
    }
    
    void CreateScoreDisplay(GameObject parent)
    {
        // Score background
        GameObject scoreBgObj = new GameObject("ScoreBackground");
        scoreBgObj.transform.SetParent(parent.transform, false);
        
        scoreBackground = scoreBgObj.AddComponent<Image>();
        scoreBackground.color = new Color(0f, 0f, 0f, 0.7f);
        
        RectTransform scoreBgRect = scoreBgObj.GetComponent<RectTransform>();
        scoreBgRect.anchorMin = new Vector2(0f, 0.6f);
        scoreBgRect.anchorMax = new Vector2(1f, 1f);
        scoreBgRect.offsetMin = new Vector2(elementPadding, elementPadding);
        scoreBgRect.offsetMax = new Vector2(-elementPadding, -elementPadding);
        
        // Score text
        GameObject scoreTextObj = new GameObject("ScoreText");
        scoreTextObj.transform.SetParent(scoreBgObj.transform, false);
        
        scoreText = scoreTextObj.AddComponent<TextMeshProUGUI>();
        scoreText.text = "Score: --";
        scoreText.fontSize = scoreFontSize;
        scoreText.color = Color.white;
        scoreText.alignment = TextAlignmentOptions.Center;
        scoreText.fontStyle = FontStyles.Bold;
        
        RectTransform scoreTextRect = scoreTextObj.GetComponent<RectTransform>();
        scoreTextRect.anchorMin = Vector2.zero;
        scoreTextRect.anchorMax = Vector2.one;
        scoreTextRect.offsetMin = Vector2.zero;
        scoreTextRect.offsetMax = Vector2.zero;
    }
    
    void CreateFeedbackDisplay(GameObject parent)
    {
        // Feedback background
        GameObject feedbackBgObj = new GameObject("FeedbackBackground");
        feedbackBgObj.transform.SetParent(parent.transform, false);
        
        feedbackBackground = feedbackBgObj.AddComponent<Image>();
        feedbackBackground.color = new Color(0f, 0f, 0f, 0.5f);
        
        RectTransform feedbackBgRect = feedbackBgObj.GetComponent<RectTransform>();
        feedbackBgRect.anchorMin = new Vector2(0f, 0.3f);
        feedbackBgRect.anchorMax = new Vector2(1f, 0.6f);
        feedbackBgRect.offsetMin = new Vector2(elementPadding, elementPadding);
        feedbackBgRect.offsetMax = new Vector2(-elementPadding, -elementPadding);
        
        // Feedback text
        GameObject feedbackTextObj = new GameObject("FeedbackText");
        feedbackTextObj.transform.SetParent(feedbackBgObj.transform, false);
        
        feedbackText = feedbackTextObj.AddComponent<TextMeshProUGUI>();
        feedbackText.text = "Follow the breathing guidance";
        feedbackText.fontSize = feedbackFontSize;
        feedbackText.color = Color.white;
        feedbackText.alignment = TextAlignmentOptions.Center;
        
        RectTransform feedbackTextRect = feedbackTextObj.GetComponent<RectTransform>();
        feedbackTextRect.anchorMin = Vector2.zero;
        feedbackTextRect.anchorMax = Vector2.one;
        feedbackTextRect.offsetMin = Vector2.zero;
        feedbackTextRect.offsetMax = Vector2.zero;
    }
    
    void CreateSummaryDisplay(GameObject parent)
    {
        // Summary background
        GameObject summaryBgObj = new GameObject("SummaryBackground");
        summaryBgObj.transform.SetParent(parent.transform, false);
        
        Image summaryBackground = summaryBgObj.AddComponent<Image>();
        summaryBackground.color = new Color(0f, 0f, 0f, 0.3f);
        
        RectTransform summaryBgRect = summaryBgObj.GetComponent<RectTransform>();
        summaryBgRect.anchorMin = new Vector2(0f, 0f);
        summaryBgRect.anchorMax = new Vector2(1f, 0.3f);
        summaryBgRect.offsetMin = new Vector2(elementPadding, elementPadding);
        summaryBgRect.offsetMax = new Vector2(-elementPadding, -elementPadding);
        
        // Summary text
        GameObject summaryTextObj = new GameObject("SummaryText");
        summaryTextObj.transform.SetParent(summaryBgObj.transform, false);
        
        summaryText = summaryTextObj.AddComponent<TextMeshProUGUI>();
        summaryText.text = "";
        summaryText.fontSize = summaryFontSize;
        summaryText.color = Color.white;
        summaryText.alignment = TextAlignmentOptions.Center;
        
        RectTransform summaryTextRect = summaryTextObj.GetComponent<RectTransform>();
        summaryTextRect.anchorMin = Vector2.zero;
        summaryTextRect.anchorMax = Vector2.one;
        summaryTextRect.offsetMin = Vector2.zero;
        summaryTextRect.offsetMax = Vector2.zero;
    }
    
    void InitializeColors()
    {
        targetScoreColor = Color.white;
        targetFeedbackColor = Color.white;
        currentScoreColor = Color.white;
        currentFeedbackColor = Color.white;
    }
    
    void Update()
    {
        if (scoreCalculator == null) return;
        
        // Update score display
        UpdateScoreDisplay();
        
        // Update feedback display
        UpdateFeedbackDisplay();
        
        // Update summary display
        UpdateSummaryDisplay();
        
        // Handle animations
        if (enableScorePulse)
        {
            UpdatePulseAnimation();
        }
        
        if (enableColorTransitions)
        {
            UpdateColorTransitions();
        }
    }
    
    void UpdateScoreDisplay()
    {
        if (scoreText == null) return;
        
        float currentScore = scoreCalculator.GetCurrentScore();
        scoreText.text = $"Score: {currentScore:F0}";
        
        // Update target color based on score
        if (currentScore >= scoreCalculator.excellentScoreThreshold)
        {
            targetScoreColor = scoreCalculator.excellentColor;
        }
        else if (currentScore >= scoreCalculator.goodScoreThreshold)
        {
            targetScoreColor = scoreCalculator.goodColor;
        }
        else
        {
            targetScoreColor = scoreCalculator.poorColor;
        }
    }
    
    void UpdateFeedbackDisplay()
    {
        if (feedbackText == null) return;
        
        // Get feedback from score calculator
        string feedback = "Follow the breathing guidance";
        
        // Try to get current feedback from the score calculator
        if (scoreCalculator.CurrentCycle != null && scoreCalculator.CurrentCycle.phases.Count > 0)
        {
            var lastPhase = scoreCalculator.CurrentCycle.phases[scoreCalculator.CurrentCycle.phases.Count - 1];
            feedback = lastPhase.feedback;
        }
        
        feedbackText.text = feedback;
        
        // Update feedback color based on score
        float currentScore = scoreCalculator.GetCurrentScore();
        if (currentScore >= scoreCalculator.excellentScoreThreshold)
        {
            targetFeedbackColor = scoreCalculator.excellentColor;
        }
        else if (currentScore >= scoreCalculator.goodScoreThreshold)
        {
            targetFeedbackColor = scoreCalculator.goodColor;
        }
        else
        {
            targetFeedbackColor = scoreCalculator.poorColor;
        }
    }
    
    void UpdateSummaryDisplay()
    {
        if (summaryText == null) return;
        
        if (scoreCalculator.IsSessionActive())
        {
            float sessionTime = scoreCalculator.GetSessionDuration();
            int cycleCount = scoreCalculator.GetCycleCount();
            summaryText.text = $"Session: {sessionTime:F1}s | Cycles: {cycleCount}";
        }
        else
        {
            summaryText.text = "Session ended - Check console for summary";
        }
    }
    
    void UpdatePulseAnimation()
    {
        if (scoreText == null) return;
        
        pulseTime += Time.deltaTime * pulseSpeed;
        float pulseScale = 1f + Mathf.Sin(pulseTime) * pulseIntensity;
        
        scoreText.transform.localScale = Vector3.one * pulseScale;
    }
    
    void UpdateColorTransitions()
    {
        if (scoreText == null || feedbackText == null) return;
        
        // Smooth color transitions
        float transitionSpeed = Time.deltaTime * colorTransitionSpeed;
        
        currentScoreColor = Color.Lerp(currentScoreColor, targetScoreColor, transitionSpeed);
        currentFeedbackColor = Color.Lerp(currentFeedbackColor, targetFeedbackColor, transitionSpeed);
        
        scoreText.color = currentScoreColor;
        feedbackText.color = currentFeedbackColor;
    }
    
    // Public methods for external control
    public void SetScoreVisibility(bool visible)
    {
        if (scoreText != null) scoreText.gameObject.SetActive(visible);
        if (scoreBackground != null) scoreBackground.gameObject.SetActive(visible);
    }
    
    public void SetFeedbackVisibility(bool visible)
    {
        if (feedbackText != null) feedbackText.gameObject.SetActive(visible);
        if (feedbackBackground != null) feedbackBackground.gameObject.SetActive(visible);
    }
    
    public void SetSummaryVisibility(bool visible)
    {
        if (summaryText != null) summaryText.gameObject.SetActive(visible);
    }
    
    public void SetAllVisibility(bool visible)
    {
        SetScoreVisibility(visible);
        SetFeedbackVisibility(visible);
        SetSummaryVisibility(visible);
    }
    
    // Manual control methods
    [ContextMenu("Test Score Display")]
    public void TestScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: 85";
            scoreText.color = Color.green;
        }
        
        if (feedbackText != null)
        {
            feedbackText.text = "Excellent breathing!";
            feedbackText.color = Color.green;
        }
        
        Debug.Log("🧪 Testing score display");
    }
    
    [ContextMenu("Reset UI")]
    public void ResetUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: --";
            scoreText.color = Color.white;
        }
        
        if (feedbackText != null)
        {
            feedbackText.text = "Follow the breathing guidance";
            feedbackText.color = Color.white;
        }
        
        if (summaryText != null)
        {
            summaryText.text = "";
        }
        
        Debug.Log("🔄 UI reset");
    }
}
