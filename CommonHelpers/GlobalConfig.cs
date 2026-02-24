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

    [StructLayout(LayoutKind.Sequential)]
    public struct OverlayModeSetting
    {
        public OverlayMode Current, Desired;
        public OverlayEnabled CurrentEnabled, DesiredEnabled;
        public KernelDriversLoaded KernelDriversLoaded;
        public KernelDriversLoaded DesiredKernelDriversLoaded;

        public float CPU_Percent;
        public float CPU_Watts;
        public float CPU_Temperature;
        public float CPU_MHz;

        public float MEM_GB;
        public float MEM_MB;

        public float GPU_Percent;
        public float GPU_MB;
        public float GPU_GB;
        public float GPU_Watts;
        public float GPU_MHz;
        public float GPU_Temperature;

        public float BATT_Percent;
        public float BATT_Minutes;
        public float BATT_DischargeWatts;
        public float BATT_ChargeWatts;

        public float FAN_RPM;

        public uint TelemetrySequence;
        public uint TelemetryTimestampUnixSeconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct OverlayTelemetrySnapshot
    {
        public float CPU_Percent;
        public float CPU_Watts;
        public float CPU_Temperature;
        public float CPU_MHz;

        public float MEM_GB;
        public float MEM_MB;

        public float GPU_Percent;
        public float GPU_MB;
        public float GPU_GB;
        public float GPU_Watts;
        public float GPU_MHz;
        public float GPU_Temperature;

        public float BATT_Percent;
        public float BATT_Minutes;
        public float BATT_DischargeWatts;
        public float BATT_ChargeWatts;

        public float FAN_RPM;

        public uint Sequence;
        public uint TimestampUnixSeconds;
    }
}
