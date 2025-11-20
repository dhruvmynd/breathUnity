using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TransformMotionDetectorDebugManager : MonoBehaviour
{
    [Header("Motion Detector")]
    [SerializeField] private TransformMotionDetector motionDetector;
    
    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI upText;
    [SerializeField] private TextMeshProUGUI downText;
    [SerializeField] private TextMeshProUGUI leftText;
    [SerializeField] private TextMeshProUGUI rightText;
    [SerializeField] private TextMeshProUGUI forwardText;
    [SerializeField] private TextMeshProUGUI backwardText;
    
    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.white;
    
    [Header("Debug")]
    [SerializeField] private bool logEvents = true;
    
    private Dictionary<MotionDirection, TextMeshProUGUI> directionTexts;

    private void Start()
    {
        if (motionDetector == null)
            motionDetector = GetComponent<TransformMotionDetector>();

        // Initialize direction-to-text mapping
        directionTexts = new Dictionary<MotionDirection, TextMeshProUGUI>
        {
            { MotionDirection.Up, upText },
            { MotionDirection.Down, downText },
            { MotionDirection.Left, leftText },
            { MotionDirection.Right, rightText },
            { MotionDirection.Forward, forwardText },
            { MotionDirection.Backward, backwardText }
        };

        // Set initial text labels and colors
        SetupTextLabels();

        if (motionDetector != null)
        {
            // Subscribe to enter events
            motionDetector.motionEvents.onEnterUp.AddListener((velocity) => OnDirectionEnter(MotionDirection.Up, velocity));
            motionDetector.motionEvents.onEnterDown.AddListener((velocity) => OnDirectionEnter(MotionDirection.Down, velocity));
            motionDetector.motionEvents.onEnterLeft.AddListener((velocity) => OnDirectionEnter(MotionDirection.Left, velocity));
            motionDetector.motionEvents.onEnterRight.AddListener((velocity) => OnDirectionEnter(MotionDirection.Right, velocity));
            motionDetector.motionEvents.onEnterForward.AddListener((velocity) => OnDirectionEnter(MotionDirection.Forward, velocity));
            motionDetector.motionEvents.onEnterBackward.AddListener((velocity) => OnDirectionEnter(MotionDirection.Backward, velocity));

            // Subscribe to leave events
            motionDetector.motionEvents.onLeaveUp.AddListener((velocity) => OnDirectionLeave(MotionDirection.Up, velocity));
            motionDetector.motionEvents.onLeaveDown.AddListener((velocity) => OnDirectionLeave(MotionDirection.Down, velocity));
            motionDetector.motionEvents.onLeaveLeft.AddListener((velocity) => OnDirectionLeave(MotionDirection.Left, velocity));
            motionDetector.motionEvents.onLeaveRight.AddListener((velocity) => OnDirectionLeave(MotionDirection.Right, velocity));
            motionDetector.motionEvents.onLeaveForward.AddListener((velocity) => OnDirectionLeave(MotionDirection.Forward, velocity));
            motionDetector.motionEvents.onLeaveBackward.AddListener((velocity) => OnDirectionLeave(MotionDirection.Backward, velocity));
        }
    }

    private void SetupTextLabels()
    {
        if (upText != null) { upText.text = "UP"; upText.color = inactiveColor; }
        if (downText != null) { downText.text = "DOWN"; downText.color = inactiveColor; }
        if (leftText != null) { leftText.text = "LEFT"; leftText.color = inactiveColor; }
        if (rightText != null) { rightText.text = "RIGHT"; rightText.color = inactiveColor; }
        if (forwardText != null) { forwardText.text = "FORWARD"; forwardText.color = inactiveColor; }
        if (backwardText != null) { backwardText.text = "BACKWARD"; backwardText.color = inactiveColor; }
    }

    private void OnDirectionEnter(MotionDirection direction, Vector3 velocity)
    {
        if (logEvents)
        {
            Debug.Log($"[MotionDebug] ENTER {direction} - Velocity: {velocity} (Magnitude: {velocity.magnitude:F3})");
        }

        // Update text color to active
        if (directionTexts.ContainsKey(direction) && directionTexts[direction] != null)
        {
            directionTexts[direction].color = activeColor;
        }
    }

    private void OnDirectionLeave(MotionDirection direction, Vector3 velocity)
    {
        if (logEvents)
        {
            Debug.Log($"[MotionDebug] LEAVE {direction} - Velocity: {velocity} (Magnitude: {velocity.magnitude:F3})");
        }

        // Update text color to inactive
        if (directionTexts.ContainsKey(direction) && directionTexts[direction] != null)
        {
            directionTexts[direction].color = inactiveColor;
        }
    }
} 