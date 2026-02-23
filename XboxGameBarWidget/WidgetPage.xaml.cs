using System;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SteamDeckToolsGameBarWidget
{
    public sealed partial class WidgetPage : Page
    {
        public WidgetPage()
        {
            InitializeComponent();
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

                bool launched = await Launcher.LaunchUriAsync(uri);
                StatusText.Text = launched
                    ? "Sent command: " + button.Content
                    : "Unable to launch command URI. Start PerformanceOverlay first.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Failed to send command: " + ex.Message;
            }
        }
    }
}
