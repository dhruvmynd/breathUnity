# Heart Rate Graph Setup Guide

## 🎯 Quick Setup (5 minutes)

### 1. Create Graph GameObject
1. **Create Empty GameObject** → Name it "HeartRateGraph"
2. **Add HeartRateGraph Script** to the GameObject

### 2. Create Line Renderers
1. **Create Empty Child** → Name it "HeartRateLine"
2. **Add LineRenderer Component** to HeartRateLine
3. **Create Empty Child** → Name it "AverageLine" 
4. **Add LineRenderer Component** to AverageLine

### 3. Create UI Elements
1. **Create Canvas** (if not exists)
2. **Create Text Objects** for:
   - Current Heart Rate
   - Average Heart Rate  
   - Min Heart Rate
   - Max Heart Rate

### 4. Configure HeartRateGraph Script
1. **Assign LineRenderers**:
   - Heart Rate Line → HeartRateLine GameObject
   - Average Line → AverageLine GameObject
2. **Assign UI Text**:
   - Current HR Text → Current Heart Rate Text
   - Average HR Text → Average Heart Rate Text
   - Min HR Text → Min Heart Rate Text
   - Max HR Text → Max Heart Rate Text

### 5. Graph Settings
- **Max Data Points**: 100 (shows last 100 readings)
- **Graph Width**: 10 (adjust for your scene)
- **Graph Height**: 5 (adjust for your scene)
- **Min Heart Rate**: 60 (for scaling)
- **Max Heart Rate**: 120 (for scaling)
- **Update Interval**: 0.1 (100ms updates)

## 🎨 Visual Features

### Real-time Graph
- **Red Line**: Current heart rate over time
- **Yellow Line**: Average heart rate
- **Scrolling**: Shows last 100 data points
- **Auto-scaling**: Adjusts to heart rate range

### Color Coding
- **Blue**: < 60 bpm (Resting)
- **Green**: 60-100 bpm (Normal)
- **Yellow**: 100-120 bpm (Elevated)
- **Orange**: 120-150 bpm (High)
- **Red**: > 150 bpm (Very High)

### Statistics
- **Current**: Real-time heart rate
- **Average**: Session average
- **Min/Max**: Session range

## 🔧 Advanced Configuration

### Customization Options
- **Line Colors**: Change heartRateColor and averageColor
- **Line Width**: Adjust lineWidth
- **Graph Size**: Modify graphWidth and graphHeight
- **Update Rate**: Change updateInterval
- **Data Points**: Adjust maxDataPoints

### Performance Tips
- **Lower updateInterval** for smoother graphs
- **Higher maxDataPoints** for longer history
- **Disable showDebugInfo** in production

## 🚀 Testing

1. **Run the scene**
2. **Send heart rate data** from iOS app
3. **Watch the graph** update in real-time
4. **Check statistics** in UI text elements

The graph will automatically start displaying data when heart rate values are received from the UDPHeartRateReceiver!
