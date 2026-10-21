using Artemis.Core;
using Artemis.Core.DeviceProviders;
using Artemis.Core.Services;
using RGB.NET.Core;
using Serilog;

namespace Artemis.Plugins.Colorful;

[PluginFeature(Name = "Colorful B760M ARGB", Description = "Three independent 60-LED ARGB headers (2F4C:1017).")]
public sealed class ColorfulDeviceProvider : DeviceProvider
{
    private readonly ILogger _logger;
    private readonly IDeviceService _deviceService;
    private ColorfulRgbProvider _provider;
    public override IRGBDeviceProvider RgbDeviceProvider => _provider;

    public ColorfulDeviceProvider(ILogger logger, IDeviceService deviceService)
    {
        _logger = logger;
        _deviceService = deviceService;
        _provider = new ColorfulRgbProvider(logger);
        CreateMissingLedsSupported = false;
        RemoveExcessiveLedsSupported = false;
        SuspendSupported = true;
    }

    public override void Enable()
    {
        _provider = new ColorfulRgbProvider(_logger);
        _deviceService.AddDeviceProvider(this);
    }

    public override void Disable()
    {
        _deviceService.RemoveDeviceProvider(this);
        _provider.Dispose();
    }
}
