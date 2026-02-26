using CommonHelpers;

namespace PerformanceOverlay
{
    internal enum OverlayControlResult
    {
        NotACommand,
        HandledAndExit,
        ContinueStartup
    }

    internal enum OverlayControlCommandType
    {
        SetMode,
        SetPosition,
        CycleMode,
        ToggleVisibility,
        Show,
        Hide
    }

    internal readonly struct OverlayControlCommand
    {
        public OverlayControlCommandType CommandType { get; }
        public OverlayMode Mode { get; }
        public OverlayPosition Position { get; }

        private OverlayControlCommand(
            OverlayControlCommandType commandType,
            OverlayMode mode = OverlayMode.FPS,
            OverlayPosition position = OverlayPosition.TopLeft)
        {
            CommandType = commandType;
            Mode = mode;
            Position = position;
        }

        public static OverlayControlCommand SetMode(OverlayMode mode)
        {
            return new OverlayControlCommand(OverlayControlCommandType.SetMode, mode);
        }

        public static OverlayControlCommand SetPosition(OverlayPosition position)
        {
            return new OverlayControlCommand(OverlayControlCommandType.SetPosition, position: position);
        }

        public static OverlayControlCommand CycleMode()
        {
            return new OverlayControlCommand(OverlayControlCommandType.CycleMode);
        }

        public static OverlayControlCommand ToggleVisibility()
        {
            return new OverlayControlCommand(OverlayControlCommandType.ToggleVisibility);
        }

        public static OverlayControlCommand Show()
        {
            return new OverlayControlCommand(OverlayControlCommandType.Show);
        }

        public static OverlayControlCommand Hide()
        {
            return new OverlayControlCommand(OverlayControlCommandType.Hide);
        }
    }

    internal static class OverlayCommandLine
    {
        internal const string ProtocolScheme = "steamdecktools-performanceoverlay";
        private const string RunOnceMutexName = "Global\\PerformanceOverlay";

        public static OverlayControlResult HandleArgs(string[] args)
        {
            if (!TryParseCommand(args, out var command))
                return OverlayControlResult.NotACommand;

            if (TryDispatchToRunningInstance(command))
                return OverlayControlResult.HandledAndExit;

            // Avoid run-once fatal dialog if an existing instance is present but IPC failed.
            if (IsPerformanceOverlayInstanceRunning())
                return OverlayControlResult.HandledAndExit;

            ApplyLocally(command);
            return OverlayControlResult.ContinueStartup;
        }

        private static bool TryParseCommand(string[] args, out OverlayControlCommand command)
        {
            command = default;

            foreach (var arg in args)
            {
                if (TryParseUriCommand(arg, out command))
                    return true;
            }

            return TryParseSwitchCommand(args, out command);
        }

        private static bool TryParseUriCommand(string arg, out OverlayControlCommand command)
        {
            command = default;

            if (!Uri.TryCreate(arg, UriKind.Absolute, out var uri))
                return false;

            if (!uri.Scheme.Equals(ProtocolScheme, StringComparison.OrdinalIgnoreCase))
                return false;

            var segments = new List<string>();
            if (!String.IsNullOrWhiteSpace(uri.Host))
                segments.Add(uri.Host);

            segments.AddRange(
                uri.AbsolutePath
                    .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            );

            if (segments.Count == 0)
                return false;

            var head = segments[0];
            if (head.Equals("cycle", StringComparison.OrdinalIgnoreCase))
            {
                command = OverlayControlCommand.CycleMode();
                return true;
            }

            if (head.Equals("toggle", StringComparison.OrdinalIgnoreCase))
            {
                command = OverlayControlCommand.ToggleVisibility();
                return true;
            }

            if (head.Equals("show", StringComparison.OrdinalIgnoreCase))
            {
                command = OverlayControlCommand.Show();
                return true;
            }

            if (head.Equals("hide", StringComparison.OrdinalIgnoreCase))
            {
                command = OverlayControlCommand.Hide();
                return true;
            }

            if ((head.Equals("mode", StringComparison.OrdinalIgnoreCase) || head.Equals("set", StringComparison.OrdinalIgnoreCase))
                && segments.Count > 1
                && TryParseMode(segments[1], out var uriMode))
            {
                command = OverlayControlCommand.SetMode(uriMode);
                return true;
            }

            if ((head.Equals("position", StringComparison.OrdinalIgnoreCase) || head.Equals("pos", StringComparison.OrdinalIgnoreCase))
                && segments.Count > 1
                && TryParsePosition(segments[1], out var uriPosition))
            {
                command = OverlayControlCommand.SetPosition(uriPosition);
                return true;
            }

            return false;
        }

        private static bool TryParseSwitchCommand(string[] args, out OverlayControlCommand command)
        {
            command = default;
            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals("--cycle", StringComparison.OrdinalIgnoreCase))
                {
                    command = OverlayControlCommand.CycleMode();
                    return true;
                }

                if (arg.Equals("--toggle", StringComparison.OrdinalIgnoreCase))
                {
                    command = OverlayControlCommand.ToggleVisibility();
                    return true;
                }

                if (arg.Equals("--show", StringComparison.OrdinalIgnoreCase))
                {
                    command = OverlayControlCommand.Show();
                    return true;
                }

                if (arg.Equals("--hide", StringComparison.OrdinalIgnoreCase))
                {
                    command = OverlayControlCommand.Hide();
                    return true;
                }

                if (arg.StartsWith("--mode=", StringComparison.OrdinalIgnoreCase))
                {
                    var modeArg = arg["--mode=".Length..];
                    if (TryParseMode(modeArg, out var inlineMode))
                    {
                        command = OverlayControlCommand.SetMode(inlineMode);
                        return true;
                    }
                }

                if (arg.Equals("--mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && TryParseMode(args[i + 1], out var nextMode))
                {
                    command = OverlayControlCommand.SetMode(nextMode);
                    return true;
                }

                if (arg.StartsWith("--position=", StringComparison.OrdinalIgnoreCase))
                {
                    var positionArg = arg["--position=".Length..];
                    if (TryParsePosition(positionArg, out var inlinePosition))
                    {
                        command = OverlayControlCommand.SetPosition(inlinePosition);
                        return true;
                    }
                }

                if (arg.Equals("--position", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length && TryParsePosition(args[i + 1], out var nextPosition))
                {
                    command = OverlayControlCommand.SetPosition(nextPosition);
                    return true;
                }
            }

            return false;
        }

        private static bool TryParseMode(string value, out OverlayMode mode)
        {
            if (Enum.TryParse<OverlayMode>(value, true, out mode))
                return true;

            var normalized = value.Trim().Replace("-", "").Replace("_", "").ToLowerInvariant();
            switch (normalized)
            {
                case "fpsbattery":
                case "fpswithbattery":
                    mode = OverlayMode.FPSWithBattery;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParsePosition(string value, out OverlayPosition position)
        {
            if (Enum.TryParse<OverlayPosition>(value, true, out position))
                return true;

            var normalized = value.Trim().Replace("-", "").Replace("_", "").ToLowerInvariant();
            switch (normalized)
            {
                case "topleft":
                case "tl":
                    position = OverlayPosition.TopLeft;
                    return true;
                case "topright":
                case "tr":
                    position = OverlayPosition.TopRight;
                    return true;
                case "bottomleft":
                case "bl":
                    position = OverlayPosition.BottomLeft;
                    return true;
                case "bottomright":
                case "br":
                    position = OverlayPosition.BottomRight;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryDispatchToRunningInstance(OverlayControlCommand command)
        {
            try
            {
                using var sharedData = SharedData<OverlayModeSetting>.OpenExisting();
                if (!sharedData.GetValue(out var value))
                    value = sharedData.NewValue();

                ApplyToSharedState(ref value, command);
                return sharedData.SetValue(value);
            }
            catch (Exception ex)
            {
                Log.TraceLine("OverlayCommandLine: IPC dispatch failed: {0}", ex.Message);
                return false;
            }
        }

        private static void ApplyToSharedState(ref OverlayModeSetting value, OverlayControlCommand command)
        {
            switch (command.CommandType)
            {
                case OverlayControlCommandType.SetMode:
                    value.Desired = command.Mode;
                    value.DesiredEnabled = OverlayEnabled.Yes;
                    return;
                case OverlayControlCommandType.SetPosition:
                    value.DesiredPosition = command.Position;
                    return;
                case OverlayControlCommandType.CycleMode:
                    value.Desired = NextMode(CurrentMode(value));
                    value.DesiredEnabled = OverlayEnabled.Yes;
                    return;
                case OverlayControlCommandType.ToggleVisibility:
                    value.DesiredEnabled = CurrentEnabled(value) == OverlayEnabled.Yes ? OverlayEnabled.No : OverlayEnabled.Yes;
                    return;
                case OverlayControlCommandType.Show:
                    value.DesiredEnabled = OverlayEnabled.Yes;
                    return;
                case OverlayControlCommandType.Hide:
                    value.DesiredEnabled = OverlayEnabled.No;
                    return;
                default:
                    return;
            }
        }

        private static OverlayMode CurrentMode(OverlayModeSetting value)
        {
            if (Enum.IsDefined<OverlayMode>(value.Current))
                return value.Current;
            if (Enum.IsDefined<OverlayMode>(value.Desired))
                return value.Desired;
            return Settings.Default.OSDMode;
        }

        private static OverlayEnabled CurrentEnabled(OverlayModeSetting value)
        {
            if (Enum.IsDefined<OverlayEnabled>(value.CurrentEnabled))
                return value.CurrentEnabled;
            if (Enum.IsDefined<OverlayEnabled>(value.DesiredEnabled))
                return value.DesiredEnabled;
            return Settings.Default.ShowOSD ? OverlayEnabled.Yes : OverlayEnabled.No;
        }

        private static void ApplyLocally(OverlayControlCommand command)
        {
            switch (command.CommandType)
            {
                case OverlayControlCommandType.SetMode:
                    Settings.Default.OSDMode = command.Mode;
                    Settings.Default.ShowOSD = true;
                    return;
                case OverlayControlCommandType.SetPosition:
                    Settings.Default.OSDPosition = command.Position;
                    return;
                case OverlayControlCommandType.CycleMode:
                    Settings.Default.OSDMode = NextMode(Settings.Default.OSDMode);
                    Settings.Default.ShowOSD = true;
                    return;
                case OverlayControlCommandType.ToggleVisibility:
                    Settings.Default.ShowOSD = !Settings.Default.ShowOSD;
                    return;
                case OverlayControlCommandType.Show:
                    Settings.Default.ShowOSD = true;
                    return;
                case OverlayControlCommandType.Hide:
                    Settings.Default.ShowOSD = false;
                    return;
                default:
                    return;
            }
        }

        private static OverlayMode NextMode(OverlayMode current)
        {
            var values = Enum.GetValues<OverlayMode>();
            var currentIndex = Array.IndexOf(values, current);
            if (currentIndex < 0)
                currentIndex = 0;

            return values[(currentIndex + 1) % values.Length];
        }

        private static bool IsPerformanceOverlayInstanceRunning()
        {
            try
            {
                using var mutex = Mutex.OpenExisting(RunOnceMutexName);
                return true;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }
    }
}
