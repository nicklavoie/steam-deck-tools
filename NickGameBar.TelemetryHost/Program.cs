using System.IO.Pipes;
using System.Text.Json;
using CommonHelpers;
using PerformanceOverlay;

const string PipeName = "NickGameBar.Telemetry.v1";
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cancellation.Cancel();
};

Instance.Open("NickGameBar Telemetry Host", useKernelDrivers: false, runOnce: "Global\\NickGameBarTelemetryHost");
using var sensors = new Sensors();

while (!cancellation.IsCancellationRequested)
{
    using var pipe = new NamedPipeServerStream(PipeName, PipeDirection.Out, NamedPipeServerStream.MaxAllowedServerInstances,
        PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

    await pipe.WaitForConnectionAsync(cancellation.Token);
    using var writer = new StreamWriter(pipe) { AutoFlush = true };

    while (pipe.IsConnected && !cancellation.IsCancellationRequested)
    {
        sensors.Update();
        var snapshot = TelemetrySnapshot.Create(sensors);
        await writer.WriteLineAsync(JsonSerializer.Serialize(snapshot));
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellation.Token);
    }
}

internal sealed record TelemetrySnapshot(
    DateTimeOffset Timestamp,
    IDictionary<string, string?> Values,
    double? Fps,
    double? FrameTimeMs)
{
    public static TelemetrySnapshot Create(Sensors sensors)
    {
        var keys = new[]
        {
            "CPU_%", "CPU_W", "CPU_T", "CPU_MHZ",
            "GPU_%", "GPU_W", "GPU_T", "GPU_MHZ", "GPU_MB", "GPU_GB",
            "MEM_MB", "MEM_GB",
            "BATT_%", "BATT_W", "BATT_CHARGE_W", "BATT_MIN",
            "FAN_RPM"
        };

        var values = keys.ToDictionary(k => k, sensors.GetValue);

        // FPS/frametime are optional. Keep nullable fields in schema without a hard RTSS dependency.
        return new TelemetrySnapshot(DateTimeOffset.UtcNow, values, null, null);
    }
}
