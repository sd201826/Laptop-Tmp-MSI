# Laptop TEMP MSI

A Windows desktop temperature dashboard with a clean black-and-white, thin-line design.

## What it shows

- CPU temperature and processor load
- GPU temperature, when the laptop exposes a supported sensor
- RAM usage
- Fan speed, when reported by the laptop
- Storage temperature, when reported by the drive
- A live CPU temperature history graph

The app reads local hardware sensors with LibreHardwareMonitorLib. Sensor availability varies by laptop and firmware; unsupported readings are shown as **N/A** rather than guessed.

## Download and install

1. Open the repository's **Actions** tab.
2. Open the newest successful **Build Laptop TEMP MSI** run.
3. Under **Artifacts**, download **Laptop-TEMP-MSI-Windows-x64**.
4. Extract the ZIP and double-click **Laptop-TEMP-MSI-Setup.msi**.
5. After installation, open **Laptop TEMP MSI** from the Start menu.

The installer is per-user. The app is a normal Windows GUI application and does not open Command Prompt. A self-contained portable build is also included in the artifact ZIP.

## Sensor access

Some hardware sensors require administrator permission. The app starts normally; use its **RUN AS ADMIN** button only when a sensor is unavailable. This opens the standard Windows permission prompt. It does not change fan speeds, overclock the laptop, or modify firmware.

## Build

GitHub Actions builds the Windows x64 app and MSI installer automatically when files are pushed to the main branch. The workflow can also be run manually.

## Compatibility

Windows 10/11 x64. Temperature sensors depend on the laptop's processor, graphics hardware, embedded controller, firmware, and Windows permissions. The app cannot display a reading that the device does not expose.

## Credits

Hardware sensor access is provided by [LibreHardwareMonitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), licensed under MPL-2.0.