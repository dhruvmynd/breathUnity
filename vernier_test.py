from godirect import GoDirect

# Initialize BLE (Bluetooth Low Energy)
gd = GoDirect(use_ble=True)

# Scan for devices for 5 seconds
devices = gd.list_devices(timeout=5)

if not devices:
    print("No GoDirect devices found.")
    exit()

device = devices[0]
print("Found device:", device.name)

# Open device
device.open()

# Print sensors available
for sensor in device.sensors:
    print("Sensor:", sensor.name)

# Enable all sensors
for sensor in device.sensors:
    sensor.enable(True)

print("Streaming data... Press Ctrl+C to stop.")

try:
    while True:
        device.read()  # pulls data internally
        
        for sensor in device.sensors:
            if sensor.value is not None:
                print(sensor.name, sensor.value)

except KeyboardInterrupt:
    print("Stopping...")

device.close()
gd.quit()
