using HidSharp;
using RGB.NET.Core;
using Serilog;

namespace Artemis.Plugins.Colorful;

internal sealed class ColorfulRgbProvider(ILogger logger) : AbstractRGBDeviceProvider
{
    // No independent rate limit: Artemis drives color updates at its configured FPS.
    // DeviceUpdateTrigger waits for queued color data; it does not free-run.
    protected override void InitializeSDK() { }

    protected override IEnumerable<IRGBDevice> LoadDevices()
    {
        // Only claim the user's tested controller, and only its lighting collection.
        foreach (HidDevice hid in DeviceList.Local.GetHidDevices(0x2f4c, 0x1017))
        {
            ColorfulUpdateQueue? queue = null;
            try
            {
                string path = hid.DevicePath;
                if (!path.Contains("mi_01", StringComparison.OrdinalIgnoreCase) &&
                    !path.Contains("mi_02", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (hid.GetMaxOutputReportLength() != ColorfulProtocol.ReportLength ||
                    !hid.GetReportDescriptor().DeviceItems.Any(item => item.Usages.GetAllValues().Contains(0xff010001u)))
                    continue;

                HidStream stream = hid.Open();
                try
                {
                    stream.WriteTimeout = 1000;
                    queue = new ColorfulUpdateQueue(GetUpdateTrigger(), stream, logger);
                }
                catch
                {
                    stream.Dispose();
                    throw;
                }
                logger.Information("Colorful B760M lighting controller opened: {DevicePath}", path);
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "Cannot open Colorful HID endpoint {DevicePath}", hid.DevicePath);
            }

            if (queue == null) continue;
            for (int channel = 0; channel < 3; channel++)
                yield return new ColorfulStrip(channel, queue);
            // MI_01/MI_02 are alternatives, never register the controller twice.
            yield break;
        }
    }
}
