using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// Breathing Score Calculator - Tracks how well the user follows breathing guidance
/// Calculates real-time score based on timing accuracy and phase synchronization
/// </summary>
public class BreathingScoreCalculator : MonoBehaviour
{
    [Header("Score Settings")]
    [Tooltip("Maximum possible score")]
    public int maxScore = 100;
    
    [Tooltip("Minimum score threshold for 'good' breathing")]
    public int goodScoreThreshold = 70;
    
    [Tooltip("Excellent score threshold")]
    public int excellentScoreThreshold = 85;
    
    [Tooltip("How many recent scores to average for smooth display")]
    public int scoreSmoothingCount = 10;
    
    [Header("Timing Tolerance")]
    [Tooltip("Tolerance for phase timing accuracy (seconds)")]
    public float timingTolerance = 0.5f;
    
    [Tooltip("Tolerance for phase transitions (seconds)")]
    public float transitionTolerance = 0.3f;
    
    [Header("Scoring Weights")]
    [Tooltip("Weight for timing accuracy (0-1)")]
    [Range(0f, 1f)]
    public float timingWeight = 0.4f;
    
    [Tooltip("Weight for phase synchronization (0-1)")]
    [Range(0f, 1f)]
    public float phaseSyncWeight = 0.3f;
    
    [Tooltip("Weight for consistency (0-1)")]
    [Range(0f, 1f)]
    public float consistencyWeight = 0.3f;
    
    [Header("UI Display")]
    [Tooltip("Text component for current score")]
    public TextMeshProUGUI scoreText;
    
    [Tooltip("Text component for score feedback")]
    public TextMeshProUGUI feedbackText;
    
    [Tooltip("Text component for session summary")]
    public TextMeshProUGUI summaryText;
    
    [Tooltip("Color for excellent scores")]
    public Color excellentColor = Color.green;
    
    [Tooltip("Color for good scores")]
    public Color goodColor = Color.yellow;
    
    [Tooltip("Color for poor scores")]
    public Color poorColor = Color.red;
    
    [Header("Debug")]
    [Tooltip("Show detailed debug information")]
    public bool showDebugInfo = true;
    
    // Internal state
    private float currentScore = 0f;
    private float smoothedScore = 0f;
    private Queue<float> recentScores = new Queue<float>();
    
    // Session tracking
    private List<BreathingCycle> breathingCycles = new List<BreathingCycle>();
    private BreathingCycle currentCycle;
    private bool sessionActive = false;
    private float sessionStartTime = 0f;
    private float totalSessionTime = 0f;
    
    // References
    private BreathingPhaseAnimator phaseAnimator;
    private UDPHeartRateReceiver udpReceiver;
    
    // Score calculation data
    private float lastPhaseChangeTime = 0f;
    private string lastExpectedPhase = "";
    private float phaseStartTime = 0f;
    private float expectedPhaseDuration = 0f;
    
    // Consistency tracking
    private List<float> recentTimingErrors = new List<float>();
    private List<float> recentPhaseSyncErrors = new List<float>();
    
    [System.Serializable]
    public class BreathingCycle
    {
        public float startTime;
        public float endTime;
        public float duration;
        public List<PhaseData> phases = new List<PhaseData>();
        public float cycleScore;
        public string cycleFeedback;
        
        public BreathingCycle()
        {
            startTime = Time.time;
        }
        
        public void EndCycle()
        {
            endTime = Time.time;
            duration = endTime - startTime;
        }
    }
    
    [System.Serializable]
    public class PhaseData
    {
        public string phase;
        public float startTime;
        public float duration;
        public float expectedDuration;
        public float timingAccuracy; // 0-1, higher is better
        public float phaseSyncScore; // 0-1, higher is better
        public string feedback;
    }
    
    void Start()
    {
        // Find required components
        phaseAnimator = FindObjectOfType<BreathingPhaseAnimator>();
        udpReceiver = FindObjectOfType<UDPHeartRateReceiver>();
        
        if (phaseAnimator == null)
        {
            Debug.LogError("BreathingScoreCalculator: BreathingPhaseAnimator not found!");
        }
        
        if (udpReceiver == null)
        {
            Debug.LogError("BreathingScoreCalculator: UDPHeartRateReceiver not found!");
        }
        
        // Initialize UI
        SetupUI();
        
        // Start first cycle
        StartNewCycle();
        
        Debug.Log("📊 Breathing Score Calculator initialized");
    }
    
    void SetupUI()
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
    }
    
    void Update()
    {
        if (udpReceiver == null || phaseAnimator == null) return;
        
        // Check if we have breathing data
        if (!udpReceiver.gotPacket || string.IsNullOrEmpty(udpReceiver.breathingPhase))
        {
            if (sessionActive)
            {
                EndSession();
            }
            return;
        }
        
        // Start session if not active
        if (!sessionActive)
        {
            StartSession();
        }
        
        // Update current cycle
        UpdateCurrentCycle();
        
        // Calculate real-time score
        CalculateRealTimeScore();
        
        // Update UI
        UpdateScoreUI();
        
        // Debug output
        if (showDebugInfo && Time.time % 3f < Time.deltaTime)
        {
            Debug.Log($"📊 Score: {smoothedScore:F1} | Phase: {udpReceiver.breathingPhase} | Cycle: {currentCycle?.phases.Count ?? 0}");
        }
    }
    
    void StartSession()
    {
        sessionActive = true;
        sessionStartTime = Time.time;
        breathingCycles.Clear();
        StartNewCycle();
        
        Debug.Log("🫁 Breathing session started");
    }
    
    void EndSession()
    {
        if (!sessionActive) return;
        
        sessionActive = false;
        totalSessionTime = Time.time - sessionStartTime;
        
        // End current cycle
        if (currentCycle != null)
        {
            EndCurrentCycle();
        }
        
        // Generate session summary
        GenerateSessionSummary();
        
        Debug.Log($"🫁 Breathing session ended. Duration: {totalSessionTime:F1}s, Cycles: {breathingCycles.Count}");
    }
    
    void StartNewCycle()
    {
        currentCycle = new BreathingCycle();
        lastPhaseChangeTime = Time.time;
        lastExpectedPhase = "";
        
        Debug.Log("🫁 New breathing cycle started");
    }
    
    void EndCurrentCycle()
    {
        if (currentCycle == null) return;
        
        currentCycle.EndCycle();
        CalculateCycleScore();
        breathingCycles.Add(currentCycle);
        
        Debug.Log($"🫁 Cycle ended. Score: {currentCycle.cycleScore:F1}, Phases: {currentCycle.phases.Count}");
    }
    
    void UpdateCurrentCycle()
    {
        if (currentCycle == null) return;
        
        string currentPhase = udpReceiver.breathingPhase;
        float phaseDuration = udpReceiver.phaseDuration;
        
        // Check if phase changed
        if (currentPhase != lastExpectedPhase)
        {
            // Add previous phase data if exists
            if (!string.IsNullOrEmpty(lastExpectedPhase))
            {
                AddPhaseData(lastExpectedPhase, phaseStartTime, Time.time - phaseStartTime);
            }
            
            // Start new phase
            lastExpectedPhase = currentPhase;
            phaseStartTime = Time.time;
            expectedPhaseDuration = phaseDuration;
            lastPhaseChangeTime = Time.time;
        }
    }
    
    void AddPhaseData(string phase, float startTime, float actualDuration)
    {
        if (currentCycle == null) return;
        
        PhaseData phaseData = new PhaseData
        {
            phase = phase,
            startTime = startTime,
            duration = actualDuration,
            expectedDuration = expectedPhaseDuration,
            timingAccuracy = CalculateTimingAccuracy(actualDuration, expectedPhaseDuration),
            phaseSyncScore = CalculatePhaseSyncScore(phase, actualDuration),
            feedback = GeneratePhaseFeedback(phase, actualDuration, expectedPhaseDuration)
        };
        
        currentCycle.phases.Add(phaseData);
        
        if (showDebugInfo)
        {
            Debug.Log($"📊 Phase: {phase} | Duration: {actualDuration:F1}s/{expectedPhaseDuration:F1}s | Accuracy: {phaseData.timingAccuracy:F2}");
        }
    }
    
    float CalculateTimingAccuracy(float actualDuration, float expectedDuration)
    {
        if (expectedDuration <= 0) return 0f;
        
        float error = Mathf.Abs(actualDuration - expectedDuration);
        float tolerance = timingTolerance;
        
        // Calculate accuracy as percentage within tolerance
        float accuracy = Mathf.Clamp01(1f - (error / tolerance));
        
        // Add to recent errors for consistency calculation
        recentTimingErrors.Add(error);
        if (recentTimingErrors.Count > 20) // Keep last 20 measurements
        {
            recentTimingErrors.RemoveAt(0);
        }
        
        return accuracy;
    }
    
    float CalculatePhaseSyncScore(string phase, float duration)
    {
        // This could be enhanced to compare with expected breathing patterns
        // For now, we'll use a simple scoring based on phase appropriateness
        
        float baseScore = 0.8f; // Base score for following any phase
        
        // Adjust based on phase type
        switch (phase.ToLower())
        {
            case "inhale":
                baseScore = 0.9f;
                break;
            case "exhale":
                baseScore = 0.9f;
                break;
            case "hold":
            case "inhale_hold":
            case "exhale_hold":
                baseScore = 0.7f; // Holds are harder to time perfectly
                break;
            default:
                baseScore = 0.6f;
                break;
        }
        
        // Add to recent sync errors
        recentPhaseSyncErrors.Add(1f - baseScore);
        if (recentPhaseSyncErrors.Count > 20)
        {
            recentPhaseSyncErrors.RemoveAt(0);
        }
        
        return baseScore;
    }
    
    string GeneratePhaseFeedback(string phase, float actualDuration, float expectedDuration)
    {
        float error = Mathf.Abs(actualDuration - expectedDuration);
        
        if (error < timingTolerance * 0.5f)
        {
            return "Perfect timing!";
        }
        else if (error < timingTolerance)
        {
            return "Good timing";
        }
        else if (error < timingTolerance * 1.5f)
        {
            return "Close, keep trying";
        }
        else
        {
            return "Try to follow more closely";
        }
    }
    
    void CalculateRealTimeScore()
    {
        if (currentCycle == null || currentCycle.phases.Count == 0)
        {
            currentScore = 0f;
            return;
        }
        
        float timingScore = 0f;
        float phaseSyncScore = 0f;
        float consistencyScore = 0f;
        
        // Calculate timing score from recent phases
        if (currentCycle.phases.Count > 0)
        {
            float totalTimingAccuracy = 0f;
            foreach (var phase in currentCycle.phases)
            {
                totalTimingAccuracy += phase.timingAccuracy;
            }
            timingScore = totalTimingAccuracy / currentCycle.phases.Count;
        }
        
        // Calculate phase sync score
        if (currentCycle.phases.Count > 0)
        {
            float totalPhaseSync = 0f;
            foreach (var phase in currentCycle.phases)
            {
                totalPhaseSync += phase.phaseSyncScore;
            }
            phaseSyncScore = totalPhaseSync / currentCycle.phases.Count;
        }
        
        // Calculate consistency score
        consistencyScore = CalculateConsistencyScore();
        
        // Weighted final score
        currentScore = (timingScore * timingWeight) + 
                      (phaseSyncScore * phaseSyncWeight) + 
                      (consistencyScore * consistencyWeight);
        
        currentScore *= maxScore;
        
        // Smooth the score
        recentScores.Enqueue(currentScore);
        if (recentScores.Count > scoreSmoothingCount)
        {
            recentScores.Dequeue();
        }
        
        // Calculate smoothed score
        float totalScore = 0f;
        foreach (float score in recentScores)
        {
            totalScore += score;
        }
        smoothedScore = totalScore / recentScores.Count;
    }
    
    float CalculateConsistencyScore()
    {
        if (recentTimingErrors.Count < 3) return 0.5f; // Not enough data
        
        // Calculate variance in timing errors (lower variance = higher consistency)
        float meanError = 0f;
        foreach (float error in recentTimingErrors)
        {
            meanError += error;
        }
        meanError /= recentTimingErrors.Count;
        
        float variance = 0f;
        foreach (float error in recentTimingErrors)
        {
            variance += Mathf.Pow(error - meanError, 2);
        }
        variance /= recentTimingErrors.Count;
        
        // Convert variance to consistency score (lower variance = higher score)
        float consistency = Mathf.Clamp01(1f - (variance / timingTolerance));
        
        return consistency;
    }
    
    void CalculateCycleScore()
    {
        if (currentCycle == null || currentCycle.phases.Count == 0)
        {
            currentCycle.cycleScore = 0f;
            currentCycle.cycleFeedback = "No breathing data";
            return;
        }
        
        float totalScore = 0f;
        foreach (var phase in currentCycle.phases)
        {
            totalScore += (phase.timingAccuracy + phase.phaseSyncScore) / 2f;
        }
        
        currentCycle.cycleScore = (totalScore / currentCycle.phases.Count) * maxScore;
        
        // Generate cycle feedback
        if (currentCycle.cycleScore >= excellentScoreThreshold)
        {
            currentCycle.cycleFeedback = "Excellent breathing!";
        }
        else if (currentCycle.cycleScore >= goodScoreThreshold)
        {
            currentCycle.cycleFeedback = "Good breathing rhythm";
        }
        else
        {
            currentCycle.cycleFeedback = "Keep practicing";
        }
    }
    
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {smoothedScore:F0}";
            
            // Color based on score
            if (smoothedScore >= excellentScoreThreshold)
            {
                scoreText.color = excellentColor;
            }
            else if (smoothedScore >= goodScoreThreshold)
            {
                scoreText.color = goodColor;
            }
            else
            {
                scoreText.color = poorColor;
            }
        }
        
        if (feedbackText != null)
        {
            if (currentCycle != null && currentCycle.phases.Count > 0)
            {
                var lastPhase = currentCycle.phases[currentCycle.phases.Count - 1];
                feedbackText.text = lastPhase.feedback;
            }
            else
            {
                feedbackText.text = "Follow the breathing guidance";
            }
        }
    }
    
    void GenerateSessionSummary()
    {
        if (summaryText == null || breathingCycles.Count == 0) return;
        
        float totalScore = 0f;
        int totalPhases = 0;
        float totalTime = 0f;
        
        foreach (var cycle in breathingCycles)
        {
            totalScore += cycle.cycleScore;
            totalPhases += cycle.phases.Count;
            totalTime += cycle.duration;
        }
        
        float averageScore = totalScore / breathingCycles.Count;
        float averageCycleTime = totalTime / breathingCycles.Count;
        
        string summary = $"Session Summary:\n";
        summary += $"Duration: {totalSessionTime:F1}s\n";
        summary += $"Cycles: {breathingCycles.Count}\n";
        summary += $"Average Score: {averageScore:F1}\n";
        summary += $"Total Phases: {totalPhases}\n";
        summary += $"Avg Cycle Time: {averageCycleTime:F1}s\n";
        
        if (averageScore >= excellentScoreThreshold)
        {
            summary += "Overall: Excellent! 🌟";
        }
        else if (averageScore >= goodScoreThreshold)
        {
            summary += "Overall: Good job! 👍";
        }
        else
        {
            summary += "Overall: Keep practicing! 💪";
        }
        
        summaryText.text = summary;
    }
    
    // Public methods for external access
    public float GetCurrentScore() => smoothedScore;
    public float GetRawScore() => currentScore;
    public bool IsSessionActive() => sessionActive;
    public int GetCycleCount() => breathingCycles.Count;
    public float GetSessionDuration() => sessionActive ? Time.time - sessionStartTime : totalSessionTime;
    
    // Public property for accessing current cycle
    public BreathingCycle CurrentCycle => currentCycle;
    
    // Manual control methods
    [ContextMenu("Start New Session")]
    public void StartNewSession()
    {
        EndSession();
        StartSession();
    }
    
    [ContextMenu("End Current Session")]
    public void EndCurrentSession()
    {
        EndSession();
    }
    
    [ContextMenu("Reset All Data")]
    public void ResetAllData()
    {
        EndSession();
        recentScores.Clear();
        recentTimingErrors.Clear();
        recentPhaseSyncErrors.Clear();
        currentScore = 0f;
        smoothedScore = 0f;
        SetupUI();
        Debug.Log("📊 All breathing score data reset");
    }
}
