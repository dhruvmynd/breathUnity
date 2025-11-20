using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class RespReceiver : MonoBehaviour
{
    public int listenPort = 53877;
    public float forceN;
    public float respRateBpm;

    UdpClient udp;
    Thread thread;
    volatile bool running;

    [Serializable]
    class Payload { public double t; public double force; public double resp_rate_bpm; }

    void Start()
    {
        udp = new UdpClient(listenPort);
        running = true;
        thread = new Thread(ListenLoop) { IsBackground = true };
        thread.Start();
    }

    void ListenLoop()
    {
        IPEndPoint any = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                var data = udp.Receive(ref any);
                var json = Encoding.UTF8.GetString(data);
                var p = JsonUtility.FromJson<Payload>(json);
                if (p != null)
                {
                    forceN = (float)p.force;
                    respRateBpm = (float)p.resp_rate_bpm;
                }
            }
            catch { /* ignore transient errors */ }
        }
    }

    void OnDestroy()
    {
        running = false;
        try { udp?.Close(); } catch {}
        try { thread?.Join(200); } catch {}
    }
}
