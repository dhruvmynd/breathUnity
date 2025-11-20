using UnityEngine;

public class TestCameraDistance : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private string centerPointTagToSet = "CenterPoint";
    [SerializeField] private bool tagThisObject = true;
    [SerializeField] private bool showDistanceInConsole = true;
    [SerializeField] private float logInterval = 1f;
    
    private float lastLogTime;
    
    void Start()
    {
        if (tagThisObject)
        {
            gameObject.tag = centerPointTagToSet;
            Debug.Log($"TestCameraDistance: Tagged '{gameObject.name}' with '{centerPointTagToSet}'");
        }
    }
    
    void Update()
    {
        if (showDistanceInConsole && Time.time - lastLogTime > logInterval)
        {
            lastLogTime = Time.time;
            
            if (Camera.main != null)
            {
                Vector3 cameraPos = Camera.main.transform.position;
                Vector3 vectorToCenter = transform.position - cameraPos;
                float distance = vectorToCenter.magnitude;
                
                Debug.Log($"Camera to {gameObject.name}: Distance={distance:F2}m, Vector=({vectorToCenter.x:F2}, {vectorToCenter.y:F2}, {vectorToCenter.z:F2})");
            }
        }
    }
}