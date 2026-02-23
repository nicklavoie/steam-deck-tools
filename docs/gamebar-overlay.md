# Xbox Game Bar Performance Overlay

This repo now includes two **new** projects for an Xbox Game Bar overlay path without changing existing .NET 6 projects:

- `NickGameBar.TelemetryHost` (`net8.0-windows` desktop host)
- `NickGameBar.GameBarWidget` (Xbox Game Bar UWP widget)

## Architecture

1. `NickGameBar.TelemetryHost` reuses Steam Deck Tools telemetry collectors (`PerformanceOverlay.Sensors` + `CommonHelpers.Instance`) on the desktop process side.
2. Every 500ms, the host writes one JSON snapshot line to a named pipe server: `NickGameBar.Telemetry.v1`.
3. `NickGameBar.GameBarWidget` connects as a named pipe client and renders compact / expanded UI in Game Bar.
4. If pipe connection fails, widget shows a clear **"Service not running"** state.

## Build steps (Windows)

1. Install Visual Studio 2022 with:
   - .NET 8 SDK
   - Universal Windows Platform workload
   - Windows 10 SDK (19041+)
2. Open `SteamDeckTools.sln` (or open each project directly).
3. Build host:
   - `NickGameBar.TelemetryHost` as `x64`.
4. Build widget package:
   - `NickGameBar.GameBarWidget` as `x64` sideload package (`.msix` / `.appxbundle`).

## Run steps

1. Launch `NickGameBar.TelemetryHost.exe`.
2. Install/sideload `NickGameBar.GameBarWidget` package on the same PC.
3. Open Xbox Game Bar (`Win + G`), add the widget.
4. Pin widget if desired (controller + touch friendly layout).

## Sideloading notes

- Enable Developer Mode in Windows Settings.
- Install signing certificate used by your generated package before installing appx/msix.
- Rebuild and reinstall the widget package after manifest changes.
- Note: widget logo assets are linked from existing `docs/images/perf_overlay_full.png` to avoid adding new binary files in this branch.

## Constraints intentionally kept

- No Armoury Crate integration.
- No moving hardware telemetry collection into widget sandbox.
- Widget is display-only; host remains telemetry source of truth.
