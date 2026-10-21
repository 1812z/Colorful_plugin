namespace Artemis.Plugins.Colorful;

/// <summary>Wire format ported from Sky's Colorful_Motherboard_Controller.js.</summary>
public static class ColorfulProtocol
{
    public const int LedsPerHeader = 60;
    public const int FrameLedCount = 220;
    public const int LedsPerPacket = 20;
    public const int ReportLength = 65;

    public static void WriteFrame(byte[] rgb, Action<byte[]> write)
    {
        ArgumentNullException.ThrowIfNull(rgb);
        ArgumentNullException.ThrowIfNull(write);
        if (rgb.Length != FrameLedCount * 3)
            throw new ArgumentException("A Colorful frame must contain exactly 220 RGB LEDs.", nameof(rgb));

        for (int packet = 0; packet < FrameLedCount / LedsPerPacket; packet++)
        {
            byte[] report = CreateReport((byte)packet);
            Buffer.BlockCopy(rgb, packet * 60, report, 5, 60);
            write(report);
        }
        write(CreateReport(0xff));
    }

    private static byte[] CreateReport(byte index)
    {
        byte[] report = new byte[ReportLength];
        report[1] = 0x01;
        report[3] = 0x88;
        report[4] = index;
        return report;
    }
}
