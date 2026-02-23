using CommonHelpers;
using Microsoft.Win32;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;

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
                    exePath = Application.ExecutablePath;
                if (String.IsNullOrWhiteSpace(exePath))
                    exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (String.IsNullOrWhiteSpace(exePath))
                {
                    Log.TraceLine("UriProtocolRegistration: executable path could not be resolved.");
                    return;
                }

                EnsureRegisteredForRoot(Registry.CurrentUser, exePath, "HKCU");

                if (IsElevated())
                    EnsureRegisteredForRoot(Registry.LocalMachine, exePath, "HKLM");
            }
            catch (Exception ex)
            {
                Log.TraceException("UriProtocolRegistration", ex);
            }
        }

        private static void EnsureRegisteredForRoot(RegistryKey root, string exePath, string rootName)
        {
            try
            {
                using var protocol = root.CreateSubKey(ProtocolKey);
                if (protocol is null)
                {
                    Log.TraceLine("UriProtocolRegistration: failed to open protocol key in {0}.", rootName);
                    return;
                }

                protocol.SetValue("", "URL:Steam Deck Tools Performance Overlay");
                protocol.SetValue("URL Protocol", "");

                using var icon = protocol.CreateSubKey("DefaultIcon");
                icon?.SetValue("", "\"" + exePath + "\",0");

                using var command = protocol.CreateSubKey(@"shell\open\command");
                command?.SetValue("", "\"" + exePath + "\" \"%1\"");

                var registered = command?.GetValue("")?.ToString();
                Log.TraceLine("UriProtocolRegistration: {0} registered command: {1}", rootName, registered ?? "<null>");
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.TraceLine("UriProtocolRegistration: {0} access denied: {1}", rootName, ex.Message);
            }
        }

        private static bool IsElevated()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}
