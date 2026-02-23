using System;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SteamDeckToolsGameBarWidget
{
    public sealed partial class WidgetPage : Page
    {
        private const string OverlayModeSharedDataName = "Global_OverlayModeSetting_Setting";
        private const string OverlayTelemetrySharedDataName = "Global_OverlayTelemetrySnapshot_Setting";
        private readonly DispatcherTimer telemetryTimer = new DispatcherTimer();
        private bool telemetryExpanded;

        [StructLayout(LayoutKind.Sequential)]
        private struct OverlayModeSettingSnapshot
        {
            public uint Current;
            public uint Desired;
            public uint CurrentEnabled;
            public uint DesiredEnabled;
            public uint KernelDriversLoaded;
            public uint DesiredKernelDriversLoaded;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct OverlayTelemetrySnapshot
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

        public WidgetPage()
        {
            InitializeComponent();

            telemetryTimer.Interval = TimeSpan.FromMilliseconds(750);
            telemetryTimer.Tick += TelemetryTimer_Tick;
            telemetryTimer.Start();

            Unloaded += WidgetPage_Unloaded;
            UpdateTelemetryText();
        }

        private async void CommandButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null || !(button.Tag is string))
                return;

            try
            {
                var targetUri = (string)button.Tag;
                Uri uri;
                if (!Uri.TryCreate(targetUri, UriKind.Absolute, out uri))
                {
                    StatusText.Text = "Invalid command URI.";
                    return;
                }

                var app = Application.Current as App;
                bool launched = false;
                if (app?.ActiveWidget != null)
                    launched = await app.ActiveWidget.LaunchUriAsync(uri);
                else
                    launched = await Launcher.LaunchUriAsync(uri);

                StatusText.Text = launched
                    ? "Sent command: " + button.Content
                    : "Unable to launch command URI. Run PerformanceOverlay once to register protocol, and avoid forcing elevated startup.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to send command: 0x" + ex.HResult.ToString("X8") + " " + ex.Message;
            }
        }

        private void TelemetryToggleButton_Click(object sender, RoutedEventArgs e)
        {
            telemetryExpanded = !telemetryExpanded;
            TelemetryPanel.Visibility = telemetryExpanded ? Visibility.Visible : Visibility.Collapsed;
            TelemetryToggleButton.Content = telemetryExpanded ? "Hide" : "Show";

            if (telemetryExpanded)
                UpdateTelemetryText();
        }

        private void TelemetryTimer_Tick(object sender, object e)
        {
            UpdateTelemetryText();
        }

        private void WidgetPage_Unloaded(object sender, RoutedEventArgs e)
        {
            telemetryTimer.Stop();
            telemetryTimer.Tick -= TelemetryTimer_Tick;
            Unloaded -= WidgetPage_Unloaded;
        }

        private void UpdateTelemetryText()
        {
            var output = new StringBuilder();

            if (TryReadSharedData<OverlayTelemetrySnapshot>(OverlayTelemetrySharedDataName, out var telemetry, out var telemetryError))
            {
                output.AppendLine("CPU_%=" + FormatValue(telemetry.CPU_Percent));
                output.AppendLine("CPU_W=" + FormatValue(telemetry.CPU_Watts));
                output.AppendLine("CPU_T=" + FormatValue(telemetry.CPU_Temperature));
                output.AppendLine("CPU_MHZ=" + FormatValue(telemetry.CPU_MHz));
                output.AppendLine("MEM_GB=" + FormatValue(telemetry.MEM_GB));
                output.AppendLine("MEM_MB=" + FormatValue(telemetry.MEM_MB));
                output.AppendLine("GPU_%=" + FormatValue(telemetry.GPU_Percent));
                output.AppendLine("GPU_MB=" + FormatValue(telemetry.GPU_MB));
                output.AppendLine("GPU_GB=" + FormatValue(telemetry.GPU_GB));
                output.AppendLine("GPU_W=" + FormatValue(telemetry.GPU_Watts));
                output.AppendLine("GPU_MHZ=" + FormatValue(telemetry.GPU_MHz));
                output.AppendLine("GPU_T=" + FormatValue(telemetry.GPU_Temperature));
                output.AppendLine("BATT_%=" + FormatValue(telemetry.BATT_Percent));
                output.AppendLine("BATT_MIN=" + FormatValue(telemetry.BATT_Minutes));
                output.AppendLine("BATT_W=" + FormatValue(telemetry.BATT_DischargeWatts));
                output.AppendLine("BATT_CHARGE_W=" + FormatValue(telemetry.BATT_ChargeWatts));
                output.AppendLine("FAN_RPM=" + FormatValue(telemetry.FAN_RPM));
                output.AppendLine("TELEMETRY_SEQ=" + telemetry.Sequence);
                output.AppendLine("TELEMETRY_TS_UNIX=" + telemetry.TimestampUnixSeconds);
            }
            else
            {
                output.AppendLine(telemetryError);
            }

            if (TryReadSharedData<OverlayModeSettingSnapshot>(OverlayModeSharedDataName, out var state, out var stateError))
            {
                output.AppendLine();
                output.AppendLine("OVERLAY_CURRENT_MODE=" + state.Current + " (" + OverlayModeLabel(state.Current) + ")");
                output.AppendLine("OVERLAY_DESIRED_MODE=" + state.Desired + " (" + OverlayModeLabel(state.Desired) + ")");
                output.AppendLine("OVERLAY_CURRENT_ENABLED=" + state.CurrentEnabled + " (" + OverlayEnabledLabel(state.CurrentEnabled) + ")");
                output.AppendLine("OVERLAY_DESIRED_ENABLED=" + state.DesiredEnabled + " (" + OverlayEnabledLabel(state.DesiredEnabled) + ")");
                output.AppendLine("OVERLAY_KERNEL=" + state.KernelDriversLoaded + " (" + KernelDriversLabel(state.KernelDriversLoaded) + ")");
                output.AppendLine("OVERLAY_DESIRED_KERNEL=" + state.DesiredKernelDriversLoaded + " (" + KernelDriversLabel(state.DesiredKernelDriversLoaded) + ")");
            }
            else
            {
                output.AppendLine();
                output.AppendLine(stateError);
            }

            output.AppendLine();
            output.AppendLine("UPDATED_LOCAL=" + DateTime.Now.ToString("HH:mm:ss.fff"));

            TelemetryText.Text = output.ToString().TrimEnd();
        }

        private static string FormatValue(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
                return "n/a";

            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static bool TryReadSharedData<T>(string mapName, out T snapshot, out string errorText) where T : struct
        {
            snapshot = default(T);
            errorText = string.Empty;

            var payloadSize = Marshal.SizeOf(typeof(T));

            try
            {
                using (var mmf = MemoryMappedFile.OpenExisting(mapName, MemoryMappedFileRights.Read))
                using (var stream = mmf.CreateViewStream(0, payloadSize, MemoryMappedFileAccess.Read))
                {
                    var buffer = new byte[payloadSize];
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead < payloadSize)
                    {
                        errorText = "Telemetry payload incomplete: " + bytesRead + "/" + payloadSize;
                        return false;
                    }

                    var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                    try
                    {
                        var value = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                        if (value is T typedValue)
                        {
                            snapshot = typedValue;
                            return true;
                        }

                        errorText = "Telemetry payload decode failed.";
                        return false;
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                errorText = "Telemetry unavailable. Start PerformanceOverlay.";
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                errorText = "Telemetry unavailable. Shared memory access denied.";
                return false;
            }
            catch (Exception ex)
            {
                errorText = "Telemetry read failed: 0x" + ex.HResult.ToString("X8");
                return false;
            }
        }

        private static string OverlayModeLabel(uint value)
        {
            switch (value)
            {
                case 10032: return "FPS";
                case 10033: return "FPSWithBattery";
                case 10034: return "Battery";
                case 10035: return "Minimal";
                case 10036: return "Detail";
                case 10037: return "Full";
                default: return "Unset";
            }
        }

        private static string OverlayEnabledLabel(uint value)
        {
            switch (value)
            {
                case 378313: return "Yes";
                case 378314: return "No";
                default: return "Unset";
            }
        }

        private static string KernelDriversLabel(uint value)
        {
            switch (value)
            {
                case 4363232: return "Yes";
                case 4363233: return "No";
                default: return "Unset";
            }
        }
    }
}
