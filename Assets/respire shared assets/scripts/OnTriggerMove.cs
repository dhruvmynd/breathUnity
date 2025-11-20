using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class OnTriggerMove : MonoBehaviour
{
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void MoveRb(Vector3 direction)
    {
        if (rb != null)
        {
            rb.AddForce(direction, ForceMode.Impulse);
        }
    }

}
