# belt_diag_read.py
from time import sleep
from godirect import GoDirect

def maybe_enable_all(dev):
    # Try the common helper if present
    if hasattr(dev, "enable_sensors"):
        try:
            dev.enable_sensors()
            return True
        except Exception as e:
            print("enable_sensors() failed:", e)

    # Try to enable by probing indices (some builds use sparse keys; catch KeyError)
    enabled = 0
    if hasattr(dev, "get_sensor"):
        for n in range(0, 256):  # generous range; invalid keys will raise
            try:
                s = dev.get_sensor(n)
            except KeyError:
                continue
            except Exception as e:
                # some builds use non-int keys only; bail on weird exceptions
                break
            if s is None:
                continue
            if hasattr(s, "enabled"):
                s.enabled = True
                enabled += 1
    return enabled > 0

def main():
    gd = GoDirect(use_ble=False)  # USB only
    dev = gd.get_device()
    if not dev:
        print("No Go Direct device found over USB.")
        print("Tip: unplug/replug; different USB port; close any Vernier apps.")
        gd.quit(); return

    print("Connected to:", getattr(dev, "name", "Go Direct (USB)"))
    print("Device type:", type(dev).__name__)

    # Enable all channels we can
    if not maybe_enable_all(dev):
        print("Could not explicitly enable sensors — will try to read anyway.")

    # Start streaming ~10 Hz
    period_ms = 100
    if hasattr(dev, "start"):
        dev.start(period=period_ms)
    else:
        print("Device has no .start(period=...) method — stopping.")
        gd.quit(); return

    print("Reading for ~5 seconds (printing lists of numbers each tick)...")
    try:
        for _ in range(50):
            vals = dev.read()
            if vals is not None:
                print(vals)
            sleep(0.1)
    finally:
        try:
            dev.stop()
        except Exception:
            pass
        gd.quit()

if __name__ == "__main__":
    main()
