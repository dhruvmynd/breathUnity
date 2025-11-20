using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Real-time Heart Rate Graph Visualization
/// Displays heart rate data as a scrolling line graph
/// </summary>
public class HeartRateGraph : MonoBehaviour
{
    [Header("Graph Settings")]
    public LineRenderer heartRateLine;
    public LineRenderer averageLine;
    public LineRenderer xAxisLine;
    public LineRenderer yAxisLine;
    public int maxDataPoints = 100;  // Number of points to show
    public float graphWidth = 10f;   // Width of the graph
    public float graphHeight = 5f;   // Height of the graph
    public float minHeartRate = 60f; // Minimum HR for scaling
    public float maxHeartRate = 120f; // Maximum HR for scaling
    
    [Header("Axis Settings")]
    public bool showAxes = true;
    public bool showGridLines = false; // Default to false for cleaner look
    public bool showTickMarks = true;
    public int xAxisTicks = 5; // Reduced from 10
    public int yAxisTicks = 3; // Reduced from 5
    public Color axisColor = Color.white;
    public Color gridColor = Color.gray;
    
    [Header("Visual Settings")]
    public Color heartRateColor = Color.red;
    public Color averageColor = Color.yellow;
    public float lineWidth = 0.15f;
    public float averageLineWidth = 0.08f;
    public float updateInterval = 0.1f; // Update every 100ms
    
    [Header("Coordinate Labels")]
    public bool showCoordinateLabels = true;
    public TextMeshProUGUI xAxisLabel;
    public TextMeshProUGUI yAxisLabel;
    public string xAxisTitle = "Time (s)";
    public string yAxisTitle = "Heart Rate (bpm)";
    
    [Header("Line Renderer Settings")]
    public bool useUnlitShader = true;
    public float lineAlpha = 1.0f;
    public bool enableLineGlow = true;
    
    [Header("UI References")]
    public TextMeshProUGUI currentHRText;
    public TextMeshProUGUI averageHRText;
    public TextMeshProUGUI minHRText;
    public TextMeshProUGUI maxHRText;
    
    [Header("Auto-Setup UI")]
    public bool autoSetupUI = true;
    public string currentHRPrefix = "Live HR: ";
    public string averageHRPrefix = "Avg HR: ";
    public string minHRPrefix = "Min: ";
    public string maxHRPrefix = "Max: ";
    
    [Header("TextMeshPro Settings")]
    public bool useRichText = true;
    public int fontSize = 24;
    public FontStyles fontStyle = FontStyles.Bold;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    // Data storage
    private List<float> heartRateData = new List<float>();
    private List<float> timeData = new List<float>();
    private float startTime;
    private float lastUpdateTime;
    
    // Axis and grid data
    private List<LineRenderer> gridLines = new List<LineRenderer>();
    private List<TextMeshProUGUI> axisLabels = new List<TextMeshProUGUI>();
    private float timeWindow = 30f; // 30 seconds window
    
    // Statistics
    private float currentHR = 0f;
    private float averageHR = 0f;
    private float minHR = float.MaxValue;
    private float maxHR = float.MinValue;
    
    // Reference to UDP receiver
    private UDPHeartRateReceiver udpReceiver;
    
    void Start()
    {
        // Initialize graph
        InitializeGraph();
        
        // Auto-setup UI if enabled
        if (autoSetupUI)
        {
            AutoSetupUI();
        }
        
        // Find UDP receiver
        udpReceiver = FindObjectOfType<UDPHeartRateReceiver>();
        if (udpReceiver == null)
        {
            Debug.LogError("UDPHeartRateReceiver not found! Make sure it's in the scene.");
        }
        
        startTime = Time.time;
        lastUpdateTime = Time.time;
        
        Debug.Log("📊 Heart Rate Graph initialized");
    }
    
    void InitializeGraph()
    {
        // Setup LineRenderer for heart rate
        if (heartRateLine != null)
        {
            SetupLineRenderer(heartRateLine, heartRateColor, lineWidth);
        }
        
        // Setup LineRenderer for average
        if (averageLine != null)
        {
            SetupLineRenderer(averageLine, averageColor, averageLineWidth);
        }
        
        // Create axes and grid
        CreateAxes();
        CreateGridLines();
        SetupCoordinateLabels();
    }
    
    void CreateAxes()
    {
        if (!showAxes) return;
        
        // Create X-axis line
        if (xAxisLine == null)
        {
            GameObject xAxisObj = new GameObject("X-Axis");
            xAxisObj.transform.SetParent(transform);
            xAxisObj.transform.localPosition = new Vector3(0, -2.5f, 0); // Bottom of graph
            xAxisObj.transform.localRotation = Quaternion.identity;
            xAxisLine = xAxisObj.AddComponent<LineRenderer>();
        }
        SetupLineRenderer(xAxisLine, axisColor, 0.02f);
        DrawXAxis();
        
        // Create Y-axis line
        if (yAxisLine == null)
        {
            GameObject yAxisObj = new GameObject("Y-Axis");
            yAxisObj.transform.SetParent(transform);
            yAxisObj.transform.localPosition = new Vector3(-5f, 0, 0); // Left side of graph
            yAxisObj.transform.localRotation = Quaternion.identity;
            yAxisLine = yAxisObj.AddComponent<LineRenderer>();
        }
        SetupLineRenderer(yAxisLine, axisColor, 0.02f);
        DrawYAxis();
    }
    
    void DrawXAxis()
    {
        if (xAxisLine == null) return;
        
        xAxisLine.positionCount = 2;
        // X-axis spans the full width of the graph
        xAxisLine.SetPosition(0, new Vector3(-graphWidth / 2f, 0, 0));
        xAxisLine.SetPosition(1, new Vector3(graphWidth / 2f, 0, 0));
    }
    
    void DrawYAxis()
    {
        if (yAxisLine == null) return;
        
        yAxisLine.positionCount = 2;
        // Y-axis spans the full height of the graph
        yAxisLine.SetPosition(0, new Vector3(0, -graphHeight / 2f, 0));
        yAxisLine.SetPosition(1, new Vector3(0, graphHeight / 2f, 0));
    }
    
    void CreateGridLines()
    {
        if (!showGridLines) return;
        
        // Clear existing grid lines
        foreach (LineRenderer gridLine in gridLines)
        {
            if (gridLine != null) DestroyImmediate(gridLine.gameObject);
        }
        gridLines.Clear();
        
        // Create vertical grid lines (time) - only major divisions
        for (int i = 1; i < xAxisTicks; i++)
        {
            GameObject gridObj = new GameObject($"GridLine_V_{i}");
            gridObj.transform.SetParent(transform);
            gridObj.transform.localPosition = Vector3.zero;
            gridObj.transform.localRotation = Quaternion.identity;
            LineRenderer gridLine = gridObj.AddComponent<LineRenderer>();
            SetupLineRenderer(gridLine, new Color(gridColor.r, gridColor.g, gridColor.b, 0.3f), 0.005f); // More transparent and thinner
            
            float x = -graphWidth / 2f + (i * graphWidth / xAxisTicks);
            gridLine.positionCount = 2;
            gridLine.SetPosition(0, new Vector3(x, -graphHeight / 2f, -0.3f));
            gridLine.SetPosition(1, new Vector3(x, graphHeight / 2f, -0.3f));
            
            gridLines.Add(gridLine);
        }
        
        // Create horizontal grid lines (heart rate) - only major divisions
        for (int i = 1; i < yAxisTicks; i++)
        {
            GameObject gridObj = new GameObject($"GridLine_H_{i}");
            gridObj.transform.SetParent(transform);
            gridObj.transform.localPosition = Vector3.zero;
            gridObj.transform.localRotation = Quaternion.identity;
            LineRenderer gridLine = gridObj.AddComponent<LineRenderer>();
            SetupLineRenderer(gridLine, new Color(gridColor.r, gridColor.g, gridColor.b, 0.3f), 0.005f); // More transparent and thinner
            
            float y = -graphHeight / 2f + (i * graphHeight / yAxisTicks);
            gridLine.positionCount = 2;
            gridLine.SetPosition(0, new Vector3(-graphWidth / 2f, y, -0.3f));
            gridLine.SetPosition(1, new Vector3(graphWidth / 2f, y, -0.3f));
            
            gridLines.Add(gridLine);
        }
    }
    
    void SetupCoordinateLabels()
    {
        if (!showCoordinateLabels) return;
        
        // Setup axis titles
        if (xAxisLabel != null)
        {
            xAxisLabel.text = xAxisTitle;
            SetupTextMeshPro(xAxisLabel);
        }
        
        if (yAxisLabel != null)
        {
            yAxisLabel.text = yAxisTitle;
            SetupTextMeshPro(yAxisLabel);
        }
    }
    
    void SetupLineRenderer(LineRenderer lineRenderer, Color color, float width)
    {
        // Create a proper material for better visibility
        Material lineMaterial;
        
        if (useUnlitShader)
        {
            // Use Unlit shader for better visibility and performance
            lineMaterial = new Material(Shader.Find("Unlit/Color"));
        }
        else
        {
            // Fallback to Legacy shader
            lineMaterial = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended"));
        }
        
        // Set color with alpha
        Color finalColor = new Color(color.r, color.g, color.b, lineAlpha);
        lineMaterial.color = finalColor;
        
        // Apply material
        lineRenderer.material = lineMaterial;
        
        // Configure line renderer
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = false;
        lineRenderer.sortingOrder = 1; // Ensure it renders on top
        
        // Add glow effect if enabled
        if (enableLineGlow)
        {
            lineRenderer.material.SetFloat("_Glow", 0.5f);
        }
        
        // Enable shadows and lighting for better visibility
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;
    }
    
    void AutoSetupUI()
    {
        // Try to find TextMeshPro components if not assigned
        if (currentHRText == null)
        {
            currentHRText = GameObject.Find("CurrentHRText")?.GetComponent<TextMeshProUGUI>();
        }
        if (averageHRText == null)
        {
            averageHRText = GameObject.Find("AverageHRText")?.GetComponent<TextMeshProUGUI>();
        }
        if (minHRText == null)
        {
            minHRText = GameObject.Find("MinHRText")?.GetComponent<TextMeshProUGUI>();
        }
        if (maxHRText == null)
        {
            maxHRText = GameObject.Find("MaxHRText")?.GetComponent<TextMeshProUGUI>();
        }
        
        // Configure found TextMeshPro components
        SetupTextMeshPro(currentHRText);
        SetupTextMeshPro(averageHRText);
        SetupTextMeshPro(minHRText);
        SetupTextMeshPro(maxHRText);
        
        // Log UI setup status
        Debug.Log($"📊 UI Setup (TextMeshPro) - Current HR: {(currentHRText != null ? "✓" : "✗")}, " +
                  $"Average HR: {(averageHRText != null ? "✓" : "✗")}, " +
                  $"Min HR: {(minHRText != null ? "✓" : "✗")}, " +
                  $"Max HR: {(maxHRText != null ? "✓" : "✗")}");
    }
    
    void SetupTextMeshPro(TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.richText = useRichText;
            textComponent.alignment = TextAlignmentOptions.Left;
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
        if (udpReceiver == null) return;
        
        // Get current heart rate from UDP receiver
        float newHR = udpReceiver.heartRateBpm;
        
        // Only update if we have valid data
        if (newHR > 0)
        {
            currentHR = newHR;
            
            // Add new data point
            heartRateData.Add(currentHR);
            timeData.Add(Time.time - startTime);
            
            // Update statistics
            UpdateStatistics();
            
            // Update graph visualization
            UpdateGraphVisualization();
            
            // Update UI
            UpdateUI();
            
            if (showDebugInfo)
            {
                Debug.Log($"📈 LIVE HR: {currentHR:F0} bpm | AVG HR: {averageHR:F1} bpm | Range: {minHR:F0}-{maxHR:F0} bpm | Data Points: {heartRateData.Count}");
            }
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
            timeData.RemoveAt(0);
        }
    }
    
    void UpdateGraphVisualization()
    {
        if (heartRateData.Count < 2) return;
        
        // Update heart rate line with time-based scrolling
        if (heartRateLine != null)
        {
            heartRateLine.positionCount = heartRateData.Count;
            
            for (int i = 0; i < heartRateData.Count; i++)
            {
                // Use actual time for X-axis instead of data point index
                float timeProgress = timeData[i] / timeWindow;
                float x = (timeProgress * graphWidth) - graphWidth / 2f;
                float y = NormalizeHeartRate(heartRateData[i]) * graphHeight - graphHeight / 2f;
                heartRateLine.SetPosition(i, new Vector3(x, y, -0.1f));
            }
            
            // Ensure line is visible
            EnsureLineVisibility(heartRateLine);
        }
        
        // Update average line
        if (averageLine != null && heartRateData.Count > 0)
        {
            averageLine.positionCount = 2;
            float avgY = NormalizeHeartRate(averageHR) * graphHeight - graphHeight / 2f;
            averageLine.SetPosition(0, new Vector3(-graphWidth / 2f, avgY, -0.05f));
            averageLine.SetPosition(1, new Vector3(graphWidth / 2f, avgY, -0.05f));
            
            // Ensure line is visible
            EnsureLineVisibility(averageLine);
        }
    }
    
    void EnsureLineVisibility(LineRenderer lineRenderer)
    {
        if (lineRenderer != null && lineRenderer.material != null)
        {
            // Ensure material color is properly set
            if (lineRenderer == heartRateLine)
            {
                Color finalColor = new Color(heartRateColor.r, heartRateColor.g, heartRateColor.b, lineAlpha);
                lineRenderer.material.color = finalColor;
            }
            else if (lineRenderer == averageLine)
            {
                Color finalColor = new Color(averageColor.r, averageColor.g, averageColor.b, lineAlpha);
                lineRenderer.material.color = finalColor;
            }
            
            // Force material update
            lineRenderer.material.SetFloat("_Alpha", lineAlpha);
        }
    }
    
    float NormalizeHeartRate(float hr)
    {
        // Normalize heart rate to 0-1 range
        return Mathf.Clamp01((hr - minHeartRate) / (maxHeartRate - minHeartRate));
    }
    
    void UpdateUI()
    {
        // Update main HR displays
        UpdateHRText(currentHRText, $"{currentHRPrefix}{currentHR:F0} bpm", GetHeartRateColor(currentHR));
        UpdateHRText(averageHRText, $"{averageHRPrefix}{averageHR:F1} bpm", averageColor);
        UpdateHRText(minHRText, $"{minHRPrefix}{minHR:F0} bpm", Color.blue);
        UpdateHRText(maxHRText, $"{maxHRPrefix}{maxHR:F0} bpm", Color.red);
        
        // Update coordinate labels
        UpdateCoordinateLabels();
    }
    
    void UpdateHRText(TextMeshProUGUI textComponent, string text, Color color)
    {
        if (textComponent != null)
        {
            if (useRichText)
            {
                text = $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
            }
            textComponent.text = text;
            if (!useRichText)
            {
                textComponent.color = color;
            }
            SetupTextMeshPro(textComponent);
        }
    }
    
    void UpdateCoordinateLabels()
    {
        if (!showCoordinateLabels) return;
        
        // Axis labels are static, no updates needed here
        // The main HR values are updated in UpdateUI()
    }
    
    Color GetHeartRateColor(float hr)
    {
        // Color coding based on heart rate zones
        if (hr < 60) return Color.blue;      // Resting
        if (hr < 100) return Color.green;    // Normal
        if (hr < 120) return Color.yellow;   // Elevated
        if (hr < 150) return Color.orange;   // High
        return Color.red;                    // Very High
    }
    
    // Public methods for external access
    public float GetCurrentHeartRate() => currentHR;
    public float GetAverageHeartRate() => averageHR;
    public float GetMinHeartRate() => minHR;
    public float GetMaxHeartRate() => maxHR;
    
    // Formatted string methods
    public string GetCurrentHRString() 
    {
        string text = $"{currentHRPrefix}{currentHR:F0} bpm";
        if (useRichText)
        {
            text = $"<color=#{ColorUtility.ToHtmlStringRGB(GetHeartRateColor(currentHR))}>{text}</color>";
        }
        return text;
    }
    
    public string GetAverageHRString() 
    {
        string text = $"{averageHRPrefix}{averageHR:F1} bpm";
        if (useRichText)
        {
            text = $"<color=#{ColorUtility.ToHtmlStringRGB(averageColor)}>{text}</color>";
        }
        return text;
    }
    
    public string GetMinHRString() 
    {
        string text = $"{minHRPrefix}{minHR:F0} bpm";
        if (useRichText)
        {
            text = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.blue)}>{text}</color>";
        }
        return text;
    }
    
    public string GetMaxHRString() 
    {
        string text = $"{maxHRPrefix}{maxHR:F0} bpm";
        if (useRichText)
        {
            text = $"<color=#{ColorUtility.ToHtmlStringRGB(Color.red)}>{text}</color>";
        }
        return text;
    }
    
    // Get heart rate status
    public string GetHeartRateStatus()
    {
        if (currentHR == 0) return "No Data";
        if (currentHR < 60) return "Resting";
        if (currentHR < 100) return "Normal";
        if (currentHR < 120) return "Elevated";
        if (currentHR < 150) return "High";
        return "Very High";
    }
    
    // Refresh line renderer materials
    [ContextMenu("Refresh Line Materials")]
    public void RefreshLineMaterials()
    {
        if (heartRateLine != null)
        {
            SetupLineRenderer(heartRateLine, heartRateColor, lineWidth);
        }
        if (averageLine != null)
        {
            SetupLineRenderer(averageLine, averageColor, averageLineWidth);
        }
        Debug.Log("📊 Line renderer materials refreshed");
    }
    
    // Rebuild graph elements
    [ContextMenu("Rebuild Graph")]
    public void RebuildGraph()
    {
        InitializeGraph();
        Debug.Log("📊 Graph rebuilt with new settings");
    }
    
    // Get graph bounds for external use
    public Vector4 GetGraphBounds()
    {
        return new Vector4(-graphWidth / 2f, graphWidth / 2f, -graphHeight / 2f, graphHeight / 2f);
    }
    
    // Get current data range
    public Vector4 GetDataRange()
    {
        float minTime = timeData.Count > 0 ? timeData[0] : 0f;
        float maxTime = timeData.Count > 0 ? timeData[timeData.Count - 1] : 0f;
        return new Vector4(minTime, maxTime, minHeartRate, maxHeartRate);
    }
    
    // Reset graph data
    [ContextMenu("Reset Graph")]
    public void ResetGraph()
    {
        heartRateData.Clear();
        timeData.Clear();
        currentHR = 0f;
        minHR = float.MaxValue;
        maxHR = float.MinValue;
        averageHR = 0f;
        
        if (heartRateLine != null) heartRateLine.positionCount = 0;
        if (averageLine != null) averageLine.positionCount = 0;
        
        // Refresh materials after reset
        RefreshLineMaterials();
        
        // Update UI to show reset values
        UpdateUI();
        
        Debug.Log("📊 Graph reset - All HR data cleared");
    }
}
