# list_sensors.py
from godirect import GoDirect

gd = GoDirect(use_ble=False)  # USB only
dev = gd.get_device()
if not dev:
    print("No Go Direct device found over USB.")
    print("Enumerated devices:", GoDirect.list_devices(use_ble=False))
    gd.quit(); raise SystemExit

print("Connected to:", getattr(dev, "name", "Go Direct"))

# Most godirect builds store sensors in a private dict: dev._sensors
smap = getattr(dev, "_sensors", None)
if not isinstance(smap, dict) or not smap:
    print("No sensors dictionary found on device object.")
    gd.quit(); raise SystemExit

print("\nRaw sensor keys present on device:")
for k in sorted(smap.keys()):
    s = smap[k]
    print(f"  key={k:<4} number={getattr(s, 'number', '?'):<4} name={s.name} [{s.unit}] enabled={s.enabled}")

gd.quit()
