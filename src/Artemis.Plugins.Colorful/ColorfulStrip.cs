using RGB.NET.Core;

namespace Artemis.Plugins.Colorful;

internal sealed class ColorfulStrip : AbstractRGBDevice<ColorfulStripInfo>
{
    public ColorfulStrip(int channel, IUpdateQueue queue) : base(new ColorfulStripInfo(channel), queue)
    {
        for (int led = 0; led < ColorfulProtocol.LedsPerHeader; led++)
            AddLed((LedId)((int)LedId.LedStripe1 + led), new Point(led * 10, 0), new Size(10, 10),
                channel * ColorfulProtocol.LedsPerHeader + led);
    }
}

internal sealed class ColorfulStripInfo(int channel) : IRGBDeviceInfo
{
    public RGBDeviceType DeviceType => RGBDeviceType.LedStripe;
    public string Manufacturer => "Colorful";
    public string Model => $"BATTLE-AX B760M-PLUS S WIFI7 ARGB {channel + 1}";
    public string DeviceName => $"{Manufacturer} {Model}";
    public object? LayoutMetadata { get; set; }
}
