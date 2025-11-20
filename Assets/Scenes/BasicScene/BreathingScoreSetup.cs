using UnityEngine;

/// <summary>
/// Breathing Score Setup - Automatically configures the breathing score system
/// Attach this to any GameObject in the scene to set up the complete scoring system
/// </summary>
public class BreathingScoreSetup : MonoBehaviour
{
    [Header("Auto Setup")]
    [Tooltip("Automatically setup the breathing score system on Start")]
    public bool autoSetupOnStart = true;
    
    [Tooltip("Create UI elements automatically")]
    public bool createUI = true;
    
    [Tooltip("Find existing components or create new ones")]
    public bool findExistingComponents = true;
    
    [Header("Manual References")]
    [Tooltip("Manual reference to BreathingPhaseAnimator")]
    public BreathingPhaseAnimator phaseAnimator;
    
    [Tooltip("Manual reference to UDPHeartRateReceiver")]
    public UDPHeartRateReceiver udpReceiver;
    
    [Tooltip("Manual reference to Canvas for UI")]
    public Canvas targetCanvas;
    
    [Header("Debug")]
    [Tooltip("Show setup debug information")]
    public bool showDebugInfo = true;
    
    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupBreathingScoreSystem();
        }
    }
    
    [ContextMenu("Setup Breathing Score System")]
    public void SetupBreathingScoreSystem()
    {
        if (showDebugInfo)
        {
            Debug.Log("🔧 Setting up Breathing Score System...");
        }
        
        // Find or create required components
        FindOrCreateComponents();
        
        // Create UI if requested
        if (createUI)
        {
            CreateUIElements();
        }
        
        // Configure components
        ConfigureComponents();
        
        if (showDebugInfo)
        {
            Debug.Log("✅ Breathing Score System setup complete!");
        }
    }
    
    void FindOrCreateComponents()
    {
        // Find BreathingPhaseAnimator
        if (phaseAnimator == null && findExistingComponents)
        {
            phaseAnimator = FindObjectOfType<BreathingPhaseAnimator>();
            if (phaseAnimator == null)
            {
                Debug.LogWarning("BreathingScoreSetup: BreathingPhaseAnimator not found. Please ensure it exists in the scene.");
            }
        }
        
        // Find UDPHeartRateReceiver
        if (udpReceiver == null && findExistingComponents)
        {
            udpReceiver = FindObjectOfType<UDPHeartRateReceiver>();
            if (udpReceiver == null)
            {
                Debug.LogWarning("BreathingScoreSetup: UDPHeartRateReceiver not found. Please ensure it exists in the scene.");
            }
        }
        
        // Find Canvas
        if (targetCanvas == null && findExistingComponents)
        {
            targetCanvas = FindObjectOfType<Canvas>();
            if (targetCanvas == null)
            {
                Debug.LogWarning("BreathingScoreSetup: No Canvas found. UI elements cannot be created.");
            }
        }
    }
    
    void CreateUIElements()
    {
        if (targetCanvas == null)
        {
            Debug.LogWarning("BreathingScoreSetup: Cannot create UI elements - no Canvas found.");
            return;
        }
        
        // Create BreathingScoreCalculator if it doesn't exist
        BreathingScoreCalculator scoreCalculator = FindObjectOfType<BreathingScoreCalculator>();
        if (scoreCalculator == null)
        {
            GameObject scoreCalculatorObj = new GameObject("BreathingScoreCalculator");
            scoreCalculator = scoreCalculatorObj.AddComponent<BreathingScoreCalculator>();
            
            if (showDebugInfo)
            {
                Debug.Log("📊 Created BreathingScoreCalculator");
            }
        }
        
        // Create BreathingScoreUIManager if it doesn't exist
        BreathingScoreUIManager uiManager = FindObjectOfType<BreathingScoreUIManager>();
        if (uiManager == null)
        {
            GameObject uiManagerObj = new GameObject("BreathingScoreUIManager");
            uiManager = uiManagerObj.AddComponent<BreathingScoreUIManager>();
            uiManager.targetCanvas = targetCanvas;
            uiManager.scoreCalculator = scoreCalculator;
            
            if (showDebugInfo)
            {
                Debug.Log("📊 Created BreathingScoreUIManager");
            }
        }
    }
    
    void ConfigureComponents()
    {
        // Configure BreathingScoreCalculator if it exists
        BreathingScoreCalculator scoreCalculator = FindObjectOfType<BreathingScoreCalculator>();
        if (scoreCalculator != null)
        {
            // Set up optimal scoring parameters
            scoreCalculator.maxScore = 100;
            scoreCalculator.goodScoreThreshold = 70;
            scoreCalculator.excellentScoreThreshold = 85;
            scoreCalculator.timingTolerance = 0.5f;
            scoreCalculator.transitionTolerance = 0.3f;
            scoreCalculator.timingWeight = 0.4f;
            scoreCalculator.phaseSyncWeight = 0.3f;
            scoreCalculator.consistencyWeight = 0.3f;
            
            // Configure colors
            scoreCalculator.excellentColor = new Color(0.2f, 0.8f, 0.2f); // Green
            scoreCalculator.goodColor = new Color(0.8f, 0.8f, 0.2f); // Yellow
            scoreCalculator.poorColor = new Color(0.8f, 0.2f, 0.2f); // Red
            
            if (showDebugInfo)
            {
                Debug.Log("⚙️ Configured BreathingScoreCalculator parameters");
            }
        }
        
        // Configure BreathingScoreUIManager if it exists
        BreathingScoreUIManager uiManager = FindObjectOfType<BreathingScoreUIManager>();
        if (uiManager != null)
        {
            // Set up UI parameters
            uiManager.scoreFontSize = 48;
            uiManager.feedbackFontSize = 24;
            uiManager.summaryFontSize = 18;
            uiManager.enableScorePulse = true;
            uiManager.enableColorTransitions = true;
            uiManager.pulseSpeed = 2f;
            uiManager.colorTransitionSpeed = 3f;
            
            if (showDebugInfo)
            {
                Debug.Log("⚙️ Configured BreathingScoreUIManager parameters");
            }
        }
    }
    
    // Public methods for manual control
    [ContextMenu("Test Complete System")]
    public void TestCompleteSystem()
    {
        BreathingScoreCalculator scoreCalculator = FindObjectOfType<BreathingScoreCalculator>();
        BreathingScoreUIManager uiManager = FindObjectOfType<BreathingScoreUIManager>();
        
        if (scoreCalculator != null)
        {
            scoreCalculator.StartNewSession();
            Debug.Log("🧪 Started test session");
        }
        
        if (uiManager != null)
        {
            uiManager.TestScoreDisplay();
            Debug.Log("🧪 Tested UI display");
        }
    }
    
    [ContextMenu("Reset All Components")]
    public void ResetAllComponents()
    {
        BreathingScoreCalculator scoreCalculator = FindObjectOfType<BreathingScoreCalculator>();
        BreathingScoreUIManager uiManager = FindObjectOfType<BreathingScoreUIManager>();
        
        if (scoreCalculator != null)
        {
            scoreCalculator.ResetAllData();
        }
        
        if (uiManager != null)
        {
            uiManager.ResetUI();
        }
        
        Debug.Log("🔄 Reset all breathing score components");
    }
    
    [ContextMenu("Show System Status")]
    public void ShowSystemStatus()
    {
        BreathingScoreCalculator scoreCalculator = FindObjectOfType<BreathingScoreCalculator>();
        BreathingScoreUIManager uiManager = FindObjectOfType<BreathingScoreUIManager>();
        BreathingPhaseAnimator phaseAnimator = FindObjectOfType<BreathingPhaseAnimator>();
        UDPHeartRateReceiver udpReceiver = FindObjectOfType<UDPHeartRateReceiver>();
        
        Debug.Log("📊 Breathing Score System Status:");
        Debug.Log($"  BreathingScoreCalculator: {(scoreCalculator != null ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"  BreathingScoreUIManager: {(uiManager != null ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"  BreathingPhaseAnimator: {(phaseAnimator != null ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"  UDPHeartRateReceiver: {(udpReceiver != null ? "✅ Found" : "❌ Missing")}");
        Debug.Log($"  Canvas: {(targetCanvas != null ? "✅ Found" : "❌ Missing")}");
        
        if (scoreCalculator != null)
        {
            Debug.Log($"  Current Score: {scoreCalculator.GetCurrentScore():F1}");
            Debug.Log($"  Session Active: {scoreCalculator.IsSessionActive()}");
            Debug.Log($"  Cycle Count: {scoreCalculator.GetCycleCount()}");
        }
    }
}
