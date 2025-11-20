# JustBreath - Unity Breathing Meditation Experience

A Unity-based breathing meditation application that uses real-time sensor data (iOS heart rate, HRV, and breathing phases) to create an immersive visual experience.

## Features

- **Real-time Breathing Visualization**: Spheres that respond to your breathing patterns
- **iOS Integration**: Receives heart rate, HRV (Heart Rate Variability), and breathing phase data from iOS devices
- **Vernier Belt Sensor Support**: Connects to Go Direct Breathing Belt via USB for real-time force, respiration rate, steps, and step rate data
- **Immersive VR/AR Experience**: First-person perspective with levitating spheres
- **Transparent Breathing Spheres**: Visual feedback with color-coded breathing phases

## Setup

### Requirements
- Unity (version compatible with the project)
- Python 3.x (for data bridges)
- iOS device with breathing/heart rate monitoring (optional)
- Vernier Go Direct Breathing Belt (optional, for physical sensor data)

### Python Bridges

#### iOS to Unity Bridge
The `python/ios_to_unity_bridge.py` script bridges iOS data to Unity:
```bash
cd python
python ios_to_unity_bridge.py
```
This receives data from iOS devices on port 53878 and forwards it to Unity on port 53879.

#### Vernier Belt Sensor
The `python/belt_test.py` script connects to a Vernier Go Direct Breathing Belt and streams sensor data:
```bash
cd python
python belt_test.py
```

**Belt Sensor Data:**
- **Force (N)**: Chest expansion/compression force
- **Respiration Rate (bpm)**: Calculated breathing rate
- **Steps**: Step count
- **Step Rate (spm)**: Steps per minute

The script sends data via UDP to Unity on port **53877** (different from iOS bridge port).

### Unity Configuration
1. Open the project in Unity
2. Configure UDP port settings in `UDPHeartRateReceiver.cs`:
   - **Port 53879**: iOS data (via bridge)
   - **Port 53877**: Vernier belt sensor data (direct)
3. Assign sphere GameObjects to the breathing controllers
4. Run the scene

**Note**: You can use either iOS data, belt sensor data, or both simultaneously. The Unity receiver handles both data sources.

## Project Structure

```
JustBreath/
├── Assets/
│   ├── BreathingPhaseController.cs      # Main breathing controller
│   ├── Scenes/
│   │   └── BasicScene/
│   │       ├── BreathingPhaseAnimator.cs
│   │       └── UDPHeartRateReceiver.cs
├── python/
│   ├── ios_to_unity_bridge.py           # iOS to Unity bridge
│   ├── belt_test.py                     # Vernier belt sensor data collection
│   └── gdx/                             # Vernier Go Direct SDK
└── README.md
```

## Data Flow

### iOS Data Path
```
iOS Device → ios_to_unity_bridge.py (port 53878) → Unity UDP Receiver (port 53879) → Breathing Controllers → Visual Spheres
```

### Vernier Belt Sensor Data Path
```
Vernier Go Direct Belt (USB) → belt_test.py → UDP (port 53877) → Unity UDP Receiver → Breathing Controllers → Visual Spheres
```

**Data Sources:**
- **iOS**: Heart rate, HRV, breathing phase, phase duration
- **Vernier Belt**: Force (N), respiration rate (bpm), steps, step rate (spm)

## License

[Add your license here]

