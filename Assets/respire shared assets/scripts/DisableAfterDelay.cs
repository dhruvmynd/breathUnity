using UnityEngine;

/// <summary>
/// Disables the attached GameObject after a specified delay.
/// </summary>
public class DisableAfterDelay : MonoBehaviour
{
    [Tooltip("Time in seconds before the GameObject is disabled")]
    [SerializeField, Range(0.1f, 60f)] private float delay = 3f;
    
    [Tooltip("Whether to start the countdown automatically on Start")]
    [SerializeField] private bool countdownOnStart = true;
    
    private float remainingTime;
    private bool isCountingDown = false;
    
    public float Delay
    {
        get => delay;
        set => delay = Mathf.Max(0.1f, value);
    }
    
    public bool DisableOnStart
    {
        get => countdownOnStart;
        set => countdownOnStart = value;
    }
    
    private void Start()
    {
        if (countdownOnStart)
        {
            BeginCountdown();
        }
    }
    
    private void Update()
    {
        if (isCountingDown)
        {
            remainingTime -= Time.deltaTime;
            
            if (remainingTime <= 0f)
            {
                DisableGameObject();
            }
        }
    }
    
    /// <summary>
    /// Starts the countdown to disable the GameObject
    /// </summary>
    public void BeginCountdown()
    {
        remainingTime = delay;
        isCountingDown = true;
    }
    
    /// <summary>
    /// Starts the countdown with a custom delay time
    /// </summary>
    /// <param name="customDelay">Custom time in seconds before disabling</param>
    public void BeginCountdown(float customDelay)
    {
        delay = Mathf.Max(0.1f, customDelay);
        BeginCountdown();
    }
    
    /// <summary>
    /// Cancels the current countdown if one is active
    /// </summary>
    public void CancelCountdown()
    {
        isCountingDown = false;
    }
    
    /// <summary>
    /// Disables the GameObject immediately
    /// </summary>
    public void DisableGameObject()
    {
        isCountingDown = false;
        gameObject.SetActive(false);
    }
} 