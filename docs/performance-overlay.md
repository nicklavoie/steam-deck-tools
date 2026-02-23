# Performance Overlay

This is a very simple application that requires [Rivatuner Statistics Server Download](https://www.guru3d.com/files-details/rtss-rivatuner-statistics-server-download.html)
and provides asthetics of SteamOS Performance Overlay.

Uninstall MSI Afterburner and any other OSD software.

It currently registers two global hotkeys:

- **Shift+F11** - enable performance overlay
- **Alt+Shift+F11** - cycle to next performance overlay (and enable it)

It also supports command URIs (used by the Xbox Game Bar widget in this fork):

- `steamdecktools-performanceoverlay://mode/fps`
- `steamdecktools-performanceoverlay://mode/fpswithbattery`
- `steamdecktools-performanceoverlay://mode/battery`
- `steamdecktools-performanceoverlay://mode/minimal`
- `steamdecktools-performanceoverlay://mode/detail`
- `steamdecktools-performanceoverlay://mode/full`
- `steamdecktools-performanceoverlay://cycle`
- `steamdecktools-performanceoverlay://show`
- `steamdecktools-performanceoverlay://hide`
- `steamdecktools-performanceoverlay://toggle`

Equivalent command-line switches are available:

- `--mode <fps|fpswithbattery|battery|minimal|detail|full>`
- `--cycle`
- `--show`
- `--hide`
- `--toggle`

There are 5 modes of presentation:

## 1. FPS

<img src="images/perf_overlay_fps.png" width="600"/>

## 2. FPS with Battery

<img src="images/perf_overlay_fpsbat.png" width="600"/>

## 3. Minimal

<img src="images/perf_overlay_min.png" width="600"/>

## 4. Detail

<img src="images/perf_overlay_detail.png" width="600"/>

## 5. Full

<img src="images/perf_overlay_full.png" height="100"/>
