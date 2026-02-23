# ROG Ally X Performance Overlay (Fork)

This repository is a personal fork of Steam Deck Tools, customized for my ROG Ally X setup and workflow.

## Scope

This fork is focused on `PerformanceOverlay` only.

Included projects:

- `PerformanceOverlay`
- `CommonHelpers`
- `ExternalHelpers`

The other original applications are intentionally not part of active development in this fork.

## Build

```powershell
dotnet restore PerformanceOverlay/PerformanceOverlay.csproj
dotnet build PerformanceOverlay/PerformanceOverlay.csproj --configuration Release
```

## CI

GitHub Actions is configured to build only `PerformanceOverlay` and publish a ZIP artifact.

## Install latest GitHub build artifact (PowerShell)

To update an existing install directory with files from the latest successful `PerformanceOverlay` GitHub Actions build artifact:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install_from_github_build.ps1 -DestinationDir "C:\SteamDeckTools"
```

Useful options:

- `-Repository owner/repo` (default: `nicklavoie/steam-deck-tools`)
- `-Ref <branch>` (default: `main`)
- `-ArtifactNamePattern "PerformanceOverlay-*.zip"`
- `-CleanDestination` (remove destination contents before copying)
- `-GitHubToken <token>` (or set `GITHUB_TOKEN`) for higher API rate limits

## Credits

Based on the original Steam Deck Tools project by Kamil Trzciński.

## License

This fork remains under the original project license:
[Creative Commons Attribution-NonCommercial-ShareAlike (CC-BY-NC-SA)](http://creativecommons.org/licenses/by-nc-sa/4.0/).

-------------------------------------------






# (Windows) Steam Deck Tools

[![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/ayufan/steam-deck-tools?label=stable&style=flat-square)](https://github.com/ayufan/steam-deck-tools/releases/latest)
[![GitHub release (latest SemVer including pre-releases)](https://img.shields.io/github/v/release/ayufan/steam-deck-tools?color=red&include_prereleases&label=beta&style=flat-square)](https://github.com/ayufan/steam-deck-tools/releases)
![GitHub all releases](https://img.shields.io/github/downloads/ayufan/steam-deck-tools/total?style=flat-square)

This repository contains my own personal set of tools to help running Windows on Steam Deck.

**This software is provided on best-effort basis and can break your SteamDeck.**

<img src="docs/images/overlay.png" height="400"/>

## Help this project

**Consider donating if you are happy with this project:**

<a href='https://ko-fi.com/ayufan' target='_blank'><img height='35' style='border:0px;height:50px;' src='https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0' alt='Buy Me a Coffee at ko-fi.com' /></a> <a href="https://www.paypal.com/donate/?hosted_button_id=DHNBE2YR9D5Y2" target='_blank'><img height='35' src="https://raw.githubusercontent.com/stefan-niedermann/paypal-donate-button/master/paypal-donate-button.png" alt="Donate with PayPal" style='border:0px;height:55px;'/></a>

## Install

See all instructions here: [https://steam-deck-tools.ayufan.dev/](https://steam-deck-tools.ayufan.dev/).

## Applications

This project provides the following applications:

- [Fan Control](https://steam-deck-tools.ayufan.dev/fan-control) - control Fan on Windows
- [Performance Overlay](https://steam-deck-tools.ayufan.dev/performance-overlay) - see FPS and other stats
- [Power Control](https://steam-deck-tools.ayufan.dev/power-control) - change TDP or refresh rate
- [Steam Controller](https://steam-deck-tools.ayufan.dev/steam-controller) - use Steam Deck with Game Pass

## Additional informations

- [Controller Shortcuts](https://steam-deck-tools.ayufan.dev/shortcuts) - default shortcuts when using [Steam Controller](https://steam-deck-tools.ayufan.dev/steam-controller).
- [Development](https://steam-deck-tools.ayufan.dev/development) - how to compile this project.
- [Risks](https://steam-deck-tools.ayufan.dev/risks) - this project uses kernel manipulation and might result in unstable system.
- [Privacy](https://steam-deck-tools.ayufan.dev/privacy) - this project can connect to remote server to check for auto-updates or track errors
- [Troubleshooting](https://steam-deck-tools.ayufan.dev/troubleshooting) - if you encounter any problems.

## Join Us

Join Us for help or chat. We are at [Official WindowsOnDeck](https://discord.gg/uF7kd33u7u) Discord server.

## Anti-Cheat and Antivirus software

[READ IF PLAYING ONLINE GAMES AND/OR GAMES THAT HAVE ANTI-CHEAT ENABLED](https://steam-deck-tools.ayufan.dev/#anti-cheat-and-antivirus-software).

## Author

Kamil Trzciński, 2022-2024

Steam Deck Tools is not affiliated with Valve, Steam, or any of their partners.

## License

[Creative Commons Attribution-NonCommercial-ShareAlike (CC-BY-NC-SA)](http://creativecommons.org/licenses/by-nc-sa/4.0/).

Free for personal use. Contact me in other cases (`ayufan@ayufan.eu`).

## Game Bar build artifact

A dedicated workflow (`.github/workflows/build_gamebar_overlay.yaml`) now builds:
- `NickGameBar.TelemetryHost` (desktop host)
- `NickGameBar.GameBarWidget` app package output for sideload

The artifact zip contains:
- `Program Files/Nick Lavoie/PerformanceOverlay/NickGameBar.TelemetryHost`
- `AppPackages/NickGameBar.GameBarWidget`

To install the latest telemetry host from that artifact to the same root used by PerformanceOverlay:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/install_gamebar_from_github_build.ps1 -DestinationRoot "C:\Program Files\Nick Lavoie\PerformanceOverlay"
```
