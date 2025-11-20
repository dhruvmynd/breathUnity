using UnityEngine;

public class TimeController : MonoBehaviour
{
    [Tooltip("Current time scale (1 = normal, <1 = slower, >1 = faster)")]
    [Range(0.1f, 10f)]
    public float timeScale = 1.0f;

    [Tooltip("Time scale presets for quick access")]
    public float[] timeScalePresets = { 0.25f, 0.5f, 1.0f, 2.0f, 5.0f };

    private float defaultFixedDeltaTime;

    private void Awake()
    {
        // Store the default fixedDeltaTime for proper physics calculation
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    private void Update()
    {
        // Apply the time scale
        SetTimeScale(timeScale);
    }

    /// <summary>
    /// Sets the game time scale to the specified value
    /// </summary>
    /// <param name="scale">Target time scale value</param>
    public void SetTimeScale(float scale)
    {
        // Update only if there's a change
        if (Time.timeScale != scale)
        {
            Time.timeScale = scale;
            // Adjust fixed delta time to maintain physics behavior
            Time.fixedDeltaTime = defaultFixedDeltaTime * scale;
        }
    }

    /// <summary>
    /// Sets the time scale to the specified preset index
    /// </summary>
    /// <param name="presetIndex">Index into the timeScalePresets array</param>
    public void SetTimeScalePreset(int presetIndex)
    {
        if (presetIndex >= 0 && presetIndex < timeScalePresets.Length)
        {
            timeScale = timeScalePresets[presetIndex];
        }
        else
        {
            Debug.LogWarning($"Invalid preset index: {presetIndex}. Valid range is 0-{timeScalePresets.Length - 1}");
        }
    }

    /// <summary>
    /// Reset time to normal (1.0) speed
    /// </summary>
    public void ResetTimeScale()
    {
        timeScale = 1.0f;
    }

    // Ensure we reset time scale when script is disabled or destroyed
    private void OnDisable()
    {
        // Reset to normal time when this component is disabled
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
} 