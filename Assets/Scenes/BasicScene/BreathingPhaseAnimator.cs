using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// Breathing Phase Animator - Animates a sphere based on iOS breathing phase data
/// Levitates up on inhale, holds position, then floats down on exhale with smooth transitions
/// User is inside the sphere and follows its movement
/// </summary>
public class BreathingPhaseAnimator : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("Camera to center sphere around at start (auto-finds if not set)")]
    public Camera targetCamera;
    [Tooltip("Center sphere on camera's starting position (user inside sphere)")]
    public bool centerOnCameraStart = true;
    
    [Header("Breathing Animation Settings")]
    [Tooltip("Constant scale for the sphere (fixed size - larger for user to be inside)")]
    public float constantScale = 12.0f;
    
    [Tooltip("Base Y position when at rest")]
    public float baseYPosition = 0f;
    
    [Tooltip("How far to move up/down from base position")]
    public float positionRange = 2.0f;
    
    [Tooltip("Maximum Y position during inhale (how high to levitate)")]
    public float maxYPosition = 1.0f;
    
    [Tooltip("Minimum Y position during exhale (how low to levitate)")]
    public float minYPosition = -1.0f;
    
    [Tooltip("Speed of position transitions (higher = faster)")]
    public float animationSpeed = 1.5f;
    
    [Tooltip("Smoothing factor for position changes (higher = smoother)")]
    [Range(0.1f, 10f)]
    public float positionSmoothing = 5.0f;
    
    [Tooltip("Smoothing factor for color changes (higher = smoother)")]
    [Range(0.1f, 10f)]
    public float colorSmoothing = 3.0f;
    
    [Tooltip("Deadzone for position changes (prevents micro-movements)")]
    [Range(0.001f, 0.1f)]
    public float positionDeadzone = 0.01f;
    
    [Tooltip("Hold duration for inhale/exhale phases (in seconds)")]
    public float holdDuration = 0.5f;
    
    [Header("Animation Curve")]
    [Tooltip("Curve for smooth inhale animation (0=rest, 1=full inhale)")]
    public AnimationCurve inhaleCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Curve for smooth exhale animation (0=full inhale, 1=full exhale)")]
    public AnimationCurve exhaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
    
    [Tooltip("Follow iOS breathing timing exactly")]
    public bool followIOSTiming = true;
    
    [Tooltip("Minimum time between phase changes (seconds)")]
    public float minPhaseChangeTime = 0.5f;
    
    [Header("Visual Effects")]
    [Tooltip("Color during inhale phase")]
    public Color inhaleColor = new Color(0f, 1f, 0f, 0.75f);
    
    [Tooltip("Color during exhale phase")]
    public Color exhaleColor = new Color(0f, 0f, 1f, 0.75f);
    
    [Tooltip("Color during hold phases")]
    public Color holdColor = new Color(1f, 1f, 1f, 0.75f);
    
    [Tooltip("Color when no breathing data")]
    public Color noDataColor = new Color(0.5f, 0.5f, 0.5f, 0.75f);
    
    [Tooltip("Enable color transitions")]
    public bool enableColorTransitions = true;
    
    [Header("UI Display")]
    [Tooltip("Text component to show current breathing phase")]
    public TextMeshProUGUI phaseText;
    
    [Tooltip("Text component to show phase duration")]
    public TextMeshProUGUI durationText;
    
    [Header("Debug")]
    [Tooltip("Show debug information in console")]
    public bool showDebugInfo = true;
    
    [Tooltip("Show current values in inspector")]
    public bool showCurrentValues = true;
    
    // Internal state
    private float currentYPosition = 0f;
    private float targetYPosition = 0f;
    private Vector3 startPosition;
    private Color currentColor;
    private Color targetColor;
    
    // Animation state
    private string currentPhase = "";
    private string previousPhase = "";
    private float phaseStartTime = 0f;
    private float currentPhaseDuration = 0f;
    private bool isAnimating = false;
    private float lastPhaseChangeTime = 0f;
    
    // No smoothing buffers needed - using iOS timing directly
    
    // Breathing phase states
    public enum BreathingState
    {
        NoData,
        Inhale,
        InhaleHold,
        Exhale,
        ExhaleHold
    }
    
    private BreathingState currentState = BreathingState.NoData;
    private float stateStartTime = 0f;
    
    // Reference to UDP receiver
    private UDPHeartRateReceiver udpReceiver;
    
    // Renderer for color changes
    private Renderer sphereRenderer;
    
    void Start()
    {
        // Find camera if not assigned
        if (centerOnCameraStart && targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                // Try to find any camera in the scene
                targetCamera = FindObjectOfType<Camera>();
            }
            
            if (targetCamera != null)
            {
                Debug.Log($"🎥 BreathingPhaseAnimator: Auto-found camera '{targetCamera.name}'");
            }
            else
            {
                Debug.LogWarning("🎥 BreathingPhaseAnimator: No camera found! Sphere will use its current position.");
                centerOnCameraStart = false;
            }
        }
        
        // Find UDP receiver
        udpReceiver = FindObjectOfType<UDPHeartRateReceiver>();
        if (udpReceiver == null)
        {
            Debug.LogError("BreathingPhaseAnimator: UDPHeartRateReceiver not found! Make sure it's in the scene.");
        }
        
        // Get sphere renderer
        sphereRenderer = GetComponent<Renderer>();
        if (sphereRenderer == null)
        {
            Debug.LogWarning("BreathingPhaseAnimator: No Renderer component found. Color transitions will not work.");
        }
        else
        {
            // Enable transparency on the material
            SetupMaterialTransparency();
        }
        
        // Initialize position and color
        // If centering on camera, use camera's position as the base
        if (centerOnCameraStart && targetCamera != null)
        {
            startPosition = targetCamera.transform.position;
            Debug.Log($"🫁 Sphere centered on camera at {startPosition}");
        }
        else
        {
            startPosition = transform.position;
        }
        
        currentYPosition = baseYPosition;
        targetYPosition = baseYPosition;
        currentColor = noDataColor;
        targetColor = noDataColor;
        
        // Set constant scale (fixed size - no growing/shrinking)
        transform.localScale = Vector3.one * constantScale;
        
        // Set initial position (centered on camera's X/Z, with base Y offset)
        transform.position = new Vector3(startPosition.x, startPosition.y + currentYPosition, startPosition.z);
        
        // Set initial color
        if (sphereRenderer != null)
        {
            sphereRenderer.material.color = currentColor;
        }
        
        // Setup UI if components are assigned
        SetupUI();
        
        Debug.Log($"🫁 Breathing Phase Animator initialized at {transform.position} with scale {constantScale}");
    }
    
    void SetupUI()
    {
        if (phaseText != null)
        {
            phaseText.text = "No Data";
            phaseText.color = noDataColor;
        }
        
        if (durationText != null)
        {
            durationText.text = "0.0s";
            durationText.color = noDataColor;
        }
    }
    
    void SetupMaterialTransparency()
    {
        if (sphereRenderer == null || sphereRenderer.material == null) return;
        
        Material material = sphereRenderer.material;
        
        // Enable transparency for Standard shader
        // Set rendering mode to Transparent (3) or Fade (2)
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000; // Transparent queue
        
        Debug.Log("🎨 Material transparency enabled");
    }
    
    void Update()
    {
        if (udpReceiver == null) return;
        
        // Get breathing phase data from UDP receiver
        string breathingPhase = udpReceiver.breathingPhase;
        float phaseDuration = udpReceiver.phaseDuration;
        
        // Update animation based on breathing phase
        UpdateBreathingAnimation(breathingPhase, phaseDuration);
        
        // Update visual appearance
        UpdateVisuals();
        
        // Update UI
        UpdateUI(breathingPhase, phaseDuration);
        
        // Debug output
        if (showDebugInfo && Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"🫁 Breathing: {breathingPhase} ({phaseDuration:F1}s) | Y Position: {currentYPosition:F2} | State: {currentState}");
        }
    }
    
    void UpdateBreathingAnimation(string breathingPhase, float phaseDuration)
    {
        // Handle no data case
        if (string.IsNullOrEmpty(breathingPhase))
        {
            if (currentState != BreathingState.NoData)
            {
                currentState = BreathingState.NoData;
                targetYPosition = baseYPosition;
                targetColor = noDataColor;
                isAnimating = true;
            }
            return;
        }
        
         // Check if phase has changed and enough time has passed
         float timeSinceLastChange = Time.time - lastPhaseChangeTime;
         if (breathingPhase != currentPhase && timeSinceLastChange >= minPhaseChangeTime)
         {
             previousPhase = currentPhase;
             currentPhase = breathingPhase;
             phaseStartTime = Time.time;
             lastPhaseChangeTime = Time.time;
             isAnimating = true;
             
             // Phase change processed
             
             // Update state based on phase
             UpdateBreathingState(breathingPhase);
             
             // Debug phase change with more detail
             if (showDebugInfo)
             {
                 Debug.Log($"🫁 Phase changed: '{previousPhase}' → '{breathingPhase}' (Duration: {phaseDuration:F1}s) | State: {currentState}");
             }
         }
         else if (breathingPhase != currentPhase && timeSinceLastChange < minPhaseChangeTime)
         {
             // Ignore rapid phase changes
             if (showDebugInfo)
             {
                 Debug.Log($"🫁 Ignoring rapid phase change: '{currentPhase}' → '{breathingPhase}' (Too soon: {timeSinceLastChange:F2}s < {minPhaseChangeTime:F2}s)");
             }
         }
        
        // Use iOS phase duration directly - no smoothing
        currentPhaseDuration = phaseDuration;
        
        // Calculate animation progress using raw iOS duration
        float animationProgress = CalculateAnimationProgress(breathingPhase, currentPhaseDuration);
        
        // Update target scale and color based on current state
        UpdateAnimationTargets(animationProgress);
    }
    
    void UpdateBreathingState(string phase)
    {
        stateStartTime = Time.time;
        
        // Convert to lowercase for consistent matching
        string phaseLower = phase.ToLower();
        
        switch (phaseLower)
        {
            case "inhale":
                currentState = BreathingState.Inhale;
                break;
            case "inhale_hold":
            case "inhalehold":
            case "hold_inhale":
            case "hold_after_inhale":
                currentState = BreathingState.InhaleHold;
                break;
            case "exhale":
                currentState = BreathingState.Exhale;
                break;
            case "exhale_hold":
            case "exhalehold":
            case "hold_exhale":
            case "hold_after_exhale":
                currentState = BreathingState.ExhaleHold;
                break;
            case "hold":
                // Determine hold type based on previous state
                if (currentState == BreathingState.Inhale || currentState == BreathingState.InhaleHold)
                {
                    currentState = BreathingState.InhaleHold;
                }
                else if (currentState == BreathingState.Exhale || currentState == BreathingState.ExhaleHold)
                {
                    currentState = BreathingState.ExhaleHold;
                }
                else
                {
                    // Default to exhale hold if we can't determine
                    currentState = BreathingState.ExhaleHold;
                }
                break;
            default:
                // Try to infer state from phase name
                if (phaseLower.Contains("inhale"))
                {
                    if (phaseLower.Contains("hold"))
                    {
                        currentState = BreathingState.InhaleHold;
                    }
                    else
                    {
                        currentState = BreathingState.Inhale;
                    }
                }
                else if (phaseLower.Contains("exhale"))
                {
                    if (phaseLower.Contains("hold"))
                    {
                        currentState = BreathingState.ExhaleHold;
                    }
                    else
                    {
                        currentState = BreathingState.Exhale;
                    }
                }
                else if (phaseLower.Contains("hold"))
                {
                    // Smart hold detection based on context
                    if (currentState == BreathingState.Inhale || currentState == BreathingState.InhaleHold)
                    {
                        currentState = BreathingState.InhaleHold;
                    }
                    else
                    {
                        currentState = BreathingState.ExhaleHold;
                    }
                }
                else
                {
                    currentState = BreathingState.NoData;
                }
                break;
        }
        
        // Debug state transitions
        if (showDebugInfo)
        {
            Debug.Log($"🫁 Phase '{phase}' → State: {currentState}");
        }
    }
    
    float CalculateAnimationProgress(string phase, float duration)
    {
        float timeInPhase = Time.time - stateStartTime;
        
        // Use iOS phase duration directly - simple linear progress
        float safeDuration = Mathf.Max(duration, 0.1f);
        float progress = Mathf.Clamp01(timeInPhase / safeDuration);
        
        return progress;
    }
    
    void UpdateAnimationTargets(float progress)
    {
        switch (currentState)
        {
            case BreathingState.Inhale:
                targetYPosition = Mathf.Lerp(baseYPosition, maxYPosition, inhaleCurve.Evaluate(progress));
                targetColor = Color.Lerp(holdColor, inhaleColor, progress);
                break;
                
            case BreathingState.InhaleHold:
                targetYPosition = maxYPosition;
                targetColor = inhaleColor;
                break;
                
            case BreathingState.Exhale:
                // Simple linear exhale - move down from max to min
                targetYPosition = Mathf.Lerp(maxYPosition, minYPosition, progress);
                targetColor = Color.Lerp(inhaleColor, exhaleColor, progress);
                
                if (showDebugInfo && Time.time % 1f < Time.deltaTime)
                {
                    Debug.Log($"🫁 Exhale: Progress={progress:F2}, Y Position={targetYPosition:F2} (Max={maxYPosition}, Min={minYPosition})");
                }
                break;
                
            case BreathingState.ExhaleHold:
                targetYPosition = minYPosition;
                targetColor = exhaleColor;
                break;
                
            case BreathingState.NoData:
                targetYPosition = baseYPosition;
                targetColor = noDataColor;
                break;
        }
    }
    
    void UpdateVisuals()
    {
        // Only update position if the difference is significant (deadzone)
        float positionDifference = Mathf.Abs(targetYPosition - currentYPosition);
        if (positionDifference > positionDeadzone)
        {
            // Use exponential smoothing for position transitions
            float positionLerpSpeed = Time.deltaTime * positionSmoothing;
            currentYPosition = Mathf.Lerp(currentYPosition, targetYPosition, positionLerpSpeed);
        }
        else
        {
            // Snap to target if within deadzone
            currentYPosition = targetYPosition;
        }
        
        // Apply position with smooth interpolation (levitation effect)
        Vector3 newPosition = new Vector3(startPosition.x, startPosition.y + currentYPosition, startPosition.z);
        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * 8f);
        
        // Use exponential smoothing for color transitions
        if (enableColorTransitions && sphereRenderer != null)
        {
            float colorLerpSpeed = Time.deltaTime * colorSmoothing;
            currentColor = Color.Lerp(currentColor, targetColor, colorLerpSpeed);
            
            // Apply color with smooth interpolation
            Color currentMaterialColor = sphereRenderer.material.color;
            Color newColor = Color.Lerp(currentMaterialColor, currentColor, Time.deltaTime * 6f);
            sphereRenderer.material.color = newColor;
        }
    }
    
    void UpdateUI(string breathingPhase, float phaseDuration)
    {
        if (phaseText != null)
        {
            string displayPhase = string.IsNullOrEmpty(breathingPhase) ? "No Data" : breathingPhase;
            phaseText.text = $"Phase: {displayPhase}";
            phaseText.color = currentColor;
        }
        
        if (durationText != null)
        {
            durationText.text = $"Duration: {phaseDuration:F1}s";
            durationText.color = currentColor;
        }
    }
    
    // Public methods for external access
    public float GetCurrentYPosition() => currentYPosition;
    public Color GetCurrentColor() => currentColor;
    public string GetCurrentPhase() => currentPhase;
    public BreathingState GetCurrentState() => currentState;
    
    // Manual control methods
    [ContextMenu("Test Inhale Animation")]
    public void TestInhaleAnimation()
    {
        currentPhase = "inhale";
        UpdateBreathingState("inhale");
        Debug.Log("🧪 Testing inhale animation");
    }
    
    [ContextMenu("Test Exhale Animation")]
    public void TestExhaleAnimation()
    {
        currentPhase = "exhale";
        UpdateBreathingState("exhale");
        Debug.Log("🧪 Testing exhale animation");
    }
    
    [ContextMenu("Test Exhale Hold")]
    public void TestExhaleHold()
    {
        currentPhase = "exhale_hold";
        UpdateBreathingState("exhale_hold");
        Debug.Log("🧪 Testing exhale hold animation");
    }
    
    [ContextMenu("Test Complete Breathing Cycle")]
    public void TestCompleteBreathingCycle()
    {
        StartCoroutine(TestBreathingCycleCoroutine());
    }
    
    [ContextMenu("Reset to Base")]
    public void ResetToBase()
    {
        currentYPosition = baseYPosition;
        targetYPosition = baseYPosition;
        currentColor = noDataColor;
        targetColor = noDataColor;
        currentPhase = "";
        currentState = BreathingState.NoData;
        
        // Reset to constant scale
        transform.localScale = Vector3.one * constantScale;
        
        transform.position = new Vector3(startPosition.x, startPosition.y + currentYPosition, startPosition.z);
        if (sphereRenderer != null)
        {
            sphereRenderer.material.color = currentColor;
        }
        
        UpdateUI("", 0f);
        Debug.Log("🔄 Reset to base state");
    }
    
    // Test coroutine for complete breathing cycle
    System.Collections.IEnumerator TestBreathingCycleCoroutine()
    {
        Debug.Log("🧪 Starting complete breathing cycle test...");
        
        // Inhale
        currentPhase = "inhale";
        UpdateBreathingState("inhale");
        Debug.Log("🫁 Phase: Inhale");
        yield return new WaitForSeconds(2f);
        
        // Inhale Hold
        currentPhase = "inhale_hold";
        UpdateBreathingState("inhale_hold");
        Debug.Log("🫁 Phase: Inhale Hold");
        yield return new WaitForSeconds(1f);
        
        // Exhale
        currentPhase = "exhale";
        UpdateBreathingState("exhale");
        Debug.Log("🫁 Phase: Exhale");
        yield return new WaitForSeconds(3f);
        
        // Exhale Hold
        currentPhase = "exhale_hold";
        UpdateBreathingState("exhale_hold");
        Debug.Log("🫁 Phase: Exhale Hold");
        yield return new WaitForSeconds(1f);
        
        Debug.Log("🧪 Complete breathing cycle test finished");
    }
    
    // Inspector display
    void OnValidate()
    {
        if (showCurrentValues)
        {
            // This will show current values in the inspector during runtime
        }
    }
}
