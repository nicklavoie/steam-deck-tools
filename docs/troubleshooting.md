# Troubleshooting

If `PerformanceOverlay` crashes, those steps might help find the problem:

1. Ensure dependencies (Visual C++ Runtime and RTSS) are installed.
2. Find error in [Event Viewer](#event-viewer).

## Event Viewer

1. Open `Event Viewer` in Windows.
1. Under `Custom Events > Administrative Events` find recent `.NET Runtime`.
1. Errors registered for this fork will have **Application:** set to:
    - `PerformanceOverlay.exe`
1. Open issue in [GitHub](https://github.com/nicklavoie/steam-deck-tools)
  with details what happened, when and included `exception`.
    ![](images/event_viewer.png)
