using CommonHelpers;
using System.Linq;

namespace PerformanceOverlay
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Instance.WithSentry(() =>
            {
                UriProtocolRegistration.EnsureRegistered();

                var commandResult = OverlayCommandLine.HandleArgs(Environment.GetCommandLineArgs().Skip(1).ToArray());
                if (commandResult == OverlayControlResult.HandledAndExit)
                    return;

                ApplicationConfiguration.Initialize();

                using (var controller = new Controller())
                {
                    Application.Run();
                }
            });
        }
    }
}
