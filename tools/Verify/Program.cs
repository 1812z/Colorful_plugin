using Artemis.Plugins.Colorful;
using HidSharp;
using RGB.NET.Core;
using System.Diagnostics;

// Check the complete wire image against the original JS framing rules,
// including the unused tail and final latch packet. No HID writes are made.
byte[] frame = new byte[660];
for (int channel = 0; channel < 3; channel++)
    for (int led = 0; led < 60; led++)
        frame[(channel * 60 + led) * 3 + channel] = (byte)(led + 1);
List<byte[]> reports = [];
ColorfulProtocol.WriteFrame(frame, reports.Add);
Check(reports.Count == 12, "11 data packets plus latch");
for (int packet = 0; packet < 12; packet++)
{
    byte[] report = reports[packet];
    Check(report.Length == 65, "65-byte report");
    Check(report[0] == 0 && report[1] == 1 && report[2] == 0 && report[3] == 0x88, "header");
    Check(report[4] == (packet == 11 ? 0xff : packet), "packet index/latch");
    for (int i = 0; i < 60; i++)
        Check(report[i + 5] == (packet == 11 ? 0 : frame[packet * 60 + i]), "payload");
}
Console.WriteLine("PASS: three independent header ranges, all RGB payloads, zero tail, 11 packets and latch.");
bool rejected = false;
try { ColorfulProtocol.WriteFrame(new byte[540], _ => { }); }
catch (ArgumentException) { rejected = true; }
Check(rejected, "reject truncated frame");

// With no independent limiter, updates follow the producer (Artemis's renderer).
using (DeviceUpdateTrigger trigger = new())
{
    int updates = 0;
    trigger.Update += (_, _) => Interlocked.Increment(ref updates);
    trigger.Start();
    foreach (int fps in new[] { 30, 120 })
    {
    Interlocked.Exchange(ref updates, 0);
    Stopwatch timer = Stopwatch.StartNew();
    while (timer.ElapsedMilliseconds < 500)
    {
        trigger.TriggerHasData();
        Thread.Sleep(TimeSpan.FromSeconds(1.0 / fps));
    }
    int count = Volatile.Read(ref updates);
    Check(count >= fps * 0.3 && count <= fps * 0.8 + 2, "live target FPS change");
    Console.WriteLine($"PASS: target {fps} FPS, RGB.NET delivered {count} updates in {timer.ElapsedMilliseconds} ms.");
    }
    Thread.Sleep(100);
    int idleCount = Volatile.Read(ref updates);
    Thread.Sleep(150);
    Check(Volatile.Read(ref updates) == idleCount, "no extra frames without renderer updates");
}

if (args.Contains("--probe"))
{
    int matches = 0;
    foreach (HidDevice hid in DeviceList.Local.GetHidDevices(0x2f4c, 0x1017))
    {
        uint[] usages = hid.GetReportDescriptor().DeviceItems.SelectMany(i => i.Usages.GetAllValues()).ToArray();
        Console.WriteLine($"{hid.DevicePath}\n  output={hid.GetMaxOutputReportLength()}, usages={string.Join(",", usages.Select(u => u.ToString("X8")))}");
        if (hid.GetMaxOutputReportLength() == 65 && usages.Contains(0xff010001u) &&
            (hid.DevicePath.Contains("mi_01", StringComparison.OrdinalIgnoreCase) ||
             hid.DevicePath.Contains("mi_02", StringComparison.OrdinalIgnoreCase)))
        {
            using HidStream stream = hid.Open();
            matches++;
            Console.WriteLine("  PASS: lighting endpoint opened (no writes).");
        }
    }
    Check(matches > 0, "usable Colorful lighting endpoint");
}

static void Check(bool value, string message)
{
    if (!value) throw new InvalidOperationException($"FAIL: {message}");
}
