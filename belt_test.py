# resp_udp.py
import json, socket, time
from gdx import gdx

UDP_IP = "127.0.0.1"
UDP_PORT = 53877

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

g = gdx.gdx()
g.open(connection='usb')

# Enable Force (1) and Respiration Rate (2). Add 4/5 if you want steps too.
g.select_sensors([1, 2])

# 50–100 ms period is a good starting point (20–10 Hz). 10 ms is overkill for Unity.
g.start(50)

print("Streaming Force (N) and Respiration Rate (bpm) over UDP to", UDP_IP, UDP_PORT)
try:
    while True:
        vals = g.read()
        if vals is None:
            break
        payload = {
            "t": time.time(),
            "force": float(vals[0]) if len(vals) > 0 else None,
            "resp_rate_bpm": float(vals[1]) if len(vals) > 1 else None
        }
        sock.sendto(json.dumps(payload).encode("utf-8"), (UDP_IP, UDP_PORT))
except KeyboardInterrupt:
    pass
finally:
    g.stop()
    g.close()
