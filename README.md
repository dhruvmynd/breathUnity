# JustBreath - Unity Breathing Meditation Experience

A Unity-based breathing meditation application that uses real-time sensor data (iOS heart rate, HRV, and breathing phases) to create an immersive visual experience.

## Features

- **Real-time Breathing Visualization**: Spheres that respond to your breathing patterns
- **iOS Integration**: Receives heart rate, HRV (Heart Rate Variability), and breathing phase data from iOS devices
- **Sensor Support**: Compatible with breathing belt sensors via UDP
- **Immersive VR/AR Experience**: First-person perspective with levitating spheres
- **Transparent Breathing Spheres**: Visual feedback with color-coded breathing phases

## Setup

### Requirements
- Unity (version compatible with the project)
- Python 3.x (for iOS to Unity bridge)
- iOS device with breathing/heart rate monitoring

### Python Bridge
The `python/ios_to_unity_bridge.py` script bridges iOS data to Unity:
```bash
cd python
python ios_to_unity_bridge.py
```

### Unity Configuration
1. Open the project in Unity
2. Configure UDP port settings in `UDPHeartRateReceiver.cs` (default: 53879)
3. Assign sphere GameObjects to the breathing controllers
4. Run the scene

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
│   └── ios_to_unity_bridge.py           # iOS to Unity bridge
└── README.md
```

## Data Flow

```
iOS Device → Python Bridge → Unity UDP Receiver → Breathing Controllers → Visual Spheres
```

## License

[Add your license here]

