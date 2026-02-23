using CommonHelpers;
using Microsoft.Win32;

namespace PerformanceOverlay
{
    internal static class UriProtocolRegistration
    {
        private const string ProtocolKey = @"Software\Classes\steamdecktools-performanceoverlay";

        public static void EnsureRegistered()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (String.IsNullOrWhiteSpace(exePath))
                    return;

                using var protocol = Registry.CurrentUser.CreateSubKey(ProtocolKey);
                if (protocol is null)
                    return;

                protocol.SetValue("", "URL:Steam Deck Tools Performance Overlay");
                protocol.SetValue("URL Protocol", "");

                using var icon = protocol.CreateSubKey("DefaultIcon");
                icon?.SetValue("", "\"" + exePath + "\",0");

                using var command = protocol.CreateSubKey(@"shell\open\command");
                command?.SetValue("", "\"" + exePath + "\" \"%1\"");
            }
            catch (Exception ex)
            {
                Log.TraceLine("UriProtocolRegistration: {0}", ex.Message);
            }
        }
    }
}
