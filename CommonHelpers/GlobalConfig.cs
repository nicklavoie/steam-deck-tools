using System.Runtime.InteropServices;

namespace CommonHelpers
{
    public enum KernelDriversLoaded : uint
    {
        Yes = 4363232,
        No
    }

    public enum OverlayMode : uint
    {
        FPS = 10032,
        FPSWithBattery,
        Battery,
        Minimal,
        Detail,
        Full
    }

    public enum OverlayEnabled : uint
    {
        Yes = 378313,
        No
    }

    public enum OverlayPosition : uint
    {
        TopLeft = 901271,
        TopRight,
        BottomLeft,
        BottomRight
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OverlayModeSetting
    {
        public OverlayMode Current, Desired;
        public OverlayEnabled CurrentEnabled, DesiredEnabled;
        public KernelDriversLoaded KernelDriversLoaded;
        public KernelDriversLoaded DesiredKernelDriversLoaded;
        public OverlayPosition CurrentPosition, DesiredPosition;
    }
}
