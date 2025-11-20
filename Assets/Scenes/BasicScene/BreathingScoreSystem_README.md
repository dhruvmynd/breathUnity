# Breathing Score System

A comprehensive real-time breathing score system for Unity that tracks how well users follow breathing guidance and provides motivational feedback.

## 🎯 Features

- **Real-time Scoring**: Calculates breathing accuracy score (0-100) based on timing and phase synchronization
- **Visual Feedback**: Dynamic UI with color-coded scores and animated feedback
- **Session Tracking**: Monitors complete breathing sessions with detailed analytics
- **Motivational System**: Encourages users with positive feedback and progress tracking
- **Easy Integration**: Automatic setup with existing breathing systems

## 📊 How It Works

The system analyzes three key aspects of breathing:

1. **Timing Accuracy** (40% weight): How closely the user follows the expected phase durations
2. **Phase Synchronization** (30% weight): How well the user follows the breathing phases (inhale, exhale, hold)
3. **Consistency** (30% weight): How consistent the user's breathing pattern is over time

### Scoring Algorithm

- **Excellent**: 85-100 points - Perfect breathing rhythm
- **Good**: 70-84 points - Solid breathing pattern
- **Needs Practice**: 0-69 points - Keep following the guidance

## 🚀 Quick Setup

### Method 1: Automatic Setup (Recommended)

1. Add the `BreathingScoreSetup` component to any GameObject in your scene
2. The system will automatically:
   - Find existing breathing components
   - Create the scoring system
   - Set up the UI
   - Configure optimal parameters

### Method 2: Manual Setup

1. Add `BreathingScoreCalculator` component to a GameObject
2. Add `BreathingScoreUIManager` component to a GameObject
3. Assign references in the inspector
4. Configure parameters as needed

## 📱 UI Components

The system creates three main UI elements:

- **Score Display**: Large, animated score with color feedback
- **Feedback Text**: Real-time guidance and encouragement
- **Session Summary**: Duration, cycles, and overall performance

## ⚙️ Configuration

### BreathingScoreCalculator Settings

```csharp
[Header("Score Settings")]
public int maxScore = 100;                    // Maximum possible score
public int goodScoreThreshold = 70;           // Threshold for "good" breathing
public int excellentScoreThreshold = 85;      // Threshold for "excellent" breathing

[Header("Timing Tolerance")]
public float timingTolerance = 0.5f;          // Tolerance for phase timing (seconds)
public float transitionTolerance = 0.3f;      // Tolerance for phase transitions (seconds)

[Header("Scoring Weights")]
public float timingWeight = 0.4f;             // Weight for timing accuracy
public float phaseSyncWeight = 0.3f;          // Weight for phase synchronization
public float consistencyWeight = 0.3f;        // Weight for consistency
```

### BreathingScoreUIManager Settings

```csharp
[Header("UI Prefab Settings")]
public int scoreFontSize = 48;                 // Font size for score display
public int feedbackFontSize = 24;              // Font size for feedback text
public int summaryFontSize = 18;               // Font size for summary text

[Header("Visual Effects")]
public bool enableScorePulse = true;           // Enable score pulse animation
public float pulseSpeed = 2f;                  // Pulse animation speed
public bool enableColorTransitions = true;     // Enable smooth color transitions
```

## 🔧 Integration

The system integrates seamlessly with existing breathing components:

- **BreathingPhaseAnimator**: Uses phase data for scoring
- **UDPHeartRateReceiver**: Receives breathing data from iOS app
- **Canvas**: Displays UI elements

## 📈 Session Analytics

Each breathing session tracks:

- **Duration**: Total session time
- **Cycles**: Number of complete breathing cycles
- **Average Score**: Overall performance
- **Phase Count**: Total breathing phases
- **Cycle Time**: Average time per cycle

## 🎮 Usage Examples

### Starting a Session
```csharp
BreathingScoreCalculator calculator = FindObjectOfType<BreathingScoreCalculator>();
calculator.StartNewSession();
```

### Getting Current Score
```csharp
float currentScore = calculator.GetCurrentScore();
bool sessionActive = calculator.IsSessionActive();
int cycleCount = calculator.GetCycleCount();
```

### Manual Control
```csharp
// End current session
calculator.EndCurrentSession();

// Reset all data
calculator.ResetAllData();

// Show system status
BreathingScoreSetup setup = FindObjectOfType<BreathingScoreSetup>();
setup.ShowSystemStatus();
```

## 🧪 Testing

The system includes built-in testing methods:

- **Test Score Display**: Shows sample score and feedback
- **Test Complete System**: Starts a test session
- **Reset All Components**: Clears all data

## 🎨 Visual Feedback

### Score Colors
- **Green**: Excellent performance (85-100)
- **Yellow**: Good performance (70-84)
- **Red**: Needs practice (0-69)

### Animations
- **Pulse Effect**: Score pulses when updating
- **Color Transitions**: Smooth color changes based on performance
- **Dynamic Feedback**: Real-time encouragement messages

## 🔍 Debug Information

Enable debug logging to see:
- Real-time score calculations
- Phase timing accuracy
- Session statistics
- Component status

## 📋 Requirements

- Unity 2020.3 or later
- TextMeshPro package
- Existing breathing system components:
  - `BreathingPhaseAnimator`
  - `UDPHeartRateReceiver`
  - Canvas for UI

## 🚨 Troubleshooting

### Common Issues

1. **No Score Display**: Ensure Canvas exists and UI components are created
2. **Score Not Updating**: Check that UDPHeartRateReceiver is receiving data
3. **UI Not Visible**: Verify Canvas settings and UI element positioning

### Debug Steps

1. Use `BreathingScoreSetup.ShowSystemStatus()` to check component status
2. Enable debug logging in `BreathingScoreCalculator`
3. Check console for error messages
4. Verify breathing data is being received

## 🎯 Best Practices

1. **Calibration**: Adjust timing tolerance based on your breathing patterns
2. **Feedback**: Use the feedback system to guide users
3. **Motivation**: Celebrate improvements and encourage consistency
4. **Testing**: Test with different breathing patterns to optimize scoring

## 🔮 Future Enhancements

- **Personalized Scoring**: Adapt to individual breathing patterns
- **Progress Tracking**: Long-term improvement tracking
- **Achievement System**: Unlockable milestones and rewards
- **Data Export**: Save session data for analysis
- **Custom UI Themes**: Different visual styles

---

**Happy Breathing! 🫁✨**
