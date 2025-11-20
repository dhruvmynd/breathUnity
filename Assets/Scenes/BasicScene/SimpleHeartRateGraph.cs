using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple Heart Rate Graph Visualization
/// Displays heart rate data as a scrolling line graph
/// </summary>
public class SimpleHeartRateGraph : MonoBehaviour
{
    [Header("Graph Settings")]
    public LineRenderer heartRateLine;
    public LineRenderer averageLine;
    public int maxDataPoints = 100;
    public float graphWidth = 10f;
    public float graphHeight = 5f;
    public float minHeartRate = 60f;
    public float maxHeartRate = 120f;
    
    [Header("Visual Settings")]
    public Color heartRateColor = Color.red;
    public Color averageColor = Color.yellow;
    public float lineWidth = 0.1f;
    public float updateInterval = 0.1f;
    
    // UI References removed for simplicity
    
    // Data storage
    private List<float> heartRateData = new List<float>();
    private float startTime;
    private float lastUpdateTime;
    
    // Statistics
    private float currentHR = 0f;
    private float averageHR = 0f;
    private float minHR = float.MaxValue;
    private float maxHR = float.MinValue;
    
    // Reference to UDP receiver
    private UDPHeartRateReceiver udpReceiver;
    
    void Start()
    {
        // Find UDP receiver
        udpReceiver = FindObjectOfType<UDPHeartRateReceiver>();
        if (udpReceiver == null)
        {
            Debug.LogError("UDPHeartRateReceiver not found! Make sure it's in the scene.");
        }
        
        // Setup LineRenderers
        SetupLineRenderers();
        
        startTime = Time.time;
        lastUpdateTime = Time.time;
        
        Debug.Log("📊 Simple Heart Rate Graph initialized");
    }
    
    void SetupLineRenderers()
    {
        // Setup heart rate line
        if (heartRateLine != null)
        {
            heartRateLine.material = new Material(Shader.Find("Sprites/Default"));
            heartRateLine.material.color = heartRateColor;
            heartRateLine.startWidth = lineWidth;
            heartRateLine.endWidth = lineWidth;
            heartRateLine.positionCount = 0;
            heartRateLine.useWorldSpace = false;
            heartRateLine.sortingOrder = 1;
        }
        
        // Setup average line
        if (averageLine != null)
        {
            averageLine.material = new Material(Shader.Find("Sprites/Default"));
            averageLine.material.color = averageColor;
            averageLine.startWidth = lineWidth * 0.5f;
            averageLine.endWidth = lineWidth * 0.5f;
            averageLine.positionCount = 0;
            averageLine.useWorldSpace = false;
            averageLine.sortingOrder = 2;
        }
    }
    
    void Update()
    {
        // Update graph at specified interval
        if (Time.time - lastUpdateTime >= updateInterval)
        {
            UpdateGraph();
            lastUpdateTime = Time.time;
        }
    }
    
    void UpdateGraph()
    {
        if (udpReceiver == null) 
        {
            Debug.LogWarning("📊 UDPHeartRateReceiver not found!");
            return;
        }
        
        // Get current heart rate from UDP receiver
        float newHR = udpReceiver.heartRateBpm;
        
        // Only update if we have valid data
        if (newHR > 0)
        {
            currentHR = newHR;
            
            // Add new data point
            heartRateData.Add(currentHR);
            
            // Update statistics
            UpdateStatistics();
            
            // Update graph visualization
            UpdateGraphVisualization();
            
            // UI removed for simplicity
            
            Debug.Log($"📈 HR Graph: {currentHR} bpm (Avg: {averageHR:F1}, Min: {minHR:F1}, Max: {maxHR:F1}) - Data Points: {heartRateData.Count}");
        }
        else
        {
            Debug.LogWarning($"📊 No valid HR data: {newHR}");
        }
    }
    
    void UpdateStatistics()
    {
        // Calculate average
        if (heartRateData.Count > 0)
        {
            float sum = 0f;
            foreach (float hr in heartRateData)
            {
                sum += hr;
            }
            averageHR = sum / heartRateData.Count;
        }
        
        // Update min/max
        if (currentHR < minHR) minHR = currentHR;
        if (currentHR > maxHR) maxHR = currentHR;
        
        // Keep only recent data points
        if (heartRateData.Count > maxDataPoints)
        {
            heartRateData.RemoveAt(0);
        }
    }
    
    void UpdateGraphVisualization()
    {
        if (heartRateData.Count < 2) 
        {
            Debug.Log($"📊 Not enough data points for graph: {heartRateData.Count}");
            return;
        }
        
        // Update heart rate line
        if (heartRateLine != null)
        {
            heartRateLine.positionCount = heartRateData.Count;
            
            for (int i = 0; i < heartRateData.Count; i++)
            {
                float x = (float)i / (maxDataPoints - 1) * graphWidth - graphWidth / 2f;
                float y = NormalizeHeartRate(heartRateData[i]) * graphHeight - graphHeight / 2f;
                heartRateLine.SetPosition(i, new Vector3(x, y, 0));
            }
            
            Debug.Log($"📊 Updated heart rate line with {heartRateData.Count} points");
        }
        else
        {
            Debug.LogWarning("📊 HeartRateLine LineRenderer is null!");
        }
        
        // Update average line
        if (averageLine != null && heartRateData.Count > 0)
        {
            averageLine.positionCount = 2;
            float avgY = NormalizeHeartRate(averageHR) * graphHeight - graphHeight / 2f;
            averageLine.SetPosition(0, new Vector3(-graphWidth / 2f, avgY, 0));
            averageLine.SetPosition(1, new Vector3(graphWidth / 2f, avgY, 0));
            
            Debug.Log($"📊 Updated average line at Y: {avgY}");
        }
        else
        {
            Debug.LogWarning("📊 AverageLine LineRenderer is null!");
        }
    }
    
    float NormalizeHeartRate(float hr)
    {
        // Normalize heart rate to 0-1 range
        return Mathf.Clamp01((hr - minHeartRate) / (maxHeartRate - minHeartRate));
    }
    
    // UpdateUI method removed for simplicity
    
    Color GetHeartRateColor(float hr)
    {
        // Color coding based on heart rate zones
        if (hr < 60) return Color.blue;
        if (hr < 100) return Color.green;
        if (hr < 120) return Color.yellow;
        if (hr < 150) return Color.orange;
        return Color.red;
    }
    
    // Public methods for external access
    public float GetCurrentHeartRate() => currentHR;
    public float GetAverageHeartRate() => averageHR;
    public float GetMinHeartRate() => minHR;
    public float GetMaxHeartRate() => maxHR;
    
    // Reset graph data
    [ContextMenu("Reset Graph")]
    public void ResetGraph()
    {
        heartRateData.Clear();
        minHR = float.MaxValue;
        maxHR = float.MinValue;
        averageHR = 0f;
        
        if (heartRateLine != null) heartRateLine.positionCount = 0;
        if (averageLine != null) averageLine.positionCount = 0;
        
        Debug.Log("📊 Graph reset");
    }
}
