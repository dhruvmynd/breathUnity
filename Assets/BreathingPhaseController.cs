using System;
using UnityEngine;

public class BreathingPhaseController : MonoBehaviour
{
    public RespReceiver receiver;     // assign your UDP receiver
    public Transform sphere;          // assign the sphere to scale
    
    [Header("Camera Settings")]
    [Tooltip("Camera to center sphere around (auto-finds if not set)")]
    public Camera targetCamera;       // camera to center sphere on
    [Tooltip("Keep sphere centered on camera (user inside sphere)")]
    public bool centerOnCamera = true;
    [Tooltip("Move camera with breathing (first-person levitation feeling)")]
    public bool moveCameraWithBreathing = false;

    [Header("Smoothing")]
    [Tooltip("Seconds for exponential smoothing of force.")]
    public float smoothingSeconds = 0.25f;   // EMA time-constant
    [Tooltip("Clamp for derivative noise (absolute)")]
    public float derivativeClamp = 10f;      // sanity clamp

    [Header("Phase Detection")]
    [Tooltip("Derivative threshold to detect inhale start (units/sec).")]
    public float dInhale = 0.05f;
    [Tooltip("Derivative threshold to detect exhale start (units/sec).")]
    public float dExhale = -0.05f;
    [Tooltip("If |derivative| stays below this for holdDwell seconds => HOLD.")]
    public float dQuiet = 0.01f;
    [Tooltip("Seconds derivative must remain quiet to call it a hold.")]
    public float holdDwell = 0.30f;
    [Tooltip("Minimum time per phase to reduce flicker.")]
    public float minPhaseSeconds = 0.35f;

    [Header("Auto-Range (normalization)")]
    [Tooltip("How quickly low/high follow the signal upward.")]
    public float rangeFollowUp = 0.3f;   // faster track upward
    [Tooltip("How quickly low/high relax back when not reached.")]
    public float rangeRelaxDown = 0.02f; // slow decay toward center
    [Tooltip("Extra padding on min/max to prevent clipping.")]
    public float rangePadding = 0.1f;

    [Header("Sphere Settings")]
    public float constantScale = 6.0f;   // fixed size for the sphere (larger for user to be inside)
    public float baseYPosition = 0f;     // vertical offset from camera (0 = camera at center)
    public float positionRange = 3.5f;   // how far to move up/down from base (increased for more range)
    public float sensitivity = 2.0f;     // sensitivity multiplier for position movement (higher = more responsive)
    [Tooltip("Invert breathing direction: true = inhale goes down, false = inhale goes up")]
    public bool invertDirection = true;  // toggle breathing direction
    [Tooltip("Smoothing for visual position (seconds).")]
    public float visualLerpSeconds = 0.1f;

    [Header("Debug (read-only)")]
    public float rawForce;
    public float smoothForce;
    public float derivative;
    public float normalized;             // 0..1 after auto-range
    public string phase = "InhaleHold";

    enum Phase { Inhale, InhaleHold, Exhale, ExhaleHold }
    Phase current = Phase.InhaleHold;

    float emaForce;
    float prevEma;
    float quietTimer;
    float phaseTimer;
    float lowTrack, highTrack;
    float targetYPosition;
    Vector3 startPosition;
    float cameraStartY;  // Store camera's initial Y position
    Transform cameraParent;  // Store camera's parent (XR Rig, etc.)

    void Start()
    {
        emaForce = 0f; prevEma = 0f;
        lowTrack = -0.5f;   // initial guesses; they'll adapt quickly
        highTrack = 0.5f;
        targetYPosition = baseYPosition;
        
        // Find camera if not assigned
        if (centerOnCamera && targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                // Try to find any camera in the scene
                targetCamera = FindObjectOfType<Camera>();
            }
            
            if (targetCamera != null)
            {
                Debug.Log($"🎥 BreathingPhaseController: Auto-found camera '{targetCamera.name}'");
            }
            else
            {
                Debug.LogWarning("🎥 BreathingPhaseController: No camera found! Sphere will not center on camera.");
                centerOnCamera = false;
            }
        }
        
        // Store camera's initial Y position and parent
        if (targetCamera != null)
        {
            cameraStartY = targetCamera.transform.position.y;
            cameraParent = targetCamera.transform.parent;
            Debug.Log($"🎥 Camera initial Y: {cameraStartY}, Parent: {(cameraParent ? cameraParent.name : "none")}");
        }
        
        // Store initial position and set constant scale
        if (sphere)
        {
            startPosition = sphere.position;
            sphere.localScale = Vector3.one * constantScale;
            
            // If centering on camera, initialize position
            if (centerOnCamera && targetCamera != null)
            {
                startPosition = targetCamera.transform.position;
                Debug.Log($"🫁 Sphere will be centered on camera at {startPosition}");
            }
        }
    }

    void Update()
    {
        // 1) Pull latest force from the UDP receiver
        rawForce = receiver != null ? receiver.forceN : 0f;

        // 2) EMA smoothing (continuous-time style)
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float alpha = 1f - Mathf.Exp(-dt / Mathf.Max(1e-3f, smoothingSeconds));
        emaForce = Mathf.Lerp(emaForce, rawForce, alpha);
        smoothForce = emaForce;

        // 3) Derivative (clamped for robustness)
        float d = (emaForce - prevEma) / dt;
        derivative = Mathf.Clamp(d, -derivativeClamp, derivativeClamp);
        prevEma = emaForce;

        // 4) Auto-range tracking for normalization
        //    Track high when signal rises fast; relax slowly otherwise.
        if (emaForce > highTrack) highTrack = Mathf.Lerp(highTrack, emaForce, rangeFollowUp * dt);
        else                      highTrack = Mathf.Lerp(highTrack, highTrack - rangePadding, rangeRelaxDown * dt);

        if (emaForce < lowTrack)  lowTrack  = Mathf.Lerp(lowTrack,  emaForce, rangeFollowUp * dt);
        else                      lowTrack  = Mathf.Lerp(lowTrack,  lowTrack + rangePadding, rangeRelaxDown * dt);

        float lo = lowTrack - rangePadding;
        float hi = highTrack + rangePadding;
        if (hi - lo < 1e-3f) { lo -= 0.5f; hi += 0.5f; } // avoid divide-by-zero
        normalized = Mathf.InverseLerp(lo, hi, emaForce);

        // 5) Phase state machine with hysteresis + dwell
        phaseTimer += dt;
        bool canSwitch = phaseTimer >= minPhaseSeconds;

        // Quiet detection (for holds)
        if (Mathf.Abs(derivative) < dQuiet) quietTimer += dt;
        else quietTimer = 0f;

        switch (current)
        {
            case Phase.Inhale:
                if (canSwitch && derivative < dExhale) { Switch(Phase.Exhale); break; }
                if (canSwitch && quietTimer >= holdDwell) { Switch(Phase.InhaleHold); }
                break;

            case Phase.InhaleHold:
                if (canSwitch && derivative > dInhale) { Switch(Phase.Inhale); break; }
                if (canSwitch && derivative < dExhale) { Switch(Phase.Exhale); break; }
                // stay holding while quiet
                break;

            case Phase.Exhale:
                if (canSwitch && derivative > dInhale) { Switch(Phase.Inhale); break; }
                if (canSwitch && quietTimer >= holdDwell) { Switch(Phase.ExhaleHold); }
                break;

            case Phase.ExhaleHold:
                if (canSwitch && derivative < dExhale) { Switch(Phase.Exhale); break; }
                if (canSwitch && derivative > dInhale) { Switch(Phase.Inhale); break; }
                // stay holding while quiet
                break;
        }

        // 6) Drive the sphere position (move up on inhale, hold position, move down on exhale)
        // Map phase to a target normalized level:
        float targetLevel = normalized;

        // Optionally "snap" during holds to make it visibly steady:
        if (current == Phase.InhaleHold)    targetLevel = Mathf.Max(targetLevel, normalized);
        if (current == Phase.ExhaleHold)    targetLevel = Mathf.Min(targetLevel, normalized);

        // Calculate breathing offset
        float lerpA = 1f - Mathf.Exp(-dt / Mathf.Max(1e-3f, visualLerpSeconds));
        
        // Map normalized (0..1) to Y position with optional inversion
        float offset = (targetLevel - 0.5f) * 2f; // Map 0..1 to -1..1
        float directionMultiplier = invertDirection ? -1f : 1f; // Apply inversion if enabled
        targetYPosition = baseYPosition + offset * positionRange * sensitivity * directionMultiplier;

        // Move camera with breathing if enabled
        if (moveCameraWithBreathing && targetCamera != null)
        {
            // Determine which transform to move (parent for XR rigs, camera for standalone)
            Transform transformToMove = cameraParent != null ? cameraParent : targetCamera.transform;
            
            // Calculate target Y position (initial Y + breathing offset)
            float targetCameraY = cameraStartY + targetYPosition;
            float currentCameraY = transformToMove.position.y;
            float newCameraY = Mathf.Lerp(currentCameraY, targetCameraY, lerpA);
            
            // Update Y position while maintaining X and Z
            Vector3 pos = transformToMove.position;
            transformToMove.position = new Vector3(pos.x, newCameraY, pos.z);
        }

        // Update sphere position
        if (sphere)
        {
            if (centerOnCamera && targetCamera != null)
            {
                // Sphere follows camera X/Z (horizontal centering) but moves up/down with breathing
                Vector3 cameraPos = targetCamera.transform.position;
                float currentY = sphere.position.y;
                // Sphere Y = camera Y + breathing offset (camera stays fixed, sphere moves)
                float targetY = cameraPos.y + targetYPosition;
                float newY = Mathf.Lerp(currentY, targetY, lerpA);
                
                // Center on camera X/Z, but use breathing Y position
                sphere.position = new Vector3(cameraPos.x, newY, cameraPos.z);
            }
            else
            {
                // Move sphere with breathing offset from start position
                float currentY = sphere.position.y;
                float targetY = startPosition.y + targetYPosition;
                float newY = Mathf.Lerp(currentY, targetY, lerpA);
                sphere.position = new Vector3(sphere.position.x, newY, sphere.position.z);
            }
        }

        // 7) Expose human-readable phase
        phase = current.ToString();
    }

    void Switch(Phase next)
    {
        current = next;
        quietTimer = 0f;
        phaseTimer = 0f;
    }
}
