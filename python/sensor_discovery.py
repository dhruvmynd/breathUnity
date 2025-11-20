# Sensor Discovery Script for Go Direct Breathing Belt
# This script helps identify what sensors are available on your device

import time
from gdx import gdx

def discover_sensors():
    """Discover available sensors on the Go Direct device"""
    g = gdx.gdx()
    
    try:
        g.open(connection='usb')
        print("✓ Connected to Go Direct device")
        
        # Common sensor numbers to test
        sensor_tests = [
            (1, "Force (N)", "Chest expansion/compression force"),
            (2, "Respiration Rate (bpm)", "Breathing rate"),
            (3, "Heart Rate (bpm)", "Heart rate"),
            (4, "Steps", "Step count"),
            (5, "Activity Level", "Activity intensity"),
            (6, "Temperature (°C)", "Body temperature"),
            (7, "Acceleration X (m/s²)", "X-axis acceleration"),
            (8, "Acceleration Y (m/s²)", "Y-axis acceleration"),
            (9, "Acceleration Z (m/s²)", "Z-axis acceleration"),
            (10, "Gyroscope X (deg/s)", "X-axis rotation rate"),
            (11, "Gyroscope Y (deg/s)", "Y-axis rotation rate"),
            (12, "Gyroscope Z (deg/s)", "Z-axis rotation rate")
        ]
        
        print("\nTesting sensor availability...")
        available_sensors = []
        
        for sensor_num, name, description in sensor_tests:
            try:
                # Try to select just this sensor
                g.select_sensors([sensor_num])
                g.start(50)  # 50ms period
                
                # Try to read a few values
                for _ in range(3):
                    vals = g.read()
                    if vals is not None and len(vals) > 0 and vals[0] is not None:
                        available_sensors.append((sensor_num, name, description))
                        print(f"✓ Sensor {sensor_num}: {name} - {description}")
                        break
                
                g.stop()
                
            except Exception as e:
                print(f"✗ Sensor {sensor_num}: {name} - Not available")
        
        print(f"\nFound {len(available_sensors)} available sensors:")
        for sensor_num, name, description in available_sensors:
            print(f"  {sensor_num}: {name}")
        
        return [s[0] for s in available_sensors]
        
    except Exception as e:
        print(f"Error connecting to device: {e}")
        return []
    finally:
        try:
            g.close()
        except:
            pass

def test_sensor_combination(sensor_list):
    """Test reading from multiple sensors simultaneously"""
    if not sensor_list:
        print("No sensors to test")
        return
        
    g = gdx.gdx()
    
    try:
        g.open(connection='usb')
        print(f"\nTesting combination of sensors: {sensor_list}")
        
        g.select_sensors(sensor_list)
        g.start(50)
        
        print("Reading data for 5 seconds...")
        start_time = time.time()
        
        while time.time() - start_time < 5:
            vals = g.read()
            if vals is not None:
                print(f"Values: {vals}")
            time.sleep(0.1)
            
    except Exception as e:
        print(f"Error testing sensor combination: {e}")
    finally:
        try:
            g.stop()
            g.close()
        except:
            pass

if __name__ == "__main__":
    print("Go Direct Breathing Belt - Sensor Discovery")
    print("=" * 50)
    
    # Discover available sensors
    available = discover_sensors()
    
    if available:
        # Test reading from all available sensors
        test_sensor_combination(available)
        
        print(f"\nRecommended sensor selection for belt_test.py:")
        print(f"g.select_sensors({available})")
    else:
        print("\nNo sensors found. Check device connection.")
