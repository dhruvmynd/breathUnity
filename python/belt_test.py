# Enhanced belt sensor data collection
import json, socket, time
from gdx import gdx

UDP_IP = "127.0.0.1"
UDP_PORT = 53877

sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

g = gdx.gdx()
g.open(connection='usb')

# Sensor mapping for Go Direct Breathing Belt:
# Sensor 1: Force (N) - Chest expansion/compression
# Sensor 2: Respiration Rate (bpm) - Calculated breathing rate
# Sensor 3: Heart Rate (bpm) - If available
# Sensor 4: Steps - If available  
# Sensor 5: Activity Level - If available
# Sensor 6: Temperature (°C) - If available

# Enable all available sensors on your Go Direct Breathing Belt
available_sensors = [1, 2, 4, 5]  # All available sensors
print("Available sensors on Go Direct Breathing Belt:")
print("1: Force (N) - Chest expansion/compression force")
print("2: Respiration Rate (bpm) - Breathing rate")
print("4: Steps (steps) - Step count")
print("5: Step Rate (spm) - Steps per minute")

# Enable all available sensors
try:
    g.select_sensors(available_sensors)
    print(f"Successfully enabled sensors: {available_sensors}")
except Exception as e:
    print(f"Error enabling sensors: {e}")
    # Fallback to basic sensors
    available_sensors = [1, 2]
    g.select_sensors(available_sensors)
    print(f"Using fallback sensors: {available_sensors}")

# 50–100 ms period is a good starting point (20–10 Hz)
g.start(50)

sensor_names = {
    1: "force",
    2: "resp_rate_bpm", 
    4: "steps",
    5: "step_rate_spm"
}

print(f"\nStreaming data over UDP to {UDP_IP}:{UDP_PORT}")
print("Available data channels:", [sensor_names.get(s, f"sensor_{s}") for s in available_sensors])

try:
    while True:
        vals = g.read()
        if vals is None:
            break
            
        # Build payload with all available sensor data
        payload = {"t": time.time()}
        
        for i, sensor_num in enumerate(available_sensors):
            if i < len(vals) and vals[i] is not None:
                sensor_name = sensor_names.get(sensor_num, f"sensor_{sensor_num}")
                payload[sensor_name] = float(vals[i])
        
        # Send data to Unity
        sock.sendto(json.dumps(payload).encode("utf-8"), (UDP_IP, UDP_PORT))
        
        # Print data for debugging (optional)
        if len(payload) > 1:  # More than just timestamp
            print(f"Data: {payload}")
            
except KeyboardInterrupt:
    print("\nStopping data collection...")
except Exception as e:
    print(f"Error: {e}")
finally:
    g.stop()
    g.close()
    sock.close()
