# Enhanced Breathing Analysis - Extract more data from existing sensors
import json, socket, time, statistics
from collections import deque
from gdx import gdx

UDP_IP = "127.0.0.1"
UDP_PORT = 53877

class BreathingAnalyzer:
    def __init__(self, window_size=50):  # 50 samples = ~2.5 seconds at 20Hz
        self.window_size = window_size
        self.force_history = deque(maxlen=window_size)
        self.resp_rate_history = deque(maxlen=window_size)
        self.timestamps = deque(maxlen=window_size)
        
        # Breathing cycle detection
        self.last_breath_time = 0
        self.breath_count = 0
        self.session_start = time.time()
        
        # Derived metrics
        self.breathing_depth = 0
        self.breathing_regularity = 0
        self.breathing_intensity = 0
        
    def add_sample(self, force, resp_rate, timestamp):
        """Add new sensor sample and update analysis"""
        self.force_history.append(force)
        self.resp_rate_history.append(resp_rate)
        self.timestamps.append(timestamp)
        
        self._update_breathing_depth()
        self._update_breathing_regularity()
        self._update_breathing_intensity()
        
    def _update_breathing_depth(self):
        """Calculate breathing depth from force variations"""
        if len(self.force_history) < 10:
            return
            
        # Breathing depth = range of force values (max - min)
        self.breathing_depth = max(self.force_history) - min(self.force_history)
        
    def _update_breathing_regularity(self):
        """Calculate breathing regularity from respiration rate consistency"""
        if len(self.resp_rate_history) < 10:
            return
            
        # Regularity = inverse of standard deviation (lower std = more regular)
        try:
            std_dev = statistics.stdev(self.resp_rate_history)
            self.breathing_regularity = max(0, 1.0 - (std_dev / 10.0))  # Normalize
        except:
            self.breathing_regularity = 0
            
    def _update_breathing_intensity(self):
        """Calculate breathing intensity from force magnitude"""
        if len(self.force_history) < 5:
            return
            
        # Intensity = average force magnitude
        self.breathing_intensity = statistics.mean([abs(f) for f in self.force_history])
        
    def get_derived_metrics(self):
        """Get all calculated breathing metrics"""
        return {
            "breathing_depth": self.breathing_depth,
            "breathing_regularity": self.breathing_regularity, 
            "breathing_intensity": self.breathing_intensity,
            "session_duration": time.time() - self.session_start,
            "avg_resp_rate": statistics.mean(self.resp_rate_history) if self.resp_rate_history else 0,
            "force_trend": self._calculate_trend(self.force_history),
            "resp_rate_trend": self._calculate_trend(self.resp_rate_history)
        }
        
    def _calculate_trend(self, data):
        """Calculate if values are increasing (1), decreasing (-1), or stable (0)"""
        if len(data) < 5:
            return 0
            
        recent = list(data)[-5:]
        if recent[-1] > recent[0] + 0.1:
            return 1  # Increasing
        elif recent[-1] < recent[0] - 0.1:
            return -1  # Decreasing
        else:
            return 0  # Stable

def main():
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    analyzer = BreathingAnalyzer()
    
    g = gdx.gdx()
    g.open(connection='usb')
    g.select_sensors([1, 2])  # Force and Respiration Rate
    g.start(50)  # 20Hz sampling
    
    print("Enhanced Breathing Analysis - Streaming to Unity")
    print("Derived metrics:")
    print("- Breathing Depth: How deep you're breathing")
    print("- Breathing Regularity: How consistent your breathing pattern is")
    print("- Breathing Intensity: How hard you're breathing")
    print("- Force Trend: Whether chest expansion is increasing/decreasing")
    print("- Resp Rate Trend: Whether breathing rate is changing")
    
    try:
        while True:
            vals = g.read()
            if vals is None:
                break
                
            timestamp = time.time()
            force = float(vals[0]) if len(vals) > 0 and vals[0] is not None else 0
            resp_rate = float(vals[1]) if len(vals) > 1 and vals[1] is not None else 0
            
            # Add sample to analyzer
            analyzer.add_sample(force, resp_rate, timestamp)
            
            # Get derived metrics
            derived = analyzer.get_derived_metrics()
            
            # Build comprehensive payload
            payload = {
                "t": timestamp,
                "force": force,
                "resp_rate_bpm": resp_rate,
                # Derived breathing metrics
                "breathing_depth": derived["breathing_depth"],
                "breathing_regularity": derived["breathing_regularity"],
                "breathing_intensity": derived["breathing_intensity"],
                "session_duration": derived["session_duration"],
                "avg_resp_rate": derived["avg_resp_rate"],
                "force_trend": derived["force_trend"],
                "resp_rate_trend": derived["resp_rate_trend"],
                # Additional useful metrics
                "force_min": min(analyzer.force_history) if analyzer.force_history else 0,
                "force_max": max(analyzer.force_history) if analyzer.force_history else 0,
                "force_range": derived["breathing_depth"]
            }
            
            # Send to Unity
            sock.sendto(json.dumps(payload).encode("utf-8"), (UDP_IP, UDP_PORT))
            
            # Print every 2 seconds for monitoring
            if int(timestamp) % 2 == 0:
                print(f"Force: {force:.2f}N, Rate: {resp_rate:.1f}bpm, "
                      f"Depth: {derived['breathing_depth']:.2f}, "
                      f"Regularity: {derived['breathing_regularity']:.2f}")
            
    except KeyboardInterrupt:
        print("\nStopping enhanced breathing analysis...")
    except Exception as e:
        print(f"Error: {e}")
    finally:
        g.stop()
        g.close()
        sock.close()

if __name__ == "__main__":
    main()
