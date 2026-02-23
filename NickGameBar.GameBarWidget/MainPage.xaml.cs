using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Gaming.XboxGameBar;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace NickGameBar.GameBarWidget;

public sealed partial class MainPage : Page
{
    private readonly CancellationTokenSource _cts = new();
    private XboxGameBarWidget? _widget;

    public MainPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += (_, _) => _cts.Cancel();
    }

    protected override void OnNavigatedTo(Windows.UI.Xaml.Navigation.NavigationEventArgs e)
    {
        _widget = e.Parameter as XboxGameBarWidget;
        if (_widget != null)
        {
            _widget.SettingsClicked += (_, _) => { };
            _widget.VisibleChanged += (_, _) => UpdateLayoutMode();
            _widget.WindowStateChanged += (_, _) => UpdateLayoutMode();
            _widget.PinningSupported = true;
        }

        UpdateLayoutMode();
    }

    private void UpdateLayoutMode()
    {
        var compact = _widget?.WindowState == XboxGameBarWidgetWindowState.Minimized;
        VisualStateManager.GoToState(this, compact == true ? "CompactState" : "ExpandedState", true);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                using var pipe = new NamedPipeClientStream(".", "NickGameBar.Telemetry.v1", PipeDirection.In, PipeOptions.Asynchronous);
                await pipe.ConnectAsync(1000);
                ServiceStateText.Text = "Connected";
                ServiceStateText.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.LightGreen);

                using var reader = new StreamReader(pipe);
                while (!_cts.IsCancellationRequested && pipe.IsConnected)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    var snapshot = JsonSerializer.Deserialize<TelemetrySnapshot>(line);
                    if (snapshot != null)
                        Apply(snapshot);
                }
            }
            catch
            {
                ServiceStateText.Text = "Service not running";
                ServiceStateText.Foreground = new Windows.UI.Xaml.Media.SolidColorBrush(Windows.UI.Colors.OrangeRed);
                await Task.Delay(1000);
            }
        }
    }

    private void Apply(TelemetrySnapshot snapshot)
    {
        string Val(string key, string fallback = "--") => snapshot.Values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value! : fallback;

        CpuText.Text = $"{Val("CPU_%")}%  {Val("CPU_W")}W";
        GpuText.Text = $"{Val("GPU_%")}%  {Val("GPU_W")}W";
        RamText.Text = $"{Val("MEM_GB")} GB";
        FpsText.Text = snapshot.Fps.HasValue ? $"{snapshot.Fps.Value:F0}" : "--";

        VramText.Text = $"VRAM: {Val("GPU_GB")} GB";
        CpuTempText.Text = $"CPU Temp: {Val("CPU_T")}°C  {Val("CPU_MHZ")} MHz";
        GpuTempText.Text = $"GPU Temp: {Val("GPU_T")}°C  {Val("GPU_MHZ")} MHz";
        BatteryText.Text = $"Battery: {Val("BATT_%")}%  {Val("BATT_W")}W  {Val("BATT_MIN")} min";
        FanText.Text = $"Fan: {Val("FAN_RPM")} RPM";
        UpdatedText.Text = $"Updated: {snapshot.Timestamp:HH:mm:ss}";
    }

    public sealed class TelemetrySnapshot
    {
        public DateTimeOffset Timestamp { get; set; }
        public Dictionary<string, string?> Values { get; set; } = new();
        public double? Fps { get; set; }
        public double? FrameTimeMs { get; set; }
    }
}
