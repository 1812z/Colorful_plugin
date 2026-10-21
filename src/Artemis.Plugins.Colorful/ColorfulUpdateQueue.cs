using HidSharp;
using RGB.NET.Core;
using Serilog;

namespace Artemis.Plugins.Colorful;

internal sealed class ColorfulUpdateQueue(IDeviceUpdateTrigger trigger, HidStream stream, ILogger logger)
    : UpdateQueue(trigger)
{
    private readonly object _ioLock = new();
    private readonly byte[] _frame = new byte[ColorfulProtocol.FrameLedCount * 3];
    private bool _disposed;
    private bool _failed;

    protected override bool Update(ReadOnlySpan<(object key, Color color)> dataSet)
    {
        lock (_ioLock)
        {
            if (_disposed) return false;
            foreach ((object key, Color color) in dataSet)
            {
                if (key is not int led || led < 0 || led >= 3 * ColorfulProtocol.LedsPerHeader) continue;
                (byte _, byte r, byte g, byte b) = color.GetRGBBytes();
                _frame[led * 3] = r;
                _frame[led * 3 + 1] = g;
                _frame[led * 3 + 2] = b;
            }
            try
            {
                ColorfulProtocol.WriteFrame(_frame, report => stream.Write(report));
                _failed = false;
                return true;
            }
            catch (Exception ex)
            {
                if (!_failed) logger.Warning(ex, "Colorful ARGB frame write failed; reconnect controller or toggle plugin to reopen it");
                _failed = true;
                return false;
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        lock (_ioLock)
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                Array.Clear(_frame);
                ColorfulProtocol.WriteFrame(_frame, report => stream.Write(report));
            }
            catch (Exception ex) { logger.Debug(ex, "Could not clear Colorful LEDs on shutdown"); }
            finally { stream.Dispose(); }
        }
    }
}
