using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SteamDeckToolsGameBarWidget
{
    public sealed partial class WidgetPage : Page
    {
        private const string TelemetryFileName = "telemetry.txt";
        private const string TelemetryStartUri = "steamdecktools-performanceoverlay://telemetry/start";
        private const string TelemetryStopUri = "steamdecktools-performanceoverlay://telemetry/stop";
        private readonly DispatcherTimer telemetryTimer = new DispatcherTimer();
        private bool telemetryExpanded;
        private bool telemetryReadInProgress;

        public WidgetPage()
        {
            InitializeComponent();

            telemetryTimer.Interval = TimeSpan.FromMilliseconds(750);
            telemetryTimer.Tick += TelemetryTimer_Tick;
            telemetryTimer.Start();

            Unloaded += WidgetPage_Unloaded;
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

        private async void TelemetryToggleButton_Click(object sender, RoutedEventArgs e)
        {
            telemetryExpanded = !telemetryExpanded;
            TelemetryPanel.Visibility = telemetryExpanded ? Visibility.Visible : Visibility.Collapsed;
            TelemetryToggleButton.Content = telemetryExpanded ? "Hide" : "Show";

            if (telemetryExpanded)
            {
                await SendTelemetryStreamCommandAsync(true);
                await UpdateTelemetryTextAsync();
            }
            else
            {
                await SendTelemetryStreamCommandAsync(false);
            }
        }

        private void TelemetryTimer_Tick(object sender, object e)
        {
            if (!telemetryExpanded)
                return;

            _ = UpdateTelemetryTextAsync();
        }

        private void WidgetPage_Unloaded(object sender, RoutedEventArgs e)
        {
            _ = SendTelemetryStreamCommandAsync(false);
            telemetryTimer.Stop();
            telemetryTimer.Tick -= TelemetryTimer_Tick;
            Unloaded -= WidgetPage_Unloaded;
        }

        private async Task SendTelemetryStreamCommandAsync(bool enabled)
        {
            var targetUri = enabled ? TelemetryStartUri : TelemetryStopUri;

            try
            {
                Uri uri;
                if (!Uri.TryCreate(targetUri, UriKind.Absolute, out uri))
                    return;

                var app = Application.Current as App;
                if (app?.ActiveWidget != null)
                    await app.ActiveWidget.LaunchUriAsync(uri);
                else
                    await Launcher.LaunchUriAsync(uri);
            }
            catch
            {
            }
        }

        private async Task UpdateTelemetryTextAsync()
        {
            if (telemetryReadInProgress)
                return;

            telemetryReadInProgress = true;
            try
            {
                StorageFile file = await ApplicationData.Current.LocalFolder.GetFileAsync(TelemetryFileName);
                string content = await FileIO.ReadTextAsync(file);

                if (string.IsNullOrWhiteSpace(content))
                {
                    TelemetryText.Text = "Telemetry unavailable. Waiting for PerformanceOverlay telemetry file.";
                    return;
                }

                TelemetryText.Text = content.TrimEnd() + Environment.NewLine + "UPDATED_WIDGET=" + DateTime.Now.ToString("HH:mm:ss.fff");
            }
            catch (FileNotFoundException)
            {
                TelemetryText.Text = "Telemetry unavailable. Waiting for PerformanceOverlay telemetry file.";
            }
            catch (Exception ex)
            {
                TelemetryText.Text = "Telemetry read failed: 0x" + ex.HResult.ToString("X8");
            }
            finally
            {
                telemetryReadInProgress = false;
            }
        }
    }
}
