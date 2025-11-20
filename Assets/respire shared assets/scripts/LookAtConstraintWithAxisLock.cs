using UnityEngine;

/// <summary>
/// A component that makes an object look at a target with the ability to lock specific axes.
/// </summary>
public class LookAtConstraintWithAxisLock : MonoBehaviour
{
    [Tooltip("The target to look at")]
    [SerializeField] private Transform _target;

    [Tooltip("Lock rotation around the X axis")]
    [SerializeField] private bool _lockXAxis = false;

    [Tooltip("Lock rotation around the Y axis")]
    [SerializeField] private bool _lockYAxis = false;

    [Tooltip("Lock rotation around the Z axis")]
    [SerializeField] private bool _lockZAxis = false;

    [Tooltip("Weight of the look at effect (0-1)")]
    [Range(0f, 1f)]
    [SerializeField] private float _weight = 1.0f;

    [Tooltip("Use local space for rotation calculations")]
    [SerializeField] private bool _useLocalSpace = false;

    [Tooltip("Forward vector for the look direction")]
    [SerializeField] private Vector3 _worldUp = Vector3.up;

    // The original rotation to blend with
    private Quaternion _originalRotation;

    // Public property for the target
    public Transform Target
    {
        get => _target;
        set => _target = value;
    }

    // Public property for the weight
    public float Weight
    {
        get => _weight;
        set => _weight = Mathf.Clamp01(value);
    }

    private void Awake()
    {
        // Store the initial rotation
        _originalRotation = transform.rotation;
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        // Calculate the rotation needed to look at the target
        Quaternion targetRotation = CalculateLookAtRotation();

        // Apply the rotation with weight
        transform.rotation = Quaternion.Slerp(_originalRotation, targetRotation, _weight);
    }

    private Quaternion CalculateLookAtRotation()
    {
        // Determine the look direction
        Vector3 lookDirection = _target.position - transform.position;
        
        // Skip if the look direction is too small
        if (lookDirection.sqrMagnitude < Mathf.Epsilon)
            return transform.rotation;

        // Calculate the rotation to look at the target
        Quaternion lookRotation = Quaternion.LookRotation(lookDirection, _worldUp);
        
        // Get the current rotation (local or world space)
        Quaternion currentRotation = _useLocalSpace ? transform.localRotation : transform.rotation;
        
        // Convert to Euler angles for axis locking
        Vector3 targetEuler = lookRotation.eulerAngles;
        Vector3 currentEuler = currentRotation.eulerAngles;
        
        // Lock axes as specified
        if (_lockXAxis) targetEuler.x = currentEuler.x;
        if (_lockYAxis) targetEuler.y = currentEuler.y;
        if (_lockZAxis) targetEuler.z = currentEuler.z;
        
        // Convert back to quaternion
        return Quaternion.Euler(targetEuler);
    }

    /// <summary>
    /// Set which axes to lock or unlock
    /// </summary>
    public void SetAxisLock(bool lockX, bool lockY, bool lockZ)
    {
        _lockXAxis = lockX;
        _lockYAxis = lockY;
        _lockZAxis = lockZ;
    }

    /// <summary>
    /// Reset to the original rotation
    /// </summary>
    public void ResetRotation()
    {
        transform.rotation = _originalRotation;
    }
} 