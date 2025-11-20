# Test UDP Receiver - Simple test to verify UDP connectivity
import socket
import json
import time

UDP_IP = "172.16.68.157"  # Your Windows machine IP
UDP_PORT = 53878

def test_udp_receiver():
    """Test UDP receiver to verify connectivity"""
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))
    
    print(f"UDP Test Receiver listening on {UDP_IP}:{UDP_PORT}")
    print("Waiting for data from iOS app...")
    print("Press Ctrl+C to stop")
    
    try:
        while True:
            data, addr = sock.recvfrom(1024)
            json_str = data.decode('utf-8')
            
            print(f"\nReceived from {addr[0]}:{addr[1]}")
            print(f"Raw data: {json_str}")
            
            try:
                parsed = json.loads(json_str)
                print(f"Parsed JSON: {parsed}")
                
                # Check for heart rate data
                if 'heart_rate_bpm' in parsed:
                    print(f"✅ Heart Rate: {parsed['heart_rate_bpm']} bpm")
                else:
                    print("❌ No heart_rate_bpm field found")
                    
            except json.JSONDecodeError as e:
                print(f"❌ JSON Parse Error: {e}")
                
    except KeyboardInterrupt:
        print("\nStopping UDP test receiver...")
    finally:
        sock.close()

if __name__ == "__main__":
    test_udp_receiver()
