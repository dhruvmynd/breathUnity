using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

[Serializable]
public class HeartRateData
{
    public double t;               // timestamp
    public double force;           // N (from belt sensor)
    public double resp_rate_bpm;   // breathing rate
    public double steps;           // step count
    public double step_rate_spm;   // steps per minute
    public double heart_rate_bpm;  // heart rate from iOS app
    public string breathing_phase; // breathing phase from iOS
    public double phase_duration;  // phase duration from iOS
}

[Serializable]
public class iOSHeartRateData
{
    public double heart_rate;      // iOS app sends "heart_rate" not "heart_rate_bpm"
    public string breathing_phase;  // iOS breathing phase: "inhale", "exhale", "hold", etc.
    public double phase_duration;   // Duration of current phase in seconds
    public bool session_active;
    public string device;
    public double timestamp;
}

public class UDPHeartRateReceiver : MonoBehaviour
{
    [Header("UDP")]
    public int port = 53879;

    [Header("Target")]
    public Transform sphere;         // assign your sphere
    public float baseScale = 1.0f;   // neutral size
    public float scaleRange = 1.5f;  // visual swing (increase for more)
    public float sensitivity = 2.0f; // additional gain on top of scaleRange
    public bool invert = true;       // true = contract on inhale, expand on exhale

    [Header("Smoothing / Normalization")]
    public float smoothingSeconds = 0.25f;   // EMA smoothing
    public float visualLerpSeconds = 0.10f;  // visual smoothing
    public float rangeFollowUp = 0.35f;      // tracking to new peaks
    public float rangeRelaxDown = 0.02f;     // decay when not hitting peaks
    public float rangePadding = 0.1f;

    [Header("Debug (read-only)")]
    public float forceN;
    public float forceSmoothed;
    public float normalized01;
    public float respRateBpm;
    public float heartRateBpm;
    public float steps;
    public float stepRateSpm;
    public string breathingPhase = "";
    public float phaseDuration = 0f;
    public bool gotPacket;
    public string lastReceivedJson = "";
    public string lastSenderIP = "";

    UdpClient _udp;
    Thread _thread;
    volatile bool _running;

    // state
    float emaForce, prevEma;
    float lowTrack = -0.5f, highTrack = 0.5f;

    void Start()
    {
        try
        {
            _udp = new UdpClient(port);
            _running = true;
            _thread = new Thread(ListenLoop) { IsBackground = true };
            _thread.Start();
            
            // Get local IP address
            string localIP = GetLocalIPAddress();
            Debug.Log($"UDP Heart Rate Receiver listening on {localIP}:{port}");
            Debug.Log($"Expected sender IP: 172.16.68.157");
        }
        catch (Exception e)
        {
            Debug.LogError("UDP init failed: " + e.Message);
        }
    }
    
    string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "Unknown";
    }

    void ListenLoop()
    {
        var any = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            try
            {
                // Set a timeout to allow checking _running periodically
                _udp.Client.ReceiveTimeout = 1000; // 1 second timeout
                
                var data = _udp.Receive(ref any);
                var json = Encoding.UTF8.GetString(data);
                
                // Store sender info for debugging
                lastSenderIP = any.Address.ToString();
                lastReceivedJson = json;

                Debug.Log($"Received from {lastSenderIP}: {json}");

                // Try to parse as iOS heart rate data first
                var iosData = JsonUtility.FromJson<iOSHeartRateData>(json);
                if (iosData != null && iosData.heart_rate > 0)
                {
                    // iOS app data
                    heartRateBpm = (float)iosData.heart_rate;
                    breathingPhase = iosData.breathing_phase ?? "";
                    phaseDuration = (float)iosData.phase_duration;
                    gotPacket = true;
                    
                    Debug.Log($"✅ iOS Data - HR: {heartRateBpm} bpm, Phase: '{breathingPhase}', Duration: {phaseDuration:F1}s (Session: {iosData.session_active})");
                }
                else
                {
                    // Try to parse as belt sensor data
                    var beltData = JsonUtility.FromJson<HeartRateData>(json);
                    if (beltData != null)
                    {
                        // Belt sensor data
                        forceN = (float)beltData.force;
                        respRateBpm = (float)beltData.resp_rate_bpm;
                        heartRateBpm = (float)beltData.heart_rate_bpm;
                        steps = (float)beltData.steps;
                        stepRateSpm = (float)beltData.step_rate_spm;
                        
                        // Handle breathing phase data if present (from bridge)
                        if (!string.IsNullOrEmpty(beltData.breathing_phase))
                        {
                            breathingPhase = beltData.breathing_phase;
                            phaseDuration = (float)beltData.phase_duration;
                        }
                        
                        gotPacket = true;
                        
                        Debug.Log($"✅ Belt Data - HR: {heartRateBpm}, Force: {forceN}, Resp: {respRateBpm}, Steps: {steps}, Phase: '{breathingPhase}', Duration: {phaseDuration:F1}s");
                    }
                    else
                    {
                        Debug.LogWarning($"❌ Failed to parse JSON: {json}");
                    }
                }
            }
            catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
            {
                // Timeout is expected - just continue the loop to check _running
                continue;
            }
            catch (ObjectDisposedException)
            {
                // Socket was closed - exit gracefully
                Debug.Log("UDP socket closed, exiting listen loop");
                break;
            }
            catch (Exception e)
            {
                if (_running)
                {
                    Debug.LogError($"UDP receive error: {e.Message}");
                }
                // If we're shutting down, don't spam errors
            }
        }
    }

    void Update()
    {
        // Always show debug info, even without sphere
        if (!gotPacket) return;
        
        // Debug: Show current values every 2 seconds
        if (Time.time % 2f < Time.deltaTime)
        {
            Debug.Log($"📊 Current Values - HR: {heartRateBpm}, Force: {forceN}, Resp: {respRateBpm}, Steps: {steps}");
        }
        
        // Only do sphere scaling if sphere is assigned
        if (sphere == null) return;

        // EMA smoothing
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float alpha = 1f - Mathf.Exp(-dt / Mathf.Max(1e-3f, smoothingSeconds));
        emaForce = Mathf.Lerp(emaForce, forceN, alpha);
        forceSmoothed = emaForce;

        // Auto-range normalization to 0..1
        if (emaForce > highTrack) highTrack = Mathf.Lerp(highTrack, emaForce, rangeFollowUp * dt);
        else                      highTrack = Mathf.Lerp(highTrack, highTrack - rangePadding, rangeRelaxDown * dt);
        if (emaForce < lowTrack)  lowTrack  = Mathf.Lerp(lowTrack,  emaForce, rangeFollowUp * dt);
        else                      lowTrack  = Mathf.Lerp(lowTrack,  lowTrack + rangePadding, rangeRelaxDown * dt);

        float lo = lowTrack - rangePadding;
        float hi = highTrack + rangePadding;
        if (hi - lo < 1e-4f) { lo -= 0.5f; hi += 0.5f; }

        normalized01 = Mathf.InverseLerp(lo, hi, emaForce);

        // Map to scale (invert = contract on inhale, expand on exhale)
        float signed = (normalized01 - 0.5f) * 2f; // -1..+1
        if (invert) signed = -signed;

        float targetScale = baseScale + signed * (scaleRange * sensitivity);
        float scaleNow = sphere.localScale.x;
        float lerpA = 1f - Mathf.Exp(-dt / Mathf.Max(1e-3f, visualLerpSeconds));
        float newScale = Mathf.Lerp(scaleNow, targetScale, lerpA);
        newScale = Mathf.Clamp(newScale, 0.05f, 10f);

        sphere.localScale = new Vector3(newScale, newScale, newScale);
    }

    void OnDestroy()
    {
        Debug.Log("UDPHeartRateReceiver shutting down...");
        
        // Signal thread to stop
        _running = false;
        
        // Close UDP socket to interrupt blocking receive
        try 
        { 
            _udp?.Close(); 
            Debug.Log("UDP socket closed");
        } 
        catch (Exception e) 
        { 
            Debug.LogWarning($"Error closing UDP socket: {e.Message}");
        }
        
        // Wait for thread to finish (with timeout)
        try 
        { 
            if (_thread != null && _thread.IsAlive)
            {
                bool joined = _thread.Join(2000); // 2 second timeout
                if (joined)
                {
                    Debug.Log("UDP thread stopped gracefully");
                }
                else
                {
                    Debug.LogWarning("UDP thread did not stop within timeout");
                }
            }
        } 
        catch (Exception e) 
        { 
            Debug.LogWarning($"Error joining UDP thread: {e.Message}");
        }
        
        Debug.Log("UDPHeartRateReceiver shutdown complete");
    }
    
    // Test method to verify parsing works
    [ContextMenu("Test iOS Data Parsing")]
    void TestiOSDataParsing()
    {
        string testJson = "{\"heart_rate\":75,\"session_active\":true,\"device\":\"iOS\",\"timestamp\":1758310976.573597}";
        Debug.Log($"Testing JSON: {testJson}");
        
        var iosData = JsonUtility.FromJson<iOSHeartRateData>(testJson);
        if (iosData != null)
        {
            Debug.Log($"✅ Test Success - HR: {iosData.heart_rate}, Session: {iosData.session_active}");
            heartRateBpm = (float)iosData.heart_rate;
            gotPacket = true;
        }
        else
        {
            Debug.LogError("❌ Test Failed - Could not parse JSON");
        }
    }
}
