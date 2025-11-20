# iOS to Unity Bridge - Receives iOS data and forwards to Unity
import json
import socket
import time
import threading

# iOS app sends to this port
IOS_PORT = 53878
IOS_IP = "172.16.68.157"

# Unity receives on this port (different from belt sensor)
UNITY_PORT = 53879
UNITY_IP = "127.0.0.1"  # Local Unity

class iOSToUnityBridge:
    def __init__(self):
        self.ios_socket = None
        self.unity_socket = None
        self.running = False
        
        # Cache for last known values (to maintain when not sent every frame)
        self.last_hrv = 0
        self.last_breathing_phase = ""
        self.last_phase_duration = 0
        
    def start(self):
        """Start the bridge service"""
        try:
            # Create socket to receive from iOS
            self.ios_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            self.ios_socket.bind((IOS_IP, IOS_PORT))
            
            # Create socket to send to Unity
            self.unity_socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
            
            self.running = True
            
            print(f"🌉 iOS to Unity Bridge Started")
            print(f"📱 Receiving from iOS: {IOS_IP}:{IOS_PORT}")
            print(f"🎮 Sending to Unity: {UNITY_IP}:{UNITY_PORT}")
            print("Press Ctrl+C to stop")
            
            # Start receiving thread
            receive_thread = threading.Thread(target=self.receive_from_ios)
            receive_thread.daemon = True
            receive_thread.start()
            
            # Keep main thread alive
            while self.running:
                time.sleep(0.1)
                
        except KeyboardInterrupt:
            print("\n🛑 Stopping bridge...")
        except Exception as e:
            print(f"❌ Error: {e}")
        finally:
            self.stop()
    
    def receive_from_ios(self):
        """Receive data from iOS app"""
        while self.running:
            try:
                data, addr = self.ios_socket.recvfrom(1024)
                json_str = data.decode('utf-8')
                
                print(f"📱 Received from iOS ({addr[0]}): {json_str}")
                
                # Parse iOS data
                ios_data = json.loads(json_str)
                
                # Convert to Unity format
                unity_data = self.convert_to_unity_format(ios_data)
                
                # Send to Unity
                self.send_to_unity(unity_data)
                
            except Exception as e:
                if self.running:
                    print(f"❌ Receive error: {e}")
    
    def convert_to_unity_format(self, ios_data):
        """Convert iOS JSON to Unity format"""
        # iOS format: {"heart_rate": 75, "hrv": 45.2, "breathing_phase": "inhale", "phase_duration": 2.5, "session_active": true, "device": "iOS", "timestamp": 1234567890}
        # Unity format: {"heart_rate_bpm": 75, "hrv": 45.2, "breathing_phase": "inhale", "phase_duration": 2.5, "force": 0, "resp_rate_bpm": 0, "steps": 0, "step_rate_spm": 0, "t": 1234567890}
        
        # Update cached values if new ones are present, otherwise use last known values
        if "hrv" in ios_data and ios_data["hrv"] > 0:
            self.last_hrv = ios_data["hrv"]
        
        if "breathing_phase" in ios_data and ios_data["breathing_phase"]:
            self.last_breathing_phase = ios_data["breathing_phase"]
        
        if "phase_duration" in ios_data:
            self.last_phase_duration = ios_data["phase_duration"]
        
        unity_data = {
            "heart_rate_bpm": ios_data.get("heart_rate", 0),
            "hrv": self.last_hrv,  # Use cached HRV value
            "breathing_phase": self.last_breathing_phase,  # Use cached breathing phase
            "phase_duration": self.last_phase_duration,  # Use cached phase duration
            "force": 0,  # No force data from iOS
            "resp_rate_bpm": 0,  # No breathing rate data from iOS
            "steps": 0,  # No step data from iOS
            "step_rate_spm": 0,  # No step rate from iOS
            "t": ios_data.get("timestamp", time.time())
        }
        
        return unity_data
    
    def send_to_unity(self, unity_data):
        """Send data to Unity"""
        try:
            json_str = json.dumps(unity_data)
            self.unity_socket.sendto(json_str.encode('utf-8'), (UNITY_IP, UNITY_PORT))
            print(f"🎮 Sent to Unity: {json_str}")
        except Exception as e:
            print(f"❌ Send error: {e}")
    
    def stop(self):
        """Stop the bridge"""
        self.running = False
        if self.ios_socket:
            self.ios_socket.close()
        if self.unity_socket:
            self.unity_socket.close()
        print("✅ Bridge stopped")

if __name__ == "__main__":
    bridge = iOSToUnityBridge()
    bridge.start()
